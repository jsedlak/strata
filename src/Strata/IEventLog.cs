namespace Strata;

public interface IEventLog<TView, TEntry>
    where TView : class, new()
    where TEntry : class
{
    Task Hydrate();
    
    void Submit(TEntry entry);

    void Submit(IEnumerable<TEntry> entries);

    /// <summary>
    /// Confirms all pending entries and (when true) truncates the log
    /// </summary>
    Task Snapshot(bool truncate);

    Task WaitForConfirmation();
    
    TView TentativeView { get; }
    
    TView ConfirmedView { get; }
    
    int ConfirmedVersion { get; }
    
    int TentativeVersion { get; }
    
    // IEnumerable<TEntry> UnconfirmedTail { get; }
}