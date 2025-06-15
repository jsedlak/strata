using Orleans;
using Strata.Tests.Commands;

namespace Strata.Tests.Grains;

public interface IBankAccountGrain : IGrainWithGuidKey
{
    public ValueTask<double> Deposit(DepositCommand command);

    public ValueTask<double> Withdraw(WithdrawCommand command);

    public ValueTask<double> GetBalance();
}
