using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.IO;

namespace FlightStreamDeck.Logics
{
    public interface IImageLogic
    {
        string DrawText(string imagePath, string text);
    }

    public class ImageLogic : IImageLogic
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>Base64 image data</returns>
        public string DrawText(string imagePath, string text)
        {
            var font = SystemFonts.CreateFont("Arial", 20, FontStyle.Bold);

            using var img = Image.Load(imagePath);
            using var img2 = img.Clone(ctx =>
            {
                var imgSize = ctx.GetCurrentSize();
                var size = TextMeasurer.Measure(text, new RendererOptions(font));

                ctx.DrawText(text, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 4));
            });
            using var memoryStream = new MemoryStream();
            img2.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }
    }
}
