using System;
using System.ComponentModel.DataAnnotations;

namespace Strata.Projections
{
    /// <summary>
    /// Configuration options for projection processing.
    /// </summary>
    public class ProjectionOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent projections to process.
        /// </summary>
        [Range(1, 1000)]
        public int MaxConcurrency { get; set; } = 10;

        /// <summary>
        /// Gets or sets the timeout for projection processing in milliseconds.
        /// </summary>
        [Range(1000, 300000)]
        public int ProcessingTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed projections.
        /// </summary>
        [Range(0, 10)]
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts in milliseconds.
        /// </summary>
        [Range(100, 60000)]
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to enable dead letter queue for failed projections.
        /// </summary>
        public bool EnableDeadLetterQueue { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum queue size for projection processing.
        /// </summary>
        [Range(100, 100000)]
        public int MaxQueueSize { get; set; } = 10000;

        /// <summary>
        /// Gets or sets whether to enable performance counters.
        /// </summary>
        public bool EnablePerformanceCounters { get; set; } = true;

        /// <summary>
        /// Gets or sets the batch size for processing multiple projections.
        /// </summary>
        [Range(1, 100)]
        public int BatchSize { get; set; } = 10;
    }
}
