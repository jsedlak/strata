using Strata.Tests.Commands;

namespace Strata.Tests.Grains;

public interface IDelayedBankAccountGrain : IGrainWithGuidKey
{
    ValueTask<double> Deposit(DepositCommand command);

    ValueTask<double> Withdraw(WithdrawCommand command);

    ValueTask<double> GetBalance();

    ValueTask<double> GetConfirmedBalance();
}