namespace Strata.Journaling.Tests.SidecarTests.Model;

[GenerateSerializer]
public class UserData
{
    [Id(0)]
    public string Name { get; set; } = null!;

    [Id(1)]
    public string ReferenceId { get; set; } = null!;
}
