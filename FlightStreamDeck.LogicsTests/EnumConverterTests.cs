using FlightStreamDeck.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FlightStreamDeck.Logics.Tests
{
    [TestClass]
    public class EnumConverterTests
    {
        readonly EnumConverter enumConverter = new EnumConverter();

        #region Settings initialization

        [TestMethod]
        public void InitializeToggleParameter_Parses_All_Numeric_To_UInt()
        {
            (var dataUInt, var variable) = enumConverter.GetUIntOrVariable("1234");

            Assert.AreEqual(1234U, dataUInt);
            Assert.IsNull(variable);
        }

        [TestMethod]
        public void InitializeToggleParameter_Null_for_Not_Set()
        {
            (var dataUInt, var variable) = enumConverter.GetUIntOrVariable(null);

            Assert.IsNull(dataUInt);
            Assert.IsNull(variable);
        }

        [TestMethod]
        public void InitializeToggleParameter_Strings_Invalid_Variable()
        {
            string input = "beep 123 some text";
            (var dataUInt, var variable) = enumConverter.GetUIntOrVariable(input);

            Assert.IsNull(dataUInt);
            Assert.IsNull(variable);
        }

        [TestMethod]
        public void InitializeToggleParameter_Strings_Valid_Variablef()
        {
            string input = "GENERAL ENG COMBUSTION:1";
            (var dataUInt, var variable) = enumConverter.GetUIntOrVariable(input);

            Assert.IsNull(dataUInt);
            Assert.AreEqual(TOGGLE_VALUE.GENERAL_ENG_COMBUSTION__1, variable);
        }

        #endregion
    }
}