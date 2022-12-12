using FlightStreamDeck.Logics.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace FlightStreamDeck.Logics;

public static class Extensions
{
    public static IServiceCollection AddPresetLogics(this IServiceCollection services)
        => services
            .AddScoped<PresetLogicFactory>()
            .AddScoped<PresetHeadingLogic>()
            .AddScoped<PresetVerticalSpeedLogic>()
            .AddScoped<PresetAltitudeLogic>()
            .AddScoped<PresetFlcLogic>()
            .AddScoped<PresetAvionicsLogic>()
            .AddScoped<PresetApMasterLogic>()
            .AddScoped<PresetNavLogic>()
            .AddScoped<PresetApproachLogic>();
}
