using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Strata.Tests.Commands;
using Strata.Tests.Grains;

namespace Strata.Tests;

[TestClass]
public class BasicEventSourcingTests : OrleansTestBase<DefaultSiloConfigurator>
{
    [TestMethod]
    public async Task CanStoreEvents()
    {
        var bankAccount = Grains.GetGrain<IBankAccountGrain>(Guid.NewGuid());

        var depositBalance = await bankAccount.Deposit(new DepositCommand() { Amount = 2_000 });
        var withdrawBalance = await bankAccount.Withdraw(new WithdrawCommand() { Amount = 1_000 });
        var finalBalance = await bankAccount.GetBalance();

        Assert.AreEqual(2_000, depositBalance, "Deposit balance is incorrect");
        Assert.AreEqual(1_000, finalBalance, "Final balance is incorrect");
    }

    [TestMethod]
    public async Task CanStoreEventsWithStringKey()
    {
        var bankAccount = Grains.GetGrain<ICompoundKeyAccountGrain>("helloworld1234");

        var depositBalance = await bankAccount.Deposit(new DepositCommand() { Amount = 2_000 });
        var withdrawBalance = await bankAccount.Withdraw(new WithdrawCommand() { Amount = 1_000 });
        var finalBalance = await bankAccount.GetBalance();

        Assert.AreEqual(2_000, depositBalance, "Deposit balance is incorrect");
        Assert.AreEqual(1_000, finalBalance, "Final balance is incorrect");
    }

    [TestMethod]
    public async Task CanDelayLogWrite()
    {
        var bankAccount = Grains.GetGrain<IDelayedBankAccountGrain>(Guid.NewGuid());

        var depositBalance = await bankAccount.Deposit(new DepositCommand() { Amount = 2_000 });
        var withdrawBalance = await bankAccount.Withdraw(new WithdrawCommand() { Amount = 1_000 });
        var finalBalance = await bankAccount.GetBalance();
        var confirmedBalance = await bankAccount.GetConfirmedBalance();

        Assert.AreEqual(2_000, depositBalance, "Deposit balance is incorrect");
        Assert.AreEqual(1_000, finalBalance, "Final balance is incorrect");
        Assert.AreNotEqual(confirmedBalance, finalBalance, "Confirmed balance should not match final balance immediately after deposit and withdraw operations.");

        await Task.Delay(TimeSpan.FromSeconds(3));

        confirmedBalance = await bankAccount.GetConfirmedBalance();

        Assert.AreEqual(confirmedBalance, finalBalance, "Confirmed balance should match final balance after delay.");
    }
}