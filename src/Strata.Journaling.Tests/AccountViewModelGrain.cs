namespace Strata.Journaling.Tests;

internal sealed class AccountViewModelGrain : Grain, IAccountViewModelGrain
{
    private double _balance;
    
    public Task<double> GetBalance() => Task.FromResult(_balance);

    public Task UpdateBalance(double newBalance)
    {
        _balance = newBalance;
        return Task.CompletedTask;
    }
}
