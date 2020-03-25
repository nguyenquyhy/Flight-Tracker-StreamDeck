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
        string GetImage(string text, bool active, string value = null);
        string GetNumberImage(int number);
        string GetNavComImage(string type, string value1 = null, string value2 = null);
    }

    public class ImageLogic : IImageLogic
    {
        readonly Image backGround = Image.Load("Images/button.png");
        readonly Image activeBackground = Image.Load("Images/button_active.png");

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Base64 image data</returns>
        public string GetImage(string text, bool active, string value = null)
        {
            var font = SystemFonts.CreateFont("Arial", 17, FontStyle.Regular);
            var valueFont = SystemFonts.CreateFont("Arial", 15, FontStyle.Regular);
            bool hasValue = value != null && value.Length > 0;

            Image img = active && !hasValue ? activeBackground : backGround;
            using var img2 = img.Clone(ctx =>
            {
                var imgSize = ctx.GetCurrentSize();
                var size = TextMeasurer.Measure(text, new RendererOptions(font));
                ctx.DrawText(text, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 4));

                if (hasValue)
                {
                    size = TextMeasurer.Measure(value, new RendererOptions(valueFont));
                    ctx.DrawText(value, valueFont, active ? Color.Yellow : Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, 46));
                }
            });
            using var memoryStream = new MemoryStream();
            img2.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }

        /// <returns>Base64 image data</returns>
        public string GetNumberImage(int number)
        {
            var font = SystemFonts.CreateFont("Arial", 20, FontStyle.Bold);

            var text = number.ToString();
            Image img = backGround;
            using var img2 = img.Clone(ctx =>
            {
                var imgSize = ctx.GetCurrentSize();
                var size = TextMeasurer.Measure(text, new RendererOptions(font));
                ctx.DrawText(text, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 2 - size.Height / 2));
            });
            using var memoryStream = new MemoryStream();
            img2.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }

        public string GetNavComImage(string type, string value1 = null, string value2 = null)
        {
            var font = SystemFonts.CreateFont("Arial", 17, FontStyle.Regular);
            var valueFont = SystemFonts.CreateFont("Arial", 13, FontStyle.Regular);

            Image img = backGround;
            using var img2 = img.Clone(ctx =>
            {
                var imgSize = ctx.GetCurrentSize();

                if (type != null)
                {
                    var size = TextMeasurer.Measure(type, new RendererOptions(font));
                    ctx.DrawText(type, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 4));
                }

                if (!string.IsNullOrWhiteSpace(value1))
                {
                    var size1 = TextMeasurer.Measure(value1, new RendererOptions(valueFont));
                    ctx.DrawText(value1, valueFont, Color.Yellow, new PointF(imgSize.Width / 2 - size1.Width / 2, imgSize.Height / 2));
                }
                if (!string.IsNullOrWhiteSpace(value2))
                {
                    var size2 = TextMeasurer.Measure(value2, new RendererOptions(valueFont));
                    ctx.DrawText(value2, valueFont, Color.White, new PointF(imgSize.Width / 2 - size2.Width / 2, imgSize.Height / 2 + size2.Height + 2));
                }
            });
            using var memoryStream = new MemoryStream();
            img2.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }
    }
}
