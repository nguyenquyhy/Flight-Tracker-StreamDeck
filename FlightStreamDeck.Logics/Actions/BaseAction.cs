using SharpDeck;
using System;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace FlightStreamDeck.Logics.Actions
{
    public abstract class BaseAction<TSettings> : StreamDeckAction<TSettings> where TSettings : class
    {
        protected TSettings? settings = null;

        public abstract Task InitializeSettingsAsync(TSettings settings);

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
}
