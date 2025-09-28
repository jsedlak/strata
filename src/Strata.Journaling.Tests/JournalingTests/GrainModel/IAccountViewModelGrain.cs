namespace Strata.Journaling.Tests.JournalingTests.GrainModel;

public interface IAccountViewModelGrain : IGrainWithStringKey
{
    Task<double> GetBalance();

    Task UpdateBalance(double newBalance);
}
