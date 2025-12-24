namespace Payments.Application.Dtos;

public record WithdrawalResultDto(
    Guid OrderId,
    string Status,
    string Reason
);