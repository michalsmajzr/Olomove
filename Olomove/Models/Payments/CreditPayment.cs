using Olomove.Models.Users;

namespace Olomove.Models.Payments;

public class CreditPayment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal Amount { get; set; }
    public string VariableSymbol { get; set; } = "";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CreditedAt { get; set; }
}
