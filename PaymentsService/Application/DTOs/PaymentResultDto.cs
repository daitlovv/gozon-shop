namespace Payments.Application.Dtos;

public record PaymentResultDto(
    Guid EventId,
    Guid OrderId,
    string Status,
    string Reason
);