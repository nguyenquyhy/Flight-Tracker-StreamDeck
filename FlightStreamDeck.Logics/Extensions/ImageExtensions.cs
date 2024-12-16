using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;

namespace FlightStreamDeck.Logics;

public static class ImageExtensions
{
    public static string ToBase64PNG(this Image image)
    {
        using var memoryStream = new MemoryStream();
        image.Save(memoryStream, new PngEncoder());
        var base64 = Convert.ToBase64String(memoryStream.ToArray());

        return "data:image/png;base64, " + base64;
    }
}
