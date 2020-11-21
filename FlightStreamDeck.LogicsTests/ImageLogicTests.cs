using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace FlightStreamDeck.Logics.Tests
{
    [TestClass()]
    public class ImageLogicTests
    {
        [TestMethod()]
        public void GetHorizonImageTest()
        {
            ImageLogic images = new ImageLogic();

            string result = images.GetHorizonImage(-10, 20, 359);

            var path = @"Images\horizon.png";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream fs = File.Create(path))
            {
                var bytes = Convert.FromBase64String(result.Substring(23));
                fs.Write(bytes, 0, bytes.Length);
            }
        }

        [TestMethod()]
        public void GetGaugeImageTest()
        {
            ImageLogic images = new ImageLogic();

            string result = images.GetGenericGaugeImage("TRQ", 50, 0, 100, "F2");

            var path = @"Images\gauge.png";

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (FileStream fs = File.Create(path))
            {
                var bytes = Convert.FromBase64String(result.Substring(23));
                fs.Write(bytes, 0, bytes.Length);
            }
        }
    }
}