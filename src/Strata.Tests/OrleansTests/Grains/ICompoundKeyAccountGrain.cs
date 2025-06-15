using Strata.Tests.Commands;

namespace Strata.Tests.Grains;

public interface ICompoundKeyAccountGrain : IGrainWithStringKey
{
    public ValueTask<double> Deposit(DepositCommand command);

    public ValueTask<double> Withdraw(WithdrawCommand command);
    
    public ValueTask<double> GetBalance();
}