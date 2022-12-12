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
        if (DeckLogic.NumpadParams?.Type == "XPDR")
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
        if (DeckLogic.NumpadParams != null)
        {
            switch (args.Action)
            {
                case "tech.flighttracker.streamdeck.number.enter":
                    if (DeckLogic.NumpadTcs != null)
                    {
                        DeckLogic.NumpadTcs.SetResult((DeckLogic.NumpadParams.Value, false));
                    }
                    await StreamDeck.SwitchToProfileAsync(registrationParameters.PluginUUID, args.Device);
                    break;
                case "tech.flighttracker.streamdeck.number.cancel":
                    if (DeckLogic.NumpadTcs != null)
                    {
                        DeckLogic.NumpadTcs.SetResult((null, false));
                    }
                    await StreamDeck.SwitchToProfileAsync(registrationParameters.PluginUUID, args.Device);
                    break;
                case "tech.flighttracker.streamdeck.number.transfer":
                    if (DeckLogic.NumpadParams.Type == "XPDR")
                    {
                        DeckLogic.NumpadParams.Value = "1200";
                        DeckLogic.NumpadTcs?.SetResult((DeckLogic.NumpadParams.Value, false));
                    }
                    else
                    {
                        DeckLogic.NumpadTcs?.SetResult((DeckLogic.NumpadParams.Value, true));
                    }
                    await StreamDeck.SwitchToProfileAsync(registrationParameters.PluginUUID, args.Device);
                    break;
                case "tech.flighttracker.streamdeck.number.backspace":
                    if (DeckLogic.NumpadParams.Value.Length > 0)
                    {
                        DeckLogic.NumpadParams.Value = DeckLogic.NumpadParams.Value[..^1];
                    }
                    break;
            }
        }
    }
}
