using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans;

namespace Strata.Projections
{
    /// <summary>
    /// Extension methods for configuring projections in the service collection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds projection services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional configuration action for projection options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddProjections(
            this IServiceCollection services, 
            Action<ProjectionOptions> configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Configure options
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            else
            {
                services.Configure<ProjectionOptions>(options => { });
            }

            // Register projection services
            services.AddSingleton<ProjectionRegistry>();
            services.AddTransient<IProjectionGrain, ProjectionGrain>();

            return services;
        }

        /// <summary>
        /// Adds projection services with custom options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The projection options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddProjections(
            this IServiceCollection services, 
            ProjectionOptions options)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            services.Configure<ProjectionOptions>(opt =>
            {
                opt.MaxConcurrency = options.MaxConcurrency;
                opt.ProcessingTimeoutMs = options.ProcessingTimeoutMs;
                opt.MaxRetryAttempts = options.MaxRetryAttempts;
                opt.RetryDelayMs = options.RetryDelayMs;
                opt.EnableDeadLetterQueue = options.EnableDeadLetterQueue;
                opt.MaxQueueSize = options.MaxQueueSize;
                opt.EnablePerformanceCounters = options.EnablePerformanceCounters;
                opt.BatchSize = options.BatchSize;
            });

            services.AddSingleton<ProjectionRegistry>();
            services.AddTransient<IProjectionGrain, ProjectionGrain>();

            return services;
        }
    }
}
