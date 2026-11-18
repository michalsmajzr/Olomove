using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Payments;
using Olomove.Models.Users;

namespace Olomove.Controllers.Profile;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/user/payments")]
public class CreditPaymentsController(
    ApplicationDbContext context,
    UserManager<User> userManager,
    IConfiguration configuration) : ControllerBase
{
    private const string AccountIban = "CZ5855000000001265098001";
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {

        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var variableSymbol = await CreateNextVariableSymbolAsync();

        var payment = new CreditPayment
        {
            UserId = user.Id,
            Amount = decimal.Round(request.Amount, 2),
            VariableSymbol = variableSymbol
        };

        context.CreditPayments.Add(payment);
        await context.SaveChangesAsync();

        var qrPayload = FormattableString.Invariant(
            $"SPD*1.0*ACC:{AccountIban}*AM:{payment.Amount:0.00}*CC:CZK*X-VS:{variableSymbol}");

        return Ok(new
        {
            payment.Id,
            payment.Amount,
            payment.VariableSymbol,
            payment.Status,
            payment.CreatedAt,
            qrPayload,
            accountName = configuration["Payment:AccountName"]?.Trim()
        });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActivePayment()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var payment = await context.CreditPayments
            .Where(p => p.UserId == user.Id && p.Status == PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (payment is null)
        {
            return NotFound();
        }

        var qrPayload = FormattableString.Invariant(
            $"SPD*1.0*ACC:{AccountIban}*AM:{payment.Amount:0.00}*CC:CZK*X-VS:{payment.VariableSymbol}");

        return Ok(new
        {
            payment.Id,
            payment.Amount,
            payment.VariableSymbol,
            payment.Status,
            payment.CreatedAt,
            qrPayload,
            accountName = configuration["Payment:AccountName"]?.Trim()
        });
    }

    [HttpDelete("{id:guid}/cancel")]
    public async Task<IActionResult> CancelPayment(Guid id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var payment = await context.CreditPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.Id);

        if (payment is null)
        {
            return NotFound("Platba nebyla nalezena.");
        }

        if (payment.Status == PaymentStatus.Credited)
        {
            return Conflict("Tuto platbu už nelze zrušit, byla připsána.");
        }

        payment.Status = PaymentStatus.Cancelled;
        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string> CreateNextVariableSymbolAsync()
    {
        var year = DateTime.UtcNow.Year;

        var lastVs = await context.CreditPayments
            .IgnoreQueryFilters()
            .Where(p => p.VariableSymbol.StartsWith(year.ToString()))
            .MaxAsync(p => (string?)p.VariableSymbol);

        var nextVs = string.IsNullOrEmpty(lastVs)
            ? year * 10000 + 1
            : int.Parse(lastVs) + 1;

        return nextVs.ToString();
    }

    public class CreatePaymentRequest
    {
        [Range(typeof(decimal), "1", "100000", ErrorMessage = "Částka musí být mezi 1 a 100 000 Kč.")]
        public decimal Amount { get; set; }
    }
}
