namespace Strata.Journaling.Tests;

public interface IAccountGrain : IGrainWithStringKey
{
    Task Deposit(double amount);

    Task Withdraw(double amount);

    Task<double> GetBalance();
}
