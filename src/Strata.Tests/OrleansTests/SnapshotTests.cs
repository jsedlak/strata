using Microsoft.Extensions.DependencyInjection;
using Strata.Snapshotting;
using Strata.Tests.Commands;
using Strata.Tests.Grains;

namespace Strata.Tests;

[TestClass]
public class SnapshotTests : OrleansTestBase<SnapshottingSiloConfigurator>
{
    [TestMethod]
    public async Task CanSnapshot()
    {
        var bankAccount = Grains.GetGrain<IBankAccountGrain>(Guid.NewGuid());

        var depositBalance = await bankAccount.Deposit(new DepositCommand() { Amount = 2_000 });
        var withdrawBalance = await bankAccount.Withdraw(new WithdrawCommand() { Amount = 1_000 });
        var finalBalance = await bankAccount.GetBalance();

        Assert.AreEqual(2000, depositBalance);
        Assert.AreEqual(finalBalance, withdrawBalance);
    }
}