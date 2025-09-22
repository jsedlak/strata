using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using Strata.Projections;

namespace Strata.Examples
{
    /// <summary>
    /// Examples demonstrating how to use the Strata projections feature.
    /// </summary>
    public class ProjectionExamples
    {
        /// <summary>
        /// Example of a simple projection that handles account events.
        /// </summary>
        public class AccountBalanceProjection : IProjection<AmountDepositedEvent>, IProjection<AmountWithdrawnEvent>
        {
            private readonly ILogger<AccountBalanceProjection> _logger;
            private decimal _balance;

            public AccountBalanceProjection(ILogger<AccountBalanceProjection> logger)
            {
                _logger = logger;
            }

            public async Task Handle(AmountDepositedEvent @event)
            {
                _balance += @event.Amount;
                _logger.LogInformation("Deposit processed. New balance: {Balance}", _balance);
                await Task.CompletedTask;
            }

            public async Task Handle(AmountWithdrawnEvent @event)
            {
                _balance -= @event.Amount;
                _logger.LogInformation("Withdrawal processed. New balance: {Balance}", _balance);
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Example of a projection that tracks account activity.
        /// </summary>
        public class AccountActivityProjection : IProjection<AmountDepositedEvent>, IProjection<AmountWithdrawnEvent>
        {
            private readonly ILogger<AccountActivityProjection> _logger;
            private int _transactionCount;

            public AccountActivityProjection(ILogger<AccountActivityProjection> logger)
            {
                _logger = logger;
            }

            public async Task Handle(AmountDepositedEvent @event)
            {
                _transactionCount++;
                _logger.LogInformation("Deposit recorded. Transaction count: {Count}", _transactionCount);
                await Task.CompletedTask;
            }

            public async Task Handle(AmountWithdrawnEvent @event)
            {
                _transactionCount++;
                _logger.LogInformation("Withdrawal recorded. Transaction count: {Count}", _transactionCount);
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Example of how to register projections with an EventSourcedGrain.
        /// </summary>
        public class BankAccountGrain : EventSourcedGrain
        {
            public BankAccountGrain(ILogger<BankAccountGrain> logger) : base(logger)
            {
                // Register projections
                this.RegisterProjection<AccountBalanceProjection>();
                this.RegisterProjection<AccountActivityProjection>();
            }

            public async Task Deposit(decimal amount)
            {
                var @event = new AmountDepositedEvent { Amount = amount, Timestamp = DateTime.UtcNow };
                await Raise(@event);
            }

            public async Task Withdraw(decimal amount)
            {
                var @event = new AmountWithdrawnEvent { Amount = amount, Timestamp = DateTime.UtcNow };
                await Raise(@event);
            }
        }

        /// <summary>
        /// Example of a stream-based projection grain.
        /// </summary>
        [ImplicitStreamSubscription("AccountStream")]
        public class StreamBasedProjectionGrain : EventRecipientGrain, IStreamBasedProjectionGrain
        {
            public StreamBasedProjectionGrain(ILogger<StreamBasedProjectionGrain> logger) : base(logger)
            {
            }

            public async Task Handle(AmountDepositedEvent @event)
            {
                // Process deposit event from stream
                await Task.CompletedTask;
            }

            public async Task Handle(AmountWithdrawnEvent @event)
            {
                // Process withdrawal event from stream
                await Task.CompletedTask;
            }
        }

        /// <summary>
        /// Example of how to configure projections in the service collection.
        /// </summary>
        public static class ServiceConfigurationExample
        {
            public static void ConfigureProjections(IServiceCollection services)
            {
                services.AddProjections(options =>
                {
                    options.MaxConcurrency = 20;
                    options.ProcessingTimeoutMs = 60000;
                    options.MaxRetryAttempts = 5;
                    options.RetryDelayMs = 2000;
                    options.EnableDeadLetterQueue = true;
                    options.MaxQueueSize = 50000;
                    options.EnablePerformanceCounters = true;
                    options.BatchSize = 25;
                });
            }
        }

        // Example event types
        public class AmountDepositedEvent
        {
            public decimal Amount { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class AmountWithdrawnEvent
        {
            public decimal Amount { get; set; }
            public DateTime Timestamp { get; set; }
        }

        // Example grain interface
        public interface IStreamBasedProjectionGrain : IGrainWithStringKey
        {
        }
    }
}
