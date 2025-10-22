using Orleans.Concurrency;

namespace Strata;

public interface IJournaledGrain 
{
    [AlwaysInterleave]
    Task ProcessOutbox();
}