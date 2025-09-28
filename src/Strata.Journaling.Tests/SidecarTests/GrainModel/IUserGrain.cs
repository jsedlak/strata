using Strata.Journaling.Tests.SidecarTests.Model;

namespace Strata.Journaling.Tests.SidecarTests.GrainModel;

public interface IUserGrain : IGrainWithStringKey
{
    Task<UserData> GetData();

    ValueTask SetName(string name);

    ValueTask SetReferenceId(string referenceId);
}
