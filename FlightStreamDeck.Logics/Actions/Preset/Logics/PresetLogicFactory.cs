using Microsoft.Extensions.DependencyInjection;
using System;

namespace FlightStreamDeck.Logics.Actions.Preset;

public class PresetLogicFactory
{
    private readonly IServiceProvider serviceProvider;

    public PresetLogicFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IPresetToggleLogic? Create(string? type) => type switch
    {
        PresetFunctions.Heading => serviceProvider.GetRequiredService<PresetHeadingLogic>(),
        PresetFunctions.VerticalSpeed => serviceProvider.GetRequiredService<PresetVerticalSpeedLogic>(),
        PresetFunctions.Altitude => serviceProvider.GetRequiredService<PresetAltitudeLogic>(),
        PresetFunctions.FLC => serviceProvider.GetRequiredService<PresetFlcLogic>(),
        PresetFunctions.AirSpeed => serviceProvider.GetRequiredService<PresetFlcLogic>(),

        PresetFunctions.Avionics => serviceProvider.GetRequiredService<PresetAvionicsLogic>(),
        PresetFunctions.ApMaster => serviceProvider.GetRequiredService<PresetApMasterLogic>(),
        PresetFunctions.Nav => serviceProvider.GetRequiredService<PresetNavLogic>(),
        PresetFunctions.Approach => serviceProvider.GetRequiredService<PresetApproachLogic>(),

        PresetFunctions.VOR1 => serviceProvider.GetRequiredService<PresetVor1Logic>(),
        PresetFunctions.VOR2 => serviceProvider.GetRequiredService<PresetVor2Logic>(),

        _ => null,
    };
}
