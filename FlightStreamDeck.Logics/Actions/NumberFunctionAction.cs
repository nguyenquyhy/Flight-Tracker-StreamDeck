using System;

namespace FlightStreamDeck.Logics.Actions;

#region Action Registration

[StreamDeckAction("tech.flighttracker.streamdeck.number.enter")]
public class NumberEnterAction : NumberFunctionAction
{
    public NumberEnterAction(RegistrationParameters registrationParameters) : base(registrationParameters)
    {
    }
}
[StreamDeckAction("tech.flighttracker.streamdeck.number.backspace")]
public class NumberBackspaceAction : NumberFunctionAction
{
    public NumberBackspaceAction(RegistrationParameters registrationParameters) : base(registrationParameters)
    {
    }
}
[StreamDeckAction("tech.flighttracker.streamdeck.number.cancel")]
public class NumberCancelAction : NumberFunctionAction
{
    public NumberCancelAction(RegistrationParameters registrationParameters) : base(registrationParameters)
    {
    }
}
[StreamDeckAction("tech.flighttracker.streamdeck.number.transfer")]
public class NumberTransferAction : NumberFunctionAction
{
    public NumberTransferAction(RegistrationParameters registrationParameters) : base(registrationParameters)
    {
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        if (NumpadStorage.NumpadParams?.Type == "XPDR")
        {
            await SetTitleAsync("VFR");
        }
        else
        {
            await SetTitleAsync("Xfer");
        }
    }
}

#endregion

public class NumberFunctionAction : StreamDeckAction
{
    private readonly RegistrationParameters registrationParameters;

    public NumberFunctionAction(RegistrationParameters registrationParameters)
    {
        this.registrationParameters = registrationParameters;
    }

    protected override async Task OnKeyDown(ActionEventArgs<KeyPayload> args)
    {
        if (NumpadStorage.NumpadParams != null)
        {
            switch (args.Action)
            {
                case "tech.flighttracker.streamdeck.number.enter":
                    if (NumpadStorage.NumpadTcs != null)
                    {
                        NumpadStorage.NumpadTcs.SetResult((NumpadStorage.NumpadParams.Value, false));
                    }
                    await StreamDeck.SwitchToProfileAsync(registrationParameters.PluginUUID, args.Device);
                    break;
                case "tech.flighttracker.streamdeck.number.cancel":
                    if (NumpadStorage.NumpadTcs != null)
                    {
                        NumpadStorage.NumpadTcs.SetResult((null, false));
                    }
                    await StreamDeck.SwitchToProfileAsync(registrationParameters.PluginUUID, args.Device);
                    break;
                case "tech.flighttracker.streamdeck.number.transfer":
                    if (NumpadStorage.NumpadParams.Type == "XPDR")
                    {
                        NumpadStorage.NumpadParams.Value = "1200";
                        NumpadStorage.NumpadTcs?.SetResult((NumpadStorage.NumpadParams.Value, false));
                    }
                    else
                    {
                        NumpadStorage.NumpadTcs?.SetResult((NumpadStorage.NumpadParams.Value, true));
                    }
                    await StreamDeck.SwitchToProfileAsync(registrationParameters.PluginUUID, args.Device);
                    break;
                case "tech.flighttracker.streamdeck.number.backspace":
                    if (NumpadStorage.NumpadParams.Value.Length > 0)
                    {
                        NumpadStorage.NumpadParams.Value = NumpadStorage.NumpadParams.Value[..^1];
                    }
                    break;
            }
        }
    }
}
