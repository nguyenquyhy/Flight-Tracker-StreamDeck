using FlightStreamDeck.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlightStreamDeck.Logics
{
    public class ComparisonEvaluator : IEvaluator
    {
        public class Expression : IExpression
        {
            public Expression(ToggleValue feedbackVariable, ToggleValue feedbackComparisonVariable, string feedbackComparisonStringValue, string feedbackComparisonOperator)
            {
                FeedbackVariable = feedbackVariable;
                FeedbackComparisonVariable = feedbackComparisonVariable;
                FeedbackComparisonStringValue = feedbackComparisonStringValue;
                FeedbackComparisonOperator = feedbackComparisonOperator;
            }

            public ToggleValue FeedbackVariable { get; }
            public ToggleValue FeedbackComparisonVariable { get; }
            public string FeedbackComparisonStringValue { get; }
            public string FeedbackComparisonOperator { get; }

            public double? PreviousFeedbackValue { get; set; }
            public double? PreviousFeedbackComparisonValue { get; set; }
            public bool PreviousResult { get; set; }
        }

        public (IEnumerable<ToggleValue>, IExpression) Parse(string feedbackValue)
        {
            (var feedbackVariable, var feedbackComparisonVariable, var feedbackComparisonStringValue, var feedbackComparisonOperator) = GetValueValueComparison(feedbackValue);

            var list = new List<ToggleValue>();
            if (feedbackVariable != null && !string.IsNullOrWhiteSpace(feedbackVariable.Name)) list.Add(new ToggleValue(feedbackVariable.Name));
            if (feedbackComparisonVariable != null && !string.IsNullOrWhiteSpace(feedbackComparisonVariable.Name)) list.Add(new ToggleValue(feedbackComparisonVariable.Name));
            return (list, new Expression(feedbackVariable, feedbackComparisonVariable, feedbackComparisonStringValue, feedbackComparisonOperator));
        }

        public bool Evaluate(List<ToggleValue> values, IExpression expression)
        {
            if (expression is Expression compareExpression)
            {
                double? feedbackValue = null;
                double? comparisonFeedbackValue = null;

                if (compareExpression.FeedbackVariable != null && values.Find(x => x.Name == compareExpression.FeedbackVariable.Name) != null)
                {
                    feedbackValue = values.Find(x => x.Name == compareExpression.FeedbackVariable.Name).Value;
                }

                if (compareExpression.FeedbackComparisonVariable != null && values.Find(x => x.Name == compareExpression.FeedbackComparisonVariable.Name) != null)
                {
                    // Compare to a variable
                    comparisonFeedbackValue = values.Find(x => x.Name == compareExpression.FeedbackComparisonVariable.Name).Value;
                }
                else
                {
                    // Compare to a number
                    if (double.TryParse(compareExpression.FeedbackComparisonStringValue, out var number))
                    {
                        comparisonFeedbackValue = number;
                    }
                }

                if (feedbackValue == compareExpression.PreviousFeedbackValue
                    && comparisonFeedbackValue == compareExpression.PreviousFeedbackComparisonValue)
                {
                    return compareExpression.PreviousResult;
                }

                compareExpression.PreviousFeedbackValue = feedbackValue;
                compareExpression.PreviousFeedbackComparisonValue = comparisonFeedbackValue;

                if (feedbackValue.HasValue && comparisonFeedbackValue.HasValue)
                {
                    compareExpression.PreviousResult = CompareValues(feedbackValue.Value, comparisonFeedbackValue.Value, compareExpression.FeedbackComparisonOperator);
                    return compareExpression.PreviousResult;
                }
                return false;
            }
            throw new ArgumentException($"{nameof(expression)} has to be of type {typeof(Expression).FullName}!", nameof(expression));
        }

        public const string OperatorEquals = "==";
        public const string OperatorTruncatedEquals = "~";
        public const string OperatorNotEquals = "!=";
        public const string OperatorGreaterOrEquals = ">=";
        public const string OperatorLessOrEquals = "<=";
        public const string OperatorGreater = ">";
        public const string OperatorLess = "<";

        public static readonly List<string> AllowedComparisons = new() {
            OperatorEquals,
            OperatorTruncatedEquals,
            OperatorNotEquals,
            OperatorGreaterOrEquals,
            OperatorLessOrEquals,
            OperatorGreater,
            OperatorLess
        };

        public static Tuple<ToggleValue, ToggleValue, string, string> GetValueValueComparison(string value)
        {
            if (string.IsNullOrEmpty(value)) return new Tuple<ToggleValue, ToggleValue, string, string>(null, null, string.Empty, string.Empty);

            ToggleValue result = new(value);

            if (result != null)
            {
                // Old behavior
                return new Tuple<ToggleValue, ToggleValue, string, string>(result, null, "0", "!=");
            }
            else
            {
                IEnumerable<string> comparisonAttempt = AllowedComparisons.Where((string allowedComp) => value.Contains(allowedComp));

                if (comparisonAttempt.Any())
                {
                    IEnumerable<string> splitInput = value.Split(comparisonAttempt.First());
                    ToggleValue leftSideEnum = new(splitInput.First());
                    ToggleValue rightSideEnum = int.TryParse(splitInput.Last(), out int temp) ? null : new ToggleValue(splitInput.Last());
                    string rightSideString = rightSideEnum == null ? splitInput.Last() : null;

                    if (
                        (leftSideEnum != null && rightSideEnum != null && leftSideEnum != rightSideEnum) ||
                        (leftSideEnum != null && !string.IsNullOrEmpty(rightSideString))
                    )
                    {
                        return new Tuple<ToggleValue, ToggleValue, string, string>(
                                leftSideEnum,
                                rightSideEnum,
                                rightSideString,
                                comparisonAttempt.First()
                        );
                    }
                }

                return new Tuple<ToggleValue, ToggleValue, string, string>(null, null, string.Empty, string.Empty);
            }
        }

        public static bool CompareValues(double currentValue, double comparisonValue, string operatorValue)
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
}
