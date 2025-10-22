using Strata.Journaling.Tests.JournalingTests.Events;
using Strata.Journaling.Tests.JournalingTests.GrainModel;

namespace Strata.Journaling.Tests.JournalingTests.Projections;

public sealed class AccountProjection : IOutboxRecipient<BaseAccountEvent>
{
    private readonly IGrainFactory _grainFactory;

    public AccountProjection(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    public async Task Handle(int version, BaseAccountEvent @event)
    {
        if (@event is BalanceAdjustedEvent balanceEvent)
        {
            var accountId = balanceEvent.Id;

            Console.WriteLine("Updating balance for account {0} to {1}", accountId, balanceEvent.Balance);

            var viewModelGrain = _grainFactory.GetGrain<IAccountViewModelGrain>(accountId);
            await viewModelGrain.UpdateBalance(balanceEvent.Balance);
        }
    }
}