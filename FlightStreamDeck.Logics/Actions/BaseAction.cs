using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace FlightStreamDeck.Logics.Actions;

public abstract class BaseAction<TSettings>(RegistrationParameters registrationParameters) : StreamDeckAction<TSettings> where TSettings : class
{
    protected readonly RegistrationParameters registrationParameters = registrationParameters;

    protected TSettings? settings = null;

    protected IdentifiableDeviceInfo? device;

    protected override async Task OnWillAppear(ActionEventArgs<AppearancePayload> args)
    {
        await base.OnWillAppear(args);

        device = registrationParameters.Info.Devices.FirstOrDefault(o => o.Id == args.Device);
    }

    public abstract Task InitializeSettingsAsync(TSettings? settings);

    public async Task RefreshSettingsAsync()
    {
        if (settings != null)
        {
            await SetSettingsAsync(settings);
            await SendToPropertyInspectorAsync(new
            {
                Action = "refresh",
                Settings = settings
            });
            await InitializeSettingsAsync(settings);
        }
    }

    public async Task SetImageSafeAsync(string base64Image)
    {
        try
        {
            await SetImageAsync(base64Image);
        }
        catch (SocketException)
        {
            // Ignore as we can't really do anything here
        }
        catch (WebSocketException)
        {
            // Ignore as we can't really do anything here
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }

    public async Task SetFeedbackSafeAsync(object feedback)
    {
        try
        {
            await SetFeedbackAsync(feedback);
        }
        catch (SocketException)
        {
            // Ignore as we can't really do anything here
        }
        catch (WebSocketException)
        {
            // Ignore as we can't really do anything here
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }

    /// <summary>
    /// Gets the stack responsible for monitoring dial interactions; this is used to determine if the press was a long-press.
    /// </summary>
    private ConcurrentStack<ActionEventArgs<DialPayload>> DialPressStack { get; } = new();

    protected virtual Task OnDialShortPress(ActionEventArgs<DialPayload> args) => Task.CompletedTask;

    protected virtual Task OnDialLongPress(ActionEventArgs<DialPayload> args) => Task.CompletedTask;

    protected override Task OnDialPress(ActionEventArgs<DialPayload> args)
    {
        if (args.Payload.Pressed)
        {
            DialPressStack.Push(args);
            if (LongKeyPressInterval > TimeSpan.Zero)
            {
                Task.Run(async delegate
                {
                    await Task.Delay(LongKeyPressInterval);
                    TryHandleDialPress(OnDialLongPress);
                });
            }
        }
        else
        {
            TryHandleDialPress(OnDialShortPress);
        }
        return Task.CompletedTask;
    }

    private void TryHandleDialPress(Func<ActionEventArgs<DialPayload>, Task> handler)
    {
        if (DialPressStack.TryPop(out var result))
        {
            handler(result);
        }
    }
}

public abstract class BaseAction : StreamDeckAction
{
    public async Task SetImageSafeAsync(string? base64Image)
    {
        try
        {
            await SetImageAsync(base64Image);
        }
        catch (SocketException)
        {
            // Ignore as we can't really do anything here
        }
        catch (WebSocketException)
        {
            // Ignore as we can't really do anything here
        }
        catch (ObjectDisposedException)
        {
            // Ignore
        }
    }
}
