using Microsoft.Extensions.Logging;
using Strata.Journaling.Tests.JournalingTests.Events;
using Strata.Journaling.Tests.JournalingTests.GrainModel;

namespace Strata.Journaling.Tests.JournalingTests.Projections;

public sealed class DelayedAccountProjection : IOutboxRecipient<BaseAccountEvent>
{
    private static readonly TimeSpan ProjectionDelay = TimeSpan.FromMilliseconds(250);

    private readonly IGrainFactory _grainFactory;
    private readonly ILogger _logger;

    public DelayedAccountProjection(IGrainFactory grainFactory, ILogger logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
    }

    public async Task Handle(int version, BaseAccountEvent @event)
    {
        if (@event is BalanceAdjustedEvent balanceEvent)
        {
            var accountId = balanceEvent.Id;

            await Task.Delay(ProjectionDelay);

            _logger.LogInformation("[Delayed] Updating balance for account {0} to {1}", accountId, balanceEvent.Balance);

            var viewModelGrain = _grainFactory.GetGrain<IAccountViewModelGrain>(accountId);
            await viewModelGrain.UpdateBalance(balanceEvent.Balance);
        }
    }
}
