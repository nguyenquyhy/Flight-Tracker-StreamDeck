using Microsoft.Extensions.DependencyInjection;
using System;

namespace FlightStreamDeck.Logics.Actions;

public class PresetLogicFactory
{
    private readonly IServiceProvider serviceProvider;

    public PresetLogicFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public IPresetToggleLogic? Create(string? type) => type switch
    {
        PresetFunction.Heading => serviceProvider.GetRequiredService<PresetHeadingLogic>(),
        PresetFunction.VerticalSpeed => serviceProvider.GetRequiredService<PresetVerticalSpeedLogic>(),
        PresetFunction.Altitude => serviceProvider.GetRequiredService<PresetAltitudeLogic>(),
        PresetFunction.FLC => serviceProvider.GetRequiredService<PresetFlcLogic>(),
        ValueChangeFunction.AirSpeed => serviceProvider.GetRequiredService<PresetFlcLogic>(),

        PresetFunction.Avionics => serviceProvider.GetRequiredService<PresetAvionicsLogic>(),
        PresetFunction.ApMaster => serviceProvider.GetRequiredService<PresetApMasterLogic>(),
        PresetFunction.Nav => serviceProvider.GetRequiredService<PresetNavLogic>(),
        PresetFunction.Approach => serviceProvider.GetRequiredService<PresetApproachLogic>(),
        _ => null,
    };
}
