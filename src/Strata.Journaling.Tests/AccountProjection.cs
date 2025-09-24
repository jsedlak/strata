namespace Strata.Journaling.Tests;

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
            var viewModelGrain = _grainFactory.GetGrain<IAccountViewModelGrain>(accountId);
            await viewModelGrain.UpdateBalance(balanceEvent.Balance);
        }
    }
}