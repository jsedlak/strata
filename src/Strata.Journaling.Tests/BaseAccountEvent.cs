namespace Strata.Journaling.Tests;

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
