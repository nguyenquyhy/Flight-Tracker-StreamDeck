using System.Threading;

namespace FlightStreamDeck.Logics.Actions;

public interface IPresetToggleLogic
{
    bool GetActive(AircraftStatus status);
    void Toggle(AircraftStatus status);
    bool IsChanged(AircraftStatus? oldStatus, AircraftStatus newStatus);
}

public interface IPresetValueLogic : IPresetToggleLogic
{
    double? GetValue(AircraftStatus status);
    void ChangeValue(AircraftStatus status, int sign, int increment);
    void Sync(AircraftStatus status);
}

public abstract class PresetBaseValueLogic : PresetBaseToggleLogic, IPresetValueLogic
{
    private double? cachedValue = null;
    private CancellationTokenSource? cts = null;

    public PresetBaseValueLogic(IFlightConnector flightConnector) : base(flightConnector)
    {
    }

    public abstract double? GetValue(AircraftStatus status);

    public override bool IsChanged(AircraftStatus? oldStatus, AircraftStatus newStatus)
        => oldStatus == null ? true : (GetActive(oldStatus) != GetActive(newStatus) || GetValue(oldStatus) != GetValue(newStatus));

    public void ChangeValue(AircraftStatus status, int sign, int increment)
    {
        if (cachedValue == null)
        {
            var value = GetValue(status);
            cachedValue ??= value;
        }
        if (cachedValue != null)
        {
            var newValue = CalculateNewValue(cachedValue.Value, sign, increment);
            UpdateValue(newValue);

            // Clear cache after 500ms
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var token = cts.Token;
            Task.Run(async () =>
            {
                await Task.Delay(500);
                if (!token.IsCancellationRequested)
                {
                    cachedValue = null;
                }
            });
        }
    }

    protected void UpdateValue(double value)
    {
        cachedValue = value;
        UpdateSimValue(value);
    }

    protected abstract double CalculateNewValue(double currentValue, int sign, int increment);

    protected abstract void UpdateSimValue(double value);

    public virtual void Sync(AircraftStatus status) { }
}

public abstract class PresetBaseToggleLogic : IPresetToggleLogic
{
    protected readonly IFlightConnector flightConnector;

    public PresetBaseToggleLogic(IFlightConnector flightConnector)
    {
        this.flightConnector = flightConnector;
    }

    public abstract bool GetActive(AircraftStatus status);

    public abstract void Toggle(AircraftStatus status);

    public virtual bool IsChanged(AircraftStatus? oldStatus, AircraftStatus newStatus)
        => oldStatus == null ? true : (GetActive(oldStatus) != GetActive(newStatus));
}
