using Strata.Journaling.Tests.JournalingTests.Events;

namespace Strata.Journaling.Tests.JournalingTests.GrainModel;

public interface IAccountGrain : IGrainWithStringKey
{
    Task Deposit(double amount);

    Task Withdraw(double amount);

    Task<double> GetBalance();

    ValueTask Deactivate();

    Task<BaseAccountEvent[]> GetEvents();
}
