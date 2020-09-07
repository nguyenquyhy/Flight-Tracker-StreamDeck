using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlightStreamDeck.Logics.Tests
{
    [TestClass()]
    public class HelperTests
    {
        private void Workhorse(string currentValue, string comparisonValue, List<string> expectedTrueComparisons)
        {
            List<string> errors = new List<string>();

            //Act
            Helpers.allowed_comparisons.ForEach((string comparisonType) => {
                bool status = Helpers.CompareValues(currentValue, comparisonValue, comparisonType);

                if (expectedTrueComparisons.Contains(comparisonType) && !status) errors.Add($"{comparisonType} was not {!status}, but should have been.");
                if (!expectedTrueComparisons.Contains(comparisonType) && status) errors.Add($"{comparisonType} was {status}, but should not have been.");
            });

            //Assert
            Assert.IsFalse(errors.Any(), string.Join(" | ", errors));
        }

        #region Numeric Comparison
        [TestMethod()]
        public void CompareValuesTest_Same_Value_Numeric()
        {
            //Arrange
            string currentValue = "0";
            string comparisonValue = "0";
            List<string> expectedTrueComparisons = new List<string> {
                Helpers.comp_equals,
                Helpers.comp_greater_or_equals,
                Helpers.comp_less_or_equals
            };

            //Act
            Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
        }

        [TestMethod()]
        public void CompareValuesTest_Different_Value_Greater_Numeric()
        {
            //Arrange
            string currentValue = "1";
            string comparisonValue = "0";
            List<string> expectedTrueComparisons = new List<string> {
                Helpers.comp_not_equals,
                Helpers.comp_greater_or_equals,
                Helpers.comp_greater
            };

            //Act
            Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
        }

        [TestMethod()]
        public void CompareValuesTest_Different_Value_Less_Numeric()
        {
            //Arrange
            string currentValue = "-1";
            string comparisonValue = "0";
            List<string> expectedTrueComparisons = new List<string> {
                Helpers.comp_not_equals,
                Helpers.comp_less_or_equals,
                Helpers.comp_less
            };

            //Act
            Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
        }
        #endregion

        #region Alpha Comparison
        [TestMethod()]
        public void CompareValuesTest_Same_Value_Alpha()
        {
            //Arrange
            string currentValue = "m";
            string comparisonValue = "m";
            List<string> expectedTrueComparisons = new List<string> {
                Helpers.comp_equals,
                Helpers.comp_greater_or_equals,
                Helpers.comp_less_or_equals
            };

            //Act
            Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
        }

        [TestMethod()]
        public void CompareValuesTest_Different_Value_Greater_Alpha()
        {
            //Arrange
            string currentValue = "n";
            string comparisonValue = "m";
            List<string> expectedTrueComparisons = new List<string> {
                Helpers.comp_not_equals,
                Helpers.comp_greater_or_equals,
                Helpers.comp_greater
            };

            //Act
            Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
        }

        [TestMethod()]
        public void CompareValuesTest_Different_Value_Less_Alpha()
        {
            //Arrange
            string currentValue = "l";
            string comparisonValue = "m";
            List<string> expectedTrueComparisons = new List<string> {
                Helpers.comp_not_equals,
                Helpers.comp_less_or_equals,
                Helpers.comp_less
            };

            //Act
            Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
        }
        #endregion
    }
}