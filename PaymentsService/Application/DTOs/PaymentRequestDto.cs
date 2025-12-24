namespace Payments.Application.Dtos;

public record PaymentRequestDto(
    Guid EventId,
    Guid OrderId,
    Guid UserId,
    decimal Amount
);