namespace Strata.Journaling.Tests.JournalingTests.Events;

[GenerateSerializer]
public abstract class BaseAccountEvent
{
    protected BaseAccountEvent(string id)
    {
       Id = id;
    }

    [Id(0)]
    public string Id { get; set; } = null!;
}
