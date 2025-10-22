//using Microsoft.Extensions.DependencyInjection;

//namespace Strata.Sidecars;

//public static class SidecarBuilderExtensions
//{
//    public static ISiloBuilder AddSidecars(this ISiloBuilder builder)
//    {
//        builder.AddGrainExtension<ISidecarControlExtension, SidecarControlExtension>();

//        builder.ConfigureServices(services =>
//        {
//            services.AddTransient(typeof(ILifecycleParticipant<IGrainLifecycle>),
//                serviceProvider =>
//                {
//                    // Factory wrapper so Orleans resolves per-grain
//                    return (object grain) =>
//                    {
//                        var grainType = grain.GetType();
//                        var sidecarInterfaces = grainType.GetInterfaces()
//                            .Where(i => i.IsGenericType &&
//                                        i.GetGenericTypeDefinition() == typeof(ISidecarHost<>));

//                        foreach (var iface in sidecarInterfaces)
//                        {
//                            var sidecarType = iface.GetGenericArguments()[0];
//                            var participantType = typeof(SidecarLifecycleParticipant<,>)
//                                .MakeGenericType(grainType, sidecarType);

//                            return ActivatorUtilities.CreateInstance(serviceProvider, participantType, grain);
//                        }

//                        return null!;
//                    };
//                });
//        });

//        return builder;
//    }
//}

