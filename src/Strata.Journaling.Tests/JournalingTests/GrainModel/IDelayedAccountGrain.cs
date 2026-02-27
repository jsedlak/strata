using Strata.Journaling.Tests.JournalingTests.Events;

namespace Strata.Journaling.Tests.JournalingTests.GrainModel;

public interface IDelayedAccountGrain : IGrainWithStringKey
{
    Task Deposit(double amount);

    Task Withdraw(double amount);

    Task<double> GetBalance();

    Task<bool> GetIsProcessingOutbox();

    Task<BaseAccountEvent[]> GetEvents();
}
