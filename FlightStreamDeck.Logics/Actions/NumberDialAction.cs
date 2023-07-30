using SharpDeck.Layouts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using System.Diagnostics;

namespace FlightStreamDeck.Logics.Actions;

public class NumberDialActionSettings
{

}

[StreamDeckAction("tech.flighttracker.streamdeck.number.dial.outer")]
public class NumberDialOuterAction : NumberDialAction
{
    public NumberDialOuterAction(IImageLogic imageLogic) : base(imageLogic)
    {
    }
}

[StreamDeckAction("tech.flighttracker.streamdeck.number.dial.inner")]
public class NumberDialInnerAction : NumberDialAction
{
    public NumberDialInnerAction(IImageLogic imageLogic) : base(imageLogic)
    {
    }
}

public abstract class NumberDialAction : BaseAction<NumberDialActionSettings>
{
    private readonly IImageLogic imageLogic;

    public NumberDialAction(IImageLogic imageLogic)
    {
        this.imageLogic = imageLogic;
    }

    public override Task InitializeSettingsAsync(NumberDialActionSettings? settings)
    {
        return Task.CompletedTask;
    }

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        if (NumpadStorage.NumpadParams != null)
        {
            if (string.IsNullOrEmpty(NumpadStorage.NumpadParams.Value))
            {
                NumpadStorage.NumpadParams.Value = NumpadStorage.NumpadParams?.CurrentValue?.Replace(".", "") ?? NumpadStorage.NumpadParams.MinPattern;
            }
            switch (args.Action)
            {
                case "tech.flighttracker.streamdeck.number.dial.outer":
                    await SetFeedbackSafeAsync(new LayoutA0
                    {
                        FullCanvas = "Images/dialouter.png",
                        Title = "Outer"
                    });
                    break;
                case "tech.flighttracker.streamdeck.number.dial.inner":
                    await SetFeedbackSafeAsync(new LayoutA0
                    {
                        FullCanvas = "Images/dialinner.png",
                        Title = "Inner"
                    });
                    break;
            }
        }
    }

    protected override Task OnDialRotate(ActionEventArgs<DialRotatePayload> args)
    {
        if (NumpadStorage.NumpadParams?.Value is string value)
        {
            var front = value[..3];
            var back = value[3..];

            switch (args.Action)
            {
                case "tech.flighttracker.streamdeck.number.dial.outer":
                    front = (int.Parse(front) + args.Payload.Ticks).ToString();

                    if (front.CompareTo(NumpadStorage.NumpadParams.MaxPattern[..3]) > 0) front = NumpadStorage.NumpadParams.MinPattern[..3];
                    if (front.CompareTo(NumpadStorage.NumpadParams.MinPattern[..3]) < 0) front = NumpadStorage.NumpadParams.MaxPattern[..3];
                    break;
                case "tech.flighttracker.streamdeck.number.dial.inner":
                    if (back.Length == 2)
                    {
                        back = ((int.Parse(back) + 5 * args.Payload.Ticks + 100) % 100).ToString("00");
                    }
                    else if (back.Length == 3)
                    {
                        var backVal = int.Parse(back) + 5 * args.Payload.Ticks;
                        if (backVal % 100 == 20 || backVal % 100 == 45 || backVal % 100 == 70 || backVal % 100 == 95)
                        {
                            backVal += args.Payload.Ticks > 0 ? 5 : -5;
                        }
                        back = ((backVal + 1000) % 1000).ToString("000");
                    }
                    break;
            }
            
            NumpadStorage.NumpadParams.Value = front + back;
        }
        return Task.CompletedTask;
    }
}
