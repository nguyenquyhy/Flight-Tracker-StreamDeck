using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;

namespace FlightStreamDeck.Logics;

public interface IImageLogic
{
    string GetImage(string text, bool active, string? value = null, int? fontSize = null, string? imageOnFilePath = null, byte[]? imageOnBytes = null, string? imageOffFilePath = null, byte[]? imageOffBytes = null, bool highResolution = false);
    string GetNumberImage(int number);
    string GetNavComImage(string type, bool dependant, string value1, string value2, bool showMainOnly = false, string? imageOnFilePath = null, byte[]? imageOnBytes = null, bool highResolution = false);
    public string GetHorizonImage(double pitchInDegrees, double rollInDegrees);
    public string GetGenericGaugeImage(string text, double value, double min, double max, int? fontSize, string valueFormat, string? subValueText = null);
    public string GetCustomGaugeImage(string textTop, string textBottom, double valueTop, double valueBottom, double min, double max, string valueFormat, bool horizontal, string[] chartSplits, int chartWidth, float chevronSize, bool absoluteValueText, bool hideHeaderTop, bool hideHeaderBottom);
    public string GetWindImage(double windDirectionInDegrees, double windVelocity, double headingInDegrees, bool relative);
}

public class ImageLogic : IImageLogic
{
    readonly Image defaultBackground = Image.Load("Images/button.png");
    readonly Image defaultActiveBackground = Image.Load("Images/button_active.png");
    readonly Image defaultBackgroundHighResolution = Image.Load("Images/button@2x.png");
    readonly Image defaultActiveBackgroundHighResolution = Image.Load("Images/button_active@2x.png");
    readonly Image horizon = Image.Load("Images/horizon.png");
    readonly Image gaugeImage = Image.Load("Images/gauge.png");
    readonly Image windImage = Image.Load("Images/wind.png");

    private const int WIDTH = 72;
    private const int HALF_WIDTH = 36;

    /// <summary>
    /// NOTE: either filePath or bytes should be set at the same time
    /// </summary>
    /// <returns>Base64 image data</returns>
    public string GetImage(string text, bool active, string? value = null,
        int? fontSize = null,
        string? imageOnFilePath = null, byte[]? imageOnBytes = null,
        string? imageOffFilePath = null, byte[]? imageOffBytes = null,
        bool highResolution = false)
    {
        var font = SystemFonts.CreateFont("Arial", 17, FontStyle.Regular);
        var valueFont = SystemFonts.CreateFont("Arial", fontSize ?? 15, FontStyle.Regular);

        // Note: logic to choose with image to show
        // 1. If user did not select custom images, the active image (with light) is used
        //    only when Feedback value is true AND Display value is empty.
        // 2. If user select custom images (esp Active one), the custom Active image is used based on Feedback value
        //    ignoring Display value.
        using var img = active ?
            GetBackgroundImage(imageOnBytes, imageOnFilePath, () => (string.IsNullOrEmpty(value) ? getDefaultActiveBackground(highResolution) : getDefaultBackground(highResolution)).Clone(_ => { })) :
            GetBackgroundImage(imageOffBytes, imageOffFilePath, () => getDefaultBackground(highResolution).Clone(_ => { }));

        var width = highResolution ? WIDTH * 2 : WIDTH;

        img.Mutate(ctx =>
        {
            ctx.Resize(width, width); // Force image to rescale to our button size, otherwise text gets super small if it is bigger.

            var imgSize = ctx.GetCurrentSize();

            // Calculate scaling for header
            var smallerDim = imgSize.Width < imgSize.Height ? imgSize.Width : imgSize.Height;
            var scale = 1f;
            if (smallerDim != width)
            {
                scale = (float)smallerDim / width;
            }
            if (highResolution)
            {
                scale *= 2;
            }
            
            if (scale != 1)
            {
                font = new Font(font, font.Size * scale);
                valueFont = new Font(valueFont, valueFont.Size * scale);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                var size = TextMeasurer.MeasureSize(text, new TextOptions(font));
                ctx.DrawText(text, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 4));
            }

            if (!string.IsNullOrEmpty(value))
            {
                var size = TextMeasurer.MeasureSize(value, new TextOptions(valueFont));
                ctx.DrawText(value, valueFont, active ? Color.Yellow : Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, 64 * scale - size.Height));
            }
        });

        return img.ToBase64PNG();
    }

    /// <returns>Base64 image data</returns>
    public string GetNumberImage(int number)
    {
        var font = SystemFonts.CreateFont("Arial", 20, FontStyle.Bold);

        var text = number.ToString();
        using var img = defaultBackground.Clone(ctx =>
        {
            var imgSize = ctx.GetCurrentSize();
            var size = TextMeasurer.MeasureSize(text, new TextOptions(font));
            ctx.DrawText(text, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 2 - size.Height / 2));
        });

        return img.ToBase64PNG();
    }

    public string GetNavComImage(
        string type, bool dependant, string value1, string value2, bool showMainOnly = false, 
        string? imageFilePath = null, byte[]? imageBytes = null,
        bool highResolution = false)
    {
        var font = SystemFonts.CreateFont("Arial", 17, FontStyle.Regular);
        var valueFont = SystemFonts.CreateFont("Arial", showMainOnly ? 26 : 15, FontStyle.Regular);

        using var img = GetBackgroundImage(imageBytes, imageFilePath, () => getDefaultBackground(highResolution).Clone(_ => { }));

        var width = highResolution ? WIDTH * 2 : WIDTH;

        img.Mutate(ctx =>
        {
            ctx.Resize(width, width); // Force image to rescale to our button size, otherwise text gets super small if it is bigger.

            var imgSize = ctx.GetCurrentSize();

            // Calculate scaling for header
            var smallerDim = imgSize.Width < imgSize.Height ? imgSize.Width : imgSize.Height;
            var scale = 1f;
            if (smallerDim != width)
            {
                scale = (float)smallerDim / width;
            }
            if (highResolution)
            {
                scale *= 2;
            }

            if (scale != 1)
            {
                font = new Font(font, font.Size * scale);
                valueFont = new Font(valueFont, valueFont.Size * scale);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                var size = TextMeasurer.MeasureSize(type, new TextOptions(font));
                Color displayColor = dependant ? Color.White : Color.LightGray;
                ctx.DrawText(type, font, displayColor, new PointF(imgSize.Width / 2 - size.Width / 2, showMainOnly ? imgSize.Height / 4 : imgSize.Height / 6));
            }

            if (!string.IsNullOrWhiteSpace(value1))
            {
                var size1 = TextMeasurer.MeasureSize(value1, new TextOptions(valueFont));
                Color displayColor = dependant ? Color.Yellow : Color.LightGray;
                ctx.DrawText(value1, valueFont, displayColor, new PointF(imgSize.Width / 2 - size1.Width / 2, showMainOnly ? (imgSize.Height / 2) : (imgSize.Height / 6 + imgSize.Height / 4)));
            }
            if (!string.IsNullOrWhiteSpace(value2) && !showMainOnly)
            {
                var size2 = TextMeasurer.MeasureSize(value2, new TextOptions(valueFont));
                Color displayColor = dependant ? Color.White : Color.LightGray;
                ctx.DrawText(value2, valueFont, displayColor, new PointF(imgSize.Width / 2 - size2.Width / 2, imgSize.Height / 6 + imgSize.Height / 4 + size2.Height + 5));
            }
        });

        return img.ToBase64PNG();
    }

    public string GetHorizonImage(double pitchInDegrees, double rollInDegrees)
    {
        var pen = new SolidPen(Color.Yellow, 3);

        using var shiftedRolledHorizon = new Image<Rgba32>(105, 105);
        shiftedRolledHorizon.Mutate(ctx =>
        {
            var size = horizon.Size;
            ctx.DrawImage(horizon, new Point(
                (int)Math.Round((float)-size.Width / 2 + 52),
                Math.Clamp((int)Math.Round((float)-size.Height / 2 + 52 - (pitchInDegrees * 2)), -size.Height + 50, 55)
                ), new GraphicsOptions());
            ctx.Rotate((float)rollInDegrees);
        });

        using var img = new Image<Rgba32>(WIDTH, WIDTH);
        img.Mutate(ctx =>
        {
            var size = shiftedRolledHorizon.Size;
            ctx.DrawImage(shiftedRolledHorizon, new Point(
                (int)Math.Round((float)-size.Width / 2 + HALF_WIDTH),
                (int)Math.Round((float)-size.Height / 2 + HALF_WIDTH)
                ), new GraphicsOptions());

            // Draw bug
            PointF[] leftLine = { new PointF(6, 36), new PointF(26, 36) };
            PointF[] rightLine = { new PointF(46, 36), new PointF(66, 36) };
            PointF[] bottomLine = { new PointF(36, 41), new PointF(36, 51) };
            ctx.DrawLine(pen, leftLine);
            ctx.DrawLine(pen, rightLine);
            ctx.DrawLine(pen, bottomLine);
        });

        return img.ToBase64PNG();
    }

    public string GetWindImage(double windDirectionInDegrees, double windVelocity, double headingInDegrees, bool relative)
    {
        var fontDegree = SystemFonts.CreateFont("Arial", 16, FontStyle.Regular);
        var fontStrength = SystemFonts.CreateFont("Arial", 16, FontStyle.Regular);

        int intDir = Convert.ToInt32(windDirectionInDegrees);
        int intVel = Convert.ToInt32(windVelocity);
        int intHdg = Convert.ToInt32(headingInDegrees);
        string txtDir = intDir.ToString();

        if (relative)
        {
            intDir = intDir - intHdg;
        }

        using var rotatedImg = windImage.Clone(ctx =>
        {
            // Rotate
            ctx.Rotate((float)intDir);
        });

        using var img = new Image<Rgba32>(WIDTH, WIDTH);
        img.Mutate(ctx =>
        {
            var size = rotatedImg.Size;
            ctx.DrawImage(rotatedImg, new Point(
                (int)Math.Round((float)-size.Width / 2 + HALF_WIDTH),
                (int)Math.Round((float)-size.Height / 2 + HALF_WIDTH)
                ), new GraphicsOptions());

            FontRectangle fsize = new FontRectangle(0, 0, 0, 0);
            fsize = TextMeasurer.MeasureSize(txtDir, new TextOptions(fontDegree));
            ctx.DrawText(txtDir + "°", fontDegree, Color.Yellow, new PointF(HALF_WIDTH - fsize.Width / 2, 53));

            string text = intVel.ToString() + " kt";
            fsize = TextMeasurer.MeasureSize(text, new TextOptions(fontStrength));
            ctx.DrawText(text, fontStrength, Color.Cyan, new PointF(HALF_WIDTH - fsize.Width / 2, 0));


            var pen = new SolidPen(Color.Cyan, 4);
            var penSmall = new SolidPen(Color.Cyan, 3);

            if (relative)
            {
                PointF[] wingLine = { new PointF(1, 59), new PointF(17, 59) };
                PointF[] middleLine = { new PointF(9, 54), new PointF(9, 68) };
                PointF[] rudderLine = { new PointF(5, 67), new PointF(13, 67) };
                ctx.DrawLine(pen, middleLine);
                ctx.DrawLine(pen, wingLine);
                ctx.DrawLine(penSmall, rudderLine);
            }
            else
            {
                text = "N";
                fsize = TextMeasurer.MeasureSize(text, new TextOptions(fontDegree));
                ctx.DrawText(text, fontDegree, Color.Cyan, new PointF(3, 53));
            }

        });

        return img.ToBase64PNG();
    }

    public string GetGenericGaugeImage(string text, double value, double min, double max, int? fontSize, string valueFormat, string? subValueText = null)
    {
        var font = SystemFonts.CreateFont("Arial", fontSize ?? 22, FontStyle.Regular);
        var titleFont = SystemFonts.CreateFont("Arial", 13, FontStyle.Regular);
        var pen = new SolidPen(Color.DarkRed, 5);
        var range = max - min;

        if (range <= 0)
        {
            range = 1;
        }

        using var img = gaugeImage.Clone(ctx =>
        {
            double angleOffset = Math.PI * -1.25;
            var ratio = (value - min) / range;
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;
            double angle = Math.PI * ratio + angleOffset;

            var startPoint = new PointF(HALF_WIDTH, HALF_WIDTH);
            var middlePoint = new PointF(
                (float)((HALF_WIDTH - 16) * Math.Cos(angle)),
                (float)((HALF_WIDTH - 16) * Math.Sin(angle))
                );

            var endPoint = new PointF(
                (float)(HALF_WIDTH * Math.Cos(angle)),
                (float)(HALF_WIDTH * Math.Sin(angle))
                );

            PointF[] needle = { startPoint + middlePoint, startPoint + endPoint };

            ctx.DrawLine(pen, needle);

            FontRectangle size = new FontRectangle(0, 0, 0, 0);
            if (!string.IsNullOrWhiteSpace(text))
            {
                size = TextMeasurer.MeasureSize(text, new TextOptions(titleFont));
                ctx.DrawText(text, titleFont, Color.White, new PointF(HALF_WIDTH - size.Width / 2, 43));
            }

            var valueText = value.ToString(valueFormat);
            var sizeValue = TextMeasurer.MeasureSize(valueText, new TextOptions(font));
            var textColor = value > max ? Color.Red : Color.White;
            ctx.DrawText(valueText, font, textColor, new PointF(18, 46 - sizeValue.Height));

            if (!string.IsNullOrWhiteSpace(subValueText)) ctx.DrawText(subValueText, titleFont, textColor, new PointF(20, 41 + size.Height));
        });

        return img.ToBase64PNG();
    }

    public string GetCustomGaugeImage(string textTop, string textBottom, double valueTop, double valueBottom, double min, double max, string valueFormat, bool horizontal, string[] splitGauge, int chartWidth, float chevronSize, bool displayAbsoluteValue, bool hideHeaderTop, bool hideHeaderBottom)
    {
        var font = SystemFonts.CreateFont("Arial", 25, FontStyle.Regular);
        var titleFont = SystemFonts.CreateFont("Arial", 15, FontStyle.Regular);

        if (max < min)
        {
            // Swap max and min
            var temp = max; max = min; min = temp;
        }

        var range = max - min;
        range = range == 0 ? 1 : range;

        using var img = defaultBackground.Clone(ctx =>
        {
            ctx.Draw(new SolidPen(Color.Black, 100), new RectangleF(0, 0, WIDTH, WIDTH));
            int width_margin = 10;
            int img_width = WIDTH - (width_margin * 2);

            //0 = critical : Red
            //1 = warning : Yellow
            //2 = nominal : Green
            //3 = superb : No Color
            Color[] colors = { Color.Red, Color.Yellow, Color.Green };
            PointF previousWidth = new PointF(width_margin, HALF_WIDTH);
            int colorSentinel = 0;

            foreach (var pct in splitGauge)
            {
                string[] split = pct.Split(':');
                if (float.TryParse(split[0], out float critFloatWidth) && colors.Length > colorSentinel)
                {
                    PointF stepWidth = previousWidth + new SizeF(critFloatWidth / 100f * img_width, 0);

                    Color? color = null;
                    if (split.Length > 1 && split[1] != string.Empty)
                    {
                        System.Drawing.Color temp = System.Drawing.Color.FromName(split[1]);
                        color = Color.FromRgb(temp.R, temp.G, temp.B);
                    }
                    else if (colors.Length > colorSentinel)
                    {
                        color = colors[colorSentinel];
                        colorSentinel += 1;
                    }

                    if (color != null)
                    {
                        var shift = new SizeF(0, chartWidth / 2f);
                        ctx.FillPolygon(
                            color.Value,
                            previousWidth - shift,
                            previousWidth + shift,
                            stepWidth + shift,
                            stepWidth - shift
                        );
                    }

                    previousWidth = stepWidth;
                }
            }

            //topValue
            var ratio = (valueTop - min) / range;
            var valueTopText = (displayAbsoluteValue ? Math.Abs(valueTop) : valueTop).ToString(valueFormat);
            DrawCustomGauge(true, textTop, valueTopText, (float)ratio, img_width, chevronSize, width_margin, chartWidth, (float)min, (float)max, ctx, hideHeaderTop);

            //bottomValue
            ratio = (valueBottom - min) / range;
            var valueBottomText = (displayAbsoluteValue ? Math.Abs(valueBottom) : valueBottom).ToString(valueFormat);
            DrawCustomGauge(false, textBottom, valueBottomText, (float)ratio, img_width, chevronSize, width_margin, chartWidth, (float)min, (float)max, ctx, hideHeaderBottom);

            if (!horizontal) ctx.Rotate(-90);
        });

        return img.ToBase64PNG();
    }

    private Image getDefaultActiveBackground(bool highResolution) => highResolution ? defaultActiveBackgroundHighResolution : defaultActiveBackground;
    private Image getDefaultBackground(bool highResolution) => highResolution ? defaultBackgroundHighResolution : defaultBackground;

    private Image GetBackgroundImage(byte[]? imageBytes, string? imageFilePath, Func<Image> imageDefaultFactory)
    {
        if (imageBytes != null && imageBytes.Length > 0)
        {
            try
            {
                return Image.Load(imageBytes);
            }
            catch (ImageFormatException)
            {
                // Let it fall through to default image
                // TODO: maybe show a warning background
            }
        }
        else if (!string.IsNullOrEmpty(imageFilePath) && File.Exists(imageFilePath))
        {
            try
            {
                return Image.Load(imageFilePath);
            }
            catch (ImageFormatException)
            {
                // Let it fall through to default image
                // TODO: maybe show a warning background
            }
        }

        return imageDefaultFactory();
    }

    private void DrawCustomGauge(bool top, string labelText, string value, float ratio, int img_width, float chevronSize, float width_margin, float chart_width, float min, float max, IImageProcessingContext ctx, bool hideHeader)
    {
        float.TryParse(value, out float floatValue);
        bool missingHeaderLabel = (labelText?.Length ?? 0) == 0;
        bool writeValueHeaderAndChevron = !hideHeader || (floatValue >= min && floatValue <= max);

        if (writeValueHeaderAndChevron && !missingHeaderLabel)
        {
            var pen = new SolidPen(Color.White, chevronSize + 1);

            var arrowStartX = (ratio * img_width) + width_margin;
            var arrowStartY = (HALF_WIDTH - ((chart_width / 2) * (top ? 1 : -1)));
            var arrowAddY = arrowStartY - ((chevronSize * 2) * (top ? 1 : -1));

            var startPoint = new PointF(arrowStartX, arrowStartY);
            var right = new PointF(arrowStartX + chevronSize, arrowAddY);
            var left = new PointF(arrowStartX - chevronSize, arrowAddY);

            PointF[] needle = { startPoint, right, left, startPoint };

            var valueText = value.ToString();
            var textColor = (floatValue > max || floatValue < min) ? Color.Red : Color.White;
            var font = SystemFonts.CreateFont("Arial", chevronSize * 4, FontStyle.Regular);

            var size = TextMeasurer.MeasureSize(valueText, new TextOptions(font));
            float adjustY = top ? Math.Abs(-5 - size.Height) : 5;
            arrowAddY = top ? arrowAddY - adjustY : arrowAddY + adjustY;
            var valuePoint = new PointF(HALF_WIDTH - size.Width / 2, arrowAddY);
            ctx.DrawText(valueText, font, textColor, valuePoint);

            ctx.DrawPolygon(pen, needle);
            var text = !string.IsNullOrEmpty(labelText) ? labelText[0].ToString() : string.Empty;
            size = TextMeasurer.MeasureSize(text, new TextOptions(SystemFonts.CreateFont("Arial", chevronSize * 3, FontStyle.Regular)));
            startPoint.Y -= top ? size.Height : 0;
            startPoint.X -= size.Width / 2;
            ctx.DrawText(text, SystemFonts.CreateFont("Arial", chevronSize * 3, FontStyle.Regular), Color.Black, startPoint);
        }
    }
}
