using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;

namespace FlightStreamDeck.Logics
{
    public interface IImageLogic
    {
        string GetImage(string text, bool active, string value = null, string customActiveBackground = null, string customBackground = null);
        string GetNumberImage(int number);
        string GetNavComImage(string type, bool dependant, string value1 = null, string value2 = null, bool showMainOnly = false);
        public string GetHorizonImage(float pitchInDegrees, float rollInDegrees, float headingInDegrees);
        public string GetGenericGaugeImage(string text, float value, float min, float max, string valuePrecision, float subValue = float.MinValue);
        public string GetCustomGaugeImage(string textTop, string textBottom, string valueTop, string valueBottom, float min, float max, bool horizontal, string[] chartSplits, int chartWidth, float chevronSize, bool absoluteValueText, string valuePrecision, bool hideHeaderTop, bool hideHeaderBottom);
    }

    public class ImageLogic : IImageLogic
    {
        readonly Image defaultBackground = Image.Load("Images/button.png");
        readonly Image defaultActiveBackground = Image.Load("Images/button_active.png");   
        readonly Image horizon = Image.Load("Images/horizon.png");
        readonly Image gaugeImage = Image.Load("Images/gauge.png");

        private const int WIDTH = 72;
        private const int HALF_WIDTH = 36;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Base64 image data</returns>
        public string GetImage(string text, bool active, string value = null, string customActiveBackground = null, string customBackground = null)
        {
            var font = SystemFonts.CreateFont("Arial", 17, FontStyle.Regular);
            var valueFont = SystemFonts.CreateFont("Arial", 15, FontStyle.Regular);
            bool hasValue = value != null && value.Length > 0;

            // Note: logic to choose with image to show
            // 1. If user did not select custom images, the active image (with light) is used 
            //    only when Feedback value is true AND Display value is empty.
            // 2. If user select custom images (esp Active one), the custom Active image is used based on Feedback value 
            //    ignoring Display value. 
            Image img;
            if (active)
            {
                img = !string.IsNullOrEmpty(customActiveBackground) && File.Exists(customActiveBackground) ?
                    Image.Load(customActiveBackground) : (!hasValue ? defaultActiveBackground : defaultBackground);
            }
            else
            {
                img = !string.IsNullOrEmpty(customBackground) && File.Exists(customBackground) ?
                    Image.Load(customBackground) : defaultBackground;
            }

            using var img2 = img.Clone(ctx =>
            {
                var imgSize = ctx.GetCurrentSize();

                // Calculate scaling for header
                var smallerDim = imgSize.Width < imgSize.Height ? imgSize.Width : imgSize.Height;
                var scale = 1f;
                if (smallerDim != WIDTH)
                {
                    scale = (float)smallerDim / WIDTH;
                    font = new Font(font, font.Size * scale);
                    valueFont = new Font(valueFont, valueFont.Size * scale);
                }

                FontRectangle size;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    size = TextMeasurer.Measure(text, new RendererOptions(font));
                    ctx.DrawText(text, font, Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 4));
                }

                if (hasValue)
                {
                    size = TextMeasurer.Measure(value, new RendererOptions(valueFont));
                    ctx.DrawText(value, valueFont, active ? Color.Yellow : Color.White, new PointF(imgSize.Width / 2 - size.Width / 2, 46 * scale));
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
            Image img = defaultBackground;
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

        public string GetNavComImage(string type, bool dependant, string value1 = null, string value2 = null, bool showMainOnly = false)
        {
            var font = SystemFonts.CreateFont("Arial", 17, FontStyle.Regular);
            var valueFont = SystemFonts.CreateFont("Arial", showMainOnly ? 26 : 13, FontStyle.Regular);

            Image img = defaultBackground;
            using var img2 = img.Clone(ctx =>
            {
                var imgSize = ctx.GetCurrentSize();

                if (!string.IsNullOrWhiteSpace(type))
                {
                    var size = TextMeasurer.Measure(type, new RendererOptions(font));
                    Color displayColor = dependant ? Color.White : Color.LightGray;
                    ctx.DrawText(type, font, displayColor, new PointF(imgSize.Width / 2 - size.Width / 2, imgSize.Height / 4));
                }

                if (!string.IsNullOrWhiteSpace(value1))
                {
                    var size1 = TextMeasurer.Measure(value1, new RendererOptions(valueFont));
                    Color displayColor = dependant ? Color.Yellow : Color.LightGray;
                    ctx.DrawText(value1, valueFont, displayColor, new PointF(imgSize.Width / 2 - size1.Width / 2, imgSize.Height / 2));
                }
                if (!string.IsNullOrWhiteSpace(value2) && !showMainOnly)
                {
                    var size2 = TextMeasurer.Measure(value2, new RendererOptions(valueFont));
                    Color displayColor = dependant ? Color.White : Color.LightGray;
                    ctx.DrawText(value2, valueFont, displayColor, new PointF(imgSize.Width / 2 - size2.Width / 2, imgSize.Height / 2 + size2.Height + 2));
                }
            });
            using var memoryStream = new MemoryStream();
            img2.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }

        public string GetHorizonImage(float pitchInDegrees, float rollInDegrees, float headingInDegrees)
        {
            //var font = SystemFonts.CreateFont("Arial", 10, FontStyle.Regular);
            //var valueFont = SystemFonts.CreateFont("Arial", 12, FontStyle.Regular);
            var pen = new Pen(Color.Yellow, 3);

            var shiftedRolledHorizon = new Image<Rgba32>(105, 105);
            shiftedRolledHorizon.Mutate(ctx =>
            {
                var size = horizon.Size();
                ctx.DrawImage(horizon, new Point(
                    (int)Math.Round((float)-size.Width / 2 + 52),
                    (int)Math.Round((float)-size.Height / 2 + 52 - (pitchInDegrees * 2))
                    ), new GraphicsOptions());
                ctx.Rotate(rollInDegrees);
            });

            using (var img = new Image<Rgba32>(WIDTH, WIDTH))
            {
                img.Mutate(ctx =>
                {
                    var size = shiftedRolledHorizon.Size();
                    ctx.DrawImage(shiftedRolledHorizon, new Point(
                        (int)Math.Round((float)-size.Width / 2 + HALF_WIDTH),
                        (int)Math.Round((float)-size.Height / 2 + HALF_WIDTH)
                        ), new GraphicsOptions());

                    // Draw bug
                    PointF[] leftLine = { new PointF(6, 36), new PointF(26, 36) };
                    PointF[] rightLine = { new PointF(46, 36), new PointF(66, 36) };
                    PointF[] bottomLine = { new PointF(36, 41), new PointF(36, 51) };
                    ctx.DrawLines(pen, leftLine);
                    ctx.DrawLines(pen, rightLine);
                    ctx.DrawLines(pen, bottomLine);
                });

                using var memoryStream = new MemoryStream();
                img.Save(memoryStream, new PngEncoder());
                var base64 = Convert.ToBase64String(memoryStream.ToArray());

                return "data:image/png;base64, " + base64;
            }
        }

        public string GetGenericGaugeImage(string text, float value, float min, float max, string valuePrecision, float subValue = float.MinValue)
        {
            var font = SystemFonts.CreateFont("Arial", 22, FontStyle.Regular);
            var titleFont = SystemFonts.CreateFont("Arial", 13, FontStyle.Regular);
            var pen = new Pen(Color.DarkRed, 5);
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

                ctx.DrawLines(pen, needle);

                FontRectangle size = new FontRectangle(0, 0, 0, 0); 
                if (!string.IsNullOrWhiteSpace(text))
                {
                    size = TextMeasurer.Measure(text, new RendererOptions(titleFont));
                    ctx.DrawText(text, titleFont, Color.White, new PointF(HALF_WIDTH - size.Width / 2, 40));
                }

                var valueText = value.ToString(valuePrecision);
                var sizeValue = TextMeasurer.Measure(valueText, new RendererOptions(font));
                Color textColor = value > max ? Color.Red : Color.White;
                ctx.DrawText(valueText, font, textColor, new PointF(18, 20));

                if (subValue != float.MinValue) ctx.DrawText(subValue.ToString("F2"), titleFont, textColor, new PointF(18, 20 + sizeValue.Height + size.Height));
            });

            using var memoryStream = new MemoryStream();
            img.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }


        public string GetCustomGaugeImage(string textTop, string textBottom, string valueTop, string valueBottom, float min, float max, bool horizontal, string[] splitGauge, int chartWidth, float chevronSize, bool absoluteValueText, string valuePrecision, bool hideHeaderTop, bool hideHeaderBottom)
        {
            var font = SystemFonts.CreateFont("Arial", 25, FontStyle.Regular);
            var titleFont = SystemFonts.CreateFont("Arial", 15, FontStyle.Regular);
            var pen = new Pen(Color.DarkRed, 5);
            var range = Math.Abs(max - min);

            using var img = defaultBackground.Clone(ctx =>
            {
                ctx.Draw(new Pen(Color.Black, 100), new RectangleF(0, 0, WIDTH, WIDTH));
                int width_margin = 10;
                int img_width = WIDTH - (width_margin * 2);

                //0 = critical : Red
                //1 = warning : Yellow
                //2 = nominal : Green
                //3 = superb : No Color
                Color[] colors = { Color.Red, Color.Yellow, Color.Green };
                PointF? stepWidth = null, previousWidth = new PointF(width_margin, HALF_WIDTH);
                int colorSentinel = 0;

                splitGauge?.ToList().ForEach(pct => {
                    string[] split = pct.Split(':');
                    if (float.TryParse(split[0], out float critFloatWidth) && colors.Length > colorSentinel)
                    {
                        stepWidth = new PointF(((PointF)previousWidth).X + ((critFloatWidth / 100) * img_width), HALF_WIDTH);
                        PointF[] critical = { (PointF)previousWidth, (PointF)stepWidth };
                        Color? color = null;
                        if (split.Length > 1 && split[1] != string.Empty)
                        {
                            try
                            {
                                System.Drawing.Color temp = System.Drawing.Color.FromName(split[1]);
                                color = Color.FromRgb(temp.R, temp.G, temp.B);
                            } finally
                            {}
                        } else if (colors.Length > colorSentinel)
                        {
                            color = colors[colorSentinel];
                        }

                        if (color != null) ctx.DrawLines(new Pen((Color)color, chartWidth), critical);

                        previousWidth = stepWidth;
                    }
                    colorSentinel += 1;
                });

                //topValue
                float.TryParse(valueTop, out float floatValueTop);
                var ratio = (floatValueTop - (min < max ? min : max)) / range;
                valueTop = absoluteValueText ? Math.Abs(floatValueTop).ToString(valuePrecision) : valueTop;
                setupValue(true, textTop, valueTop, ratio, img_width, chevronSize, width_margin, chartWidth, min, max, ctx, hideHeaderTop);

                //bottomValue
                float.TryParse(valueBottom, out float floatValueBottom);
                ratio = (floatValueBottom - (min < max ? min : max)) / range;
                valueBottom = absoluteValueText ? Math.Abs(floatValueBottom).ToString(valuePrecision) : valueBottom;
                setupValue(false, textBottom, valueBottom, ratio, img_width, chevronSize, width_margin, chartWidth, min, max, ctx, hideHeaderBottom);

                if (!horizontal) ctx.Rotate(-90);
            });

            using var memoryStream = new MemoryStream();
            img.Save(memoryStream, new PngEncoder());
            var base64 = Convert.ToBase64String(memoryStream.ToArray());

            return "data:image/png;base64, " + base64;
        }

        private void setupValue(bool top, string labelText, string value, float ratio, int img_width, float chevronSize, float width_margin, float chart_width, float min, float max, IImageProcessingContext ctx, bool hideHeader)
        {

            float.TryParse(value, out float floatValue);
            bool missingHeaderLabel = (labelText?.Length ?? 0) == 0;
            bool writeValueHeaderAndChevron = !hideHeader || (hideHeader && !(floatValue < min || floatValue > max));

            if (writeValueHeaderAndChevron && !missingHeaderLabel)
            {
                var pen = new Pen(Color.White, chevronSize + 1);

                var arrowStartX = (ratio * img_width) + width_margin;
                var arrowStartY = (HALF_WIDTH - ((chart_width / 2) * (top ? 1 : -1)));
                var arrowAddY = arrowStartY - ((chevronSize * 2) * (top ? 1 : -1));

                var startPoint = new PointF(arrowStartX, arrowStartY);
                var right = new PointF(arrowStartX + chevronSize, arrowAddY);
                var left = new PointF(arrowStartX - chevronSize, arrowAddY);

                PointF[] needle = { startPoint, right, left, startPoint };

                var valueText = value.ToString();
                Color textColor = (floatValue > max && min < max) ? Color.Red : Color.White;
                var font = SystemFonts.CreateFont("Arial", chevronSize * 4, FontStyle.Regular);

                var size = TextMeasurer.Measure(valueText, new RendererOptions(font));
                float adjustY = top ? Math.Abs(-5 - size.Height) : 5;
                arrowAddY = top ? arrowAddY - adjustY : arrowAddY + adjustY;
                var valuePoint = new PointF(HALF_WIDTH - size.Width / 2, arrowAddY);
                ctx.DrawText(valueText, font, textColor, valuePoint);

                ctx.DrawPolygon(pen, needle);
                var text = labelText != string.Empty ? labelText[0].ToString() : string.Empty;
                size = TextMeasurer.Measure(text, new RendererOptions(SystemFonts.CreateFont("Arial", chevronSize * 3, FontStyle.Regular)));
                startPoint.Y -= top ? size.Height : 0;
                startPoint.X -= size.Width / 2;
                ctx.DrawText(text, SystemFonts.CreateFont("Arial", chevronSize * 3, FontStyle.Regular), Color.Black, startPoint);
            }
        }
    }
}
