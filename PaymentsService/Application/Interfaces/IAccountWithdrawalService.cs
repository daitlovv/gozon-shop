using Payments.Application.Dtos;
using Payments.Application.Services;

namespace Payments.Application.Interfaces;

public interface IAccountWithdrawalService
{
    Task<WithdrawalResultDto> TryWithdrawAsync(PaymentRequestDto request);
}