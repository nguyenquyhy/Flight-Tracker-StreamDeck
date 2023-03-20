using System.Collections.Generic;
using System.Linq;

namespace FlightStreamDeck.Logics.Tests;

[TestClass]
public class ComparisonEvaluatorTests
{
    private void Workhorse(double currentValue, double comparisonValue, List<string> expectedTrueComparisons)
    {
        Mock<IFlightConnector> mockFlightConnector = new Mock<IFlightConnector>();
        var simVarManager = new SimVarManager(
            new LoggerFactory().CreateLogger<SimVarManager>(),
            mockFlightConnector.Object
        );

        var evaluator = new ComparisonEvaluator(simVarManager);

        //Act
        var errors = new List<string>();
        ComparisonEvaluator.AllowedComparisons.ForEach((string comparisonType) =>
        {
            (var variables, var expression) = evaluator.Parse($"GENERAL ENG OIL PRESSURE:1 {comparisonType} {comparisonValue}");
            bool status = expression.Evaluate(new Dictionary<SimVarRegistration, double>
            {
                [new("GENERAL ENG OIL PRESSURE:1", "Psi")] = currentValue
            });

            if (expectedTrueComparisons.Contains(comparisonType) && !status) errors.Add($"{comparisonType} was not {!status}, but should have been.");
            if (!expectedTrueComparisons.Contains(comparisonType) && status) errors.Add($"{comparisonType} was {status}, but should not have been.");
        });

        //Assert
        Assert.IsFalse(errors.Any(), string.Join(" | ", errors));
    }

    #region Numeric Comparison

    [TestMethod]
    public void CompareValuesTest_Same_Value_Numeric()
    {
        //Arrange
        var currentValue = 0;
        var comparisonValue = 0;
        List<string> expectedTrueComparisons = new List<string> {
            ComparisonEvaluator.OperatorTruncatedEquals,
            ComparisonEvaluator.OperatorEquals,
            ComparisonEvaluator.OperatorGreaterOrEquals,
            ComparisonEvaluator.OperatorLessOrEquals
        };

        //Act
        Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    }

    [TestMethod]
    public void CompareValuesTest_Same_Truncated_Value_Numeric()
    {
        //Arrange
        var currentValue = 1.7;
        var comparisonValue = 1;
        List<string> expectedTrueComparisons = new List<string> {
            ComparisonEvaluator.OperatorTruncatedEquals,
            ComparisonEvaluator.OperatorNotEquals,
            ComparisonEvaluator.OperatorGreaterOrEquals,
            ComparisonEvaluator.OperatorGreater
        };

        //Act
        Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    }

    [TestMethod]
    public void CompareValuesTest_Different_Value_Greater_Numeric()
    {
        //Arrange
        var currentValue = 1;
        var comparisonValue = 0;
        List<string> expectedTrueComparisons = new List<string> {
            ComparisonEvaluator.OperatorNotEquals,
            ComparisonEvaluator.OperatorGreaterOrEquals,
            ComparisonEvaluator.OperatorGreater
        };

        //Act
        Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    }

    [TestMethod]
    public void CompareValuesTest_Different_Value_Less_Numeric()
    {
        //Arrange
        var currentValue = -1;
        var comparisonValue = 0;
        List<string> expectedTrueComparisons = new List<string> {
            ComparisonEvaluator.OperatorNotEquals,
            ComparisonEvaluator.OperatorLessOrEquals,
            ComparisonEvaluator.OperatorLess
        };

        //Act
        Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    }
    #endregion

    #region Alpha Comparison

    //[TestMethod]
    //public void CompareValuesTest_Same_Value_Alpha()
    //{
    //    //Arrange
    //    string currentValue = "m";
    //    string comparisonValue = "m";
    //    List<string> expectedTrueComparisons = new List<string> {
    //        ComparisonEvaluator.OperatorEquals,
    //        ComparisonEvaluator.OperatorGreaterOrEquals,
    //        ComparisonEvaluator.OperatorLessOrEquals
    //    };

    //    //Act
    //    Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    //}

    //[TestMethod]
    //public void CompareValuesTest_Different_Value_Greater_Alpha()
    //{
    //    //Arrange
    //    string currentValue = "n";
    //    string comparisonValue = "m";
    //    List<string> expectedTrueComparisons = new List<string> {
    //        ComparisonEvaluator.OperatorNotEquals,
    //        ComparisonEvaluator.OperatorGreaterOrEquals,
    //        ComparisonEvaluator.OperatorGreater
    //    };

    //    //Act
    //    Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    //}

    //[TestMethod]
    //public void CompareValuesTest_Different_Value_Less_Alpha()
    //{
    //    //Arrange
    //    string currentValue = "l";
    //    string comparisonValue = "m";
    //    List<string> expectedTrueComparisons = new List<string> {
    //        ComparisonEvaluator.OperatorNotEquals,
    //        ComparisonEvaluator.OperatorLessOrEquals,
    //        ComparisonEvaluator.OperatorLess
    //    };

    //    //Act
    //    Workhorse(currentValue, comparisonValue, expectedTrueComparisons);
    //}

    #endregion
}