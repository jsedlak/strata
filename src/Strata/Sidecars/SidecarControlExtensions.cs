namespace Strata.Sidecars;

public static class SidecarControlExtensions
{
    public static Task EnableSidecar<TSidecar>(this ISidecarHost<TSidecar> grain)
        where TSidecar : class, ISidecarGrain
        => grain.AsReference<ISidecarControlExtension>().EnableSidecar();

    public static Task DisableSidecar<TSidecar>(this ISidecarHost<TSidecar> grain)
        where TSidecar : class, ISidecarGrain
        => grain.AsReference<ISidecarControlExtension>().DisableSidecar();

    // Convenience methods for any IGrain that can be cast to ISidecarHost
    public static async Task EnableSidecar<TSidecar>(this IGrain grain)
        where TSidecar : class, ISidecarGrain
    {
        var host = grain.AsReference<ISidecarHost<TSidecar>>();
        await host.EnableSidecar();
    }

    public static async Task DisableSidecar<TSidecar>(this IGrain grain)
        where TSidecar : class, ISidecarGrain
    {
        var host = grain.AsReference<ISidecarHost<TSidecar>>();
        await host.DisableSidecar();
    }
}

