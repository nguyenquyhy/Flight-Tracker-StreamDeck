using Microsoft.Win32;
using System;
using System.IO;

namespace FlightStreamDeck.Logics;

public class EmbedLinkLogic
{
    private readonly IAction action;

    public interface IAction
    {
        Task RefreshSettingsAsync();
        string? GetImagePath(string fileKey);
        string? GetImageBase64(string fileKey);
        void SetImagePath(string fileKey, string path);
        void SetImageBase64(string fileKey, string? base64);
    }

    public EmbedLinkLogic(IAction action)
    {
        this.action = action;
    }

    public async Task ConvertLinkToEmbedAsync(string? fileKey)
    {
        if (fileKey != null)
        {
            var imagePath = action.GetImagePath(fileKey);
            if (File.Exists(imagePath))
            {
                action.SetImageBase64(fileKey, Convert.ToBase64String(File.ReadAllBytes(imagePath)));
                await action.RefreshSettingsAsync();
            }
        }
    }

    public Task ConvertEmbedToLinkAsync(string? fileKey)
    {
        if (fileKey != null)
        {
            return System.Windows.Application.Current.Dispatcher.Invoke(async () =>
            {
                var imagePath = action.GetImagePath(fileKey);
                var base64 = action.GetImageBase64(fileKey);
                if (imagePath != null && base64 != null)
                {
                    var dialog = new SaveFileDialog
                    {
                        FileName = Path.GetFileName(imagePath),
                        Filter = "Images|*.jpg;*.jpeg;*.png"
                    };
                    if (dialog.ShowDialog() == true)
                    {
                        var bytes = Convert.FromBase64String(base64);
                        if (bytes != null)
                        {
                            File.WriteAllBytes(dialog.FileName, bytes);
                            action.SetImageBase64(fileKey, null);
                            action.SetImagePath(fileKey, dialog.FileName.Replace("\\", "/"));
                        }
                        await action.RefreshSettingsAsync();
                    }
                }
            });
        }

        return Task.CompletedTask;
    }
}
