using Microsoft.Extensions.DependencyInjection;
using Strata.Journaling.Tests.JournalingTests.GrainModel;
using Strata.Journaling.Tests.JournalingTests.Model;

namespace Strata.Journaling.Tests.JournalingTests.Grains;

internal sealed class AccountViewModelGrain : Grain, IAccountViewModelGrain
{
    private readonly IPersistentState<AccountViewModel> _state;

    public AccountViewModelGrain(
        [FromKeyedServices("state")] IPersistentState<AccountViewModel> state
    )
    {
        _state = state;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        if(!_state.RecordExists)
        {
            _state.State = new();
            await _state.WriteStateAsync();
        }
        
    }

    public Task<double> GetBalance() => Task.FromResult(_state.State.Balance);

    public async Task UpdateBalance(double newBalance)
    {
        Console.WriteLine("Receiving balance update for account {0} to {1}", this.GetPrimaryKeyString(), newBalance);
        _state.State.Balance = newBalance;
        await _state.WriteStateAsync();
    }
}
