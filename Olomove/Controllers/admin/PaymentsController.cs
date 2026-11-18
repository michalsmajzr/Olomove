using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Olomove.Data;
using Olomove.Models.Payments;
using Olomove.Models.Users;

namespace Olomove.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/payments")]
public class PaymentsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPayments()
    {
        var payments = await context.CreditPayments
            .AsNoTracking()
            .OrderByDescending(payment => payment.CreatedAt)
            .Select(payment => new
            {
                payment.Id,
                payment.Amount,
                payment.VariableSymbol,
                payment.Status,
                payment.CreatedAt,
                payment.CreditedAt,
                User = new
                {
                    payment.User.Id,
                    Name = $"{payment.User.FirstName} {payment.User.LastName}".Trim(),
                    payment.User.Email
                }
            })
            .ToListAsync();

        return Ok(payments);
    }

    [HttpPost("{id:guid}/credit")]
    public async Task<IActionResult> Credit(Guid id)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var payment = await context.CreditPayments.Include(item => item.User).SingleOrDefaultAsync(item => item.Id == id);

        if (payment is null)
        {
            return NotFound("Platba nebyla nalezena.");
        }
            
        if (payment.Status == PaymentStatus.Credited)
        {
            return Conflict("Platba už byla připsána.");
        }
            
        payment.User.Credit += payment.Amount;
        payment.Status = PaymentStatus.Credited;
        payment.CreditedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new
        {
            payment.Id,
            payment.Status,
            payment.CreditedAt,
            payment.UserId,
            payment.User.Credit
        });
    }

    [HttpDelete("{id:guid}/cancel")]
    public async Task<IActionResult> CancelPayment(Guid id)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        var payment = await context.CreditPayments
            .SingleOrDefaultAsync(item => item.Id == id);

        if (payment is null)
            return NotFound("Platba nebyla nalezena.");

        if (payment.Status == PaymentStatus.Credited)
            return Conflict("Platba už byla připsána.");

        payment.Status = PaymentStatus.Cancelled;
        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return NoContent();
    }
}
