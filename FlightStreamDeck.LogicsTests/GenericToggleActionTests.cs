using FlightStreamDeck.Logics.Actions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace FlightStreamDeck.Logics.Tests
{
    [TestClass]
    public class GenericToggleActionTests
    {
        #region Settings initialization

        [TestMethod]
        public void InitializeToggleParameter_Parses_All_Numeric_To_UInt()
        {
            GenericToggleSettings settings = new GenericToggleSettings();

            settings.ToggleValueData = "1234";
            settings.ParseToggleValueData();

            Assert.AreEqual(1234U, settings.toggleEventDataUInt);
            Assert.IsFalse(settings.toggleParameterIsVariable);
            Assert.IsNull(settings.toggleParameterVariable);
        }

        [TestMethod]
        public void InitializeToggleParameter_Null_for_Not_Set()
        {
            GenericToggleSettings settings = new GenericToggleSettings
            {
                ToggleValueData = null
            };

            settings.ParseToggleValueData();

            Assert.IsNull(settings.toggleEventDataUInt);
            Assert.IsNull(settings.toggleParameterVariable);
            Assert.IsFalse(settings.toggleParameterIsVariable);
        }

        [TestMethod]
        public void InitializeToggleParameter_Strings_With_Numbers()
        {
            string input = "beep 123 some text";

            GenericToggleSettings settings = new GenericToggleSettings
            {
                ToggleValueData = input
            };
            settings.ParseToggleValueData();
            Assert.IsNull(settings.toggleEventDataUInt);
            Assert.IsTrue(settings.toggleParameterIsVariable);
            Assert.AreEqual(input, settings.toggleParameterVariable);
        }

        [TestMethod]
        public void InitializeToggleParameter_Numbers_Plus_Letters()
        {
            string input = "123 some text";

            GenericToggleSettings settings = new GenericToggleSettings
            {
                ToggleValueData = input
            };
            settings.ParseToggleValueData();
            Assert.IsNull(settings.toggleEventDataUInt);
            Assert.IsTrue(settings.toggleParameterIsVariable);
            Assert.AreEqual(input, settings.toggleParameterVariable);
        }



        #endregion
    }
}