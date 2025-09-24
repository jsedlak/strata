namespace Strata.Journaling.Tests;

public interface IAccountViewModelGrain : IGrainWithStringKey
{
    Task<double> GetBalance();

    Task UpdateBalance(double newBalance);
}
