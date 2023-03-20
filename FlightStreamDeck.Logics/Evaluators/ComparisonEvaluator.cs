using System.Collections.Generic;
using System.Linq;

namespace FlightStreamDeck.Logics;

public class ComparisonEvaluator : IEvaluator
{
    public class Expression : IExpression
    {
        public Expression(SimVarRegistration? feedbackVariable, SimVarRegistration? feedbackComparisonVariable, string? feedbackComparisonStringValue, string feedbackComparisonOperator)
        {
            FeedbackVariable = feedbackVariable;
            FeedbackComparisonVariable = feedbackComparisonVariable;
            FeedbackComparisonStringValue = feedbackComparisonStringValue;
            FeedbackComparisonOperator = feedbackComparisonOperator;
        }

        public SimVarRegistration? FeedbackVariable { get; }
        public SimVarRegistration? FeedbackComparisonVariable { get; }
        public string? FeedbackComparisonStringValue { get; }
        public string FeedbackComparisonOperator { get; }

        public double? PreviousFeedbackValue { get; set; }
        public double? PreviousFeedbackComparisonValue { get; set; }
        public bool PreviousResult { get; set; }

        public bool Evaluate(Dictionary<SimVarRegistration, double> values)
        {
            double? feedbackValue = null;
            double? comparisonFeedbackValue = null;

            if (FeedbackVariable != null && values.ContainsKey(FeedbackVariable))
            {
                feedbackValue = values[FeedbackVariable];
            }

            if (FeedbackComparisonVariable != null && values.ContainsKey(FeedbackComparisonVariable))
            {
                // Compare to a variable
                comparisonFeedbackValue = values[FeedbackComparisonVariable];
            }
            else
            {
                // Compare to a number
                if (double.TryParse(FeedbackComparisonStringValue, out var number))
                {
                    comparisonFeedbackValue = number;
                }
            }

            if (feedbackValue == PreviousFeedbackValue
                && comparisonFeedbackValue == PreviousFeedbackComparisonValue)
            {
                return PreviousResult;
            }

            PreviousFeedbackValue = feedbackValue;
            PreviousFeedbackComparisonValue = comparisonFeedbackValue;

            if (feedbackValue.HasValue && comparisonFeedbackValue.HasValue)
            {
                PreviousResult = CompareValues(feedbackValue.Value, comparisonFeedbackValue.Value, FeedbackComparisonOperator);
                return PreviousResult;
            }
            return false;
        }

        public bool CompareValues(double currentValue, double comparisonValue, string operatorValue)
        {
            bool output = false;

            switch (operatorValue)
            {
                case OperatorEquals:
                    output = currentValue == comparisonValue;
                    break;
                case OperatorTruncatedEquals:
                    output = (long)currentValue == (long)comparisonValue;
                    break;
                case OperatorNotEquals:
                    output = currentValue != comparisonValue;
                    break;
                case OperatorGreater:
                    output = currentValue > comparisonValue;
                    break;
                case OperatorLess:
                    output = currentValue < comparisonValue;
                    break;
                case OperatorGreaterOrEquals:
                    output = currentValue >= comparisonValue;
                    break;
                case OperatorLessOrEquals:
                    output = currentValue <= comparisonValue;
                    break;
            }

            return output;
        }
    }

    public const string OperatorEquals = "==";
    public const string OperatorTruncatedEquals = "~";
    public const string OperatorNotEquals = "!=";
    public const string OperatorGreaterOrEquals = ">=";
    public const string OperatorLessOrEquals = "<=";
    public const string OperatorGreater = ">";
    public const string OperatorLess = "<";

    public static readonly List<string> AllowedComparisons = new List<string> {
        OperatorEquals,
        OperatorTruncatedEquals,
        OperatorNotEquals,
        OperatorGreaterOrEquals,
        OperatorLessOrEquals,
        OperatorGreater,
        OperatorLess
    };

    private readonly SimVarManager simVarManager;

    public ComparisonEvaluator(SimVarManager simVarManager)
    {
        this.simVarManager = simVarManager;
    }

    public (IEnumerable<SimVarRegistration>, IExpression) Parse(string feedbackValue)
    {
        var (feedbackVariable, feedbackComparisonVariable, feedbackComparisonStringValue, feedbackComparisonOperator) =
               GetValueValueComparison(feedbackValue);

        var list = new List<SimVarRegistration>();
        if (feedbackVariable != null) list.Add(feedbackVariable);
        if (feedbackComparisonVariable != null) list.Add(feedbackComparisonVariable);
        return (list, new Expression(feedbackVariable, feedbackComparisonVariable, feedbackComparisonStringValue, feedbackComparisonOperator));
    }

    private (SimVarRegistration? leftEnum, SimVarRegistration? rightEnum, string? rightString, string comparisionOperator) GetValueValueComparison(string value)
    {
        if (string.IsNullOrEmpty(value)) return (null, null, string.Empty, string.Empty);

        var comparisonFound = AllowedComparisons.FirstOrDefault(value.Contains);

        if (comparisonFound != null)
        {
            var splitInput = value.Split(comparisonFound);

            if (splitInput is [var left, var right])
            {
                var leftSideEnum = simVarManager.GetRegistration(left, null);
                var rightSideEnum = int.TryParse(right, out int temp) ? null : simVarManager.GetRegistration(right, null);
                var rightSideString = rightSideEnum == null ? right : null;

                if (
                    (leftSideEnum != null && rightSideEnum != null && leftSideEnum != rightSideEnum) ||
                    (leftSideEnum != null && !string.IsNullOrEmpty(rightSideString))
                )
                {
                    return (
                        leftSideEnum,
                        rightSideEnum,
                        rightSideString,
                        comparisonFound
                    );
                }
            }
        }
        else
        {
            // Old behavior
            var result = simVarManager.GetRegistration(value, null);
            if (result != null)
            {
                return (result, null, "0", "!=");
            }
        }

        return (null, null, string.Empty, string.Empty);
    }
}
