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
            public Expression(TOGGLE_VALUE? feedbackVariable, TOGGLE_VALUE? feedbackComparisonVariable, string feedbackComparisonStringValue, string feedbackComparisonOperator)
            {
                FeedbackVariable = feedbackVariable;
                FeedbackComparisonVariable = feedbackComparisonVariable;
                FeedbackComparisonStringValue = feedbackComparisonStringValue;
                FeedbackComparisonOperator = feedbackComparisonOperator;
            }

            public TOGGLE_VALUE? FeedbackVariable { get; }
            public TOGGLE_VALUE? FeedbackComparisonVariable { get; }
            public string FeedbackComparisonStringValue { get; }
            public string FeedbackComparisonOperator { get; }

            public double? PreviousFeedbackValue { get; set; }
            public double? PreviousFeedbackComparisonValue { get; set; }
            public bool PreviousResult { get; set; }
        }

        private readonly EnumConverter enumConverter;

        public ComparisonEvaluator(EnumConverter enumConverter)
        {
            this.enumConverter = enumConverter;
        }

        public (IEnumerable<TOGGLE_VALUE>, IExpression) Parse(string feedbackValue)
        {
            (var feedbackVariable, var feedbackComparisonVariable, var feedbackComparisonStringValue, var feedbackComparisonOperator) =
                   GetValueValueComparison(feedbackValue);

            var list = new List<TOGGLE_VALUE>();
            if (feedbackVariable != null) list.Add(feedbackVariable.Value);
            if (feedbackComparisonVariable != null) list.Add(feedbackComparisonVariable.Value);
            return (list, new Expression(feedbackVariable, feedbackComparisonVariable, feedbackComparisonStringValue, feedbackComparisonOperator));
        }

        public bool Evaluate(Dictionary<TOGGLE_VALUE, double> values, IExpression expression)
        {
            if (expression is Expression compareExpression)
            {
                double? feedbackValue = null;
                double? comparisonFeedbackValue = null;

                if (compareExpression.FeedbackVariable.HasValue && values.ContainsKey(compareExpression.FeedbackVariable.Value))
                {
                    feedbackValue = values[compareExpression.FeedbackVariable.Value];
                }

                if (compareExpression.FeedbackComparisonVariable.HasValue && values.ContainsKey(compareExpression.FeedbackComparisonVariable.Value))
                {
                    // Compare to a variable
                    comparisonFeedbackValue = values[compareExpression.FeedbackComparisonVariable.Value];
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
        public const string OperatorNotEquals = "!=";
        public const string OperatorGreaterOrEquals = ">=";
        public const string OperatorLessOrEquals = "<=";
        public const string OperatorGreater = ">";
        public const string OperatorLess = "<";

        public static readonly List<string> AllowedComparisons = new List<string> {
            OperatorEquals,
            OperatorNotEquals,
            OperatorGreaterOrEquals,
            OperatorLessOrEquals,
            OperatorGreater,
            OperatorLess
        };

        public Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string> GetValueValueComparison(string value)
        {
            if (string.IsNullOrEmpty(value)) return new Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string>(null, null, string.Empty, string.Empty);

            TOGGLE_VALUE? result = enumConverter.GetVariableEnum(value);

            if (result != null)
            {
                // Old behavior
                return new Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string>(result, null, "0", "!=");
            }
            else
            {
                IEnumerable<string> comparisonAttempt = AllowedComparisons.Where((string allowedComp) => value.Contains(allowedComp));

                if (comparisonAttempt.Count() >= 1)
                {
                    IEnumerable<string> splitInput = value.Split(comparisonAttempt.First());
                    TOGGLE_VALUE? leftSideEnum = enumConverter.GetVariableEnum(splitInput.First());
                    TOGGLE_VALUE? rightSideEnum = int.TryParse(splitInput.Last(), out int temp) ? null : enumConverter.GetVariableEnum(splitInput.Last());
                    string rightSideString = rightSideEnum == null ? splitInput.Last() : null;

                    if (
                        (leftSideEnum != null && rightSideEnum != null && leftSideEnum != rightSideEnum) ||
                        (leftSideEnum != null && !string.IsNullOrEmpty(rightSideString))
                    )
                    {
                        return new Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string>(
                                leftSideEnum,
                                rightSideEnum,
                                rightSideString,
                                comparisonAttempt.First()
                        );
                    }
                }

                return new Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string>(null, null, string.Empty, string.Empty);
            }
        }

        public bool CompareValues(double currentValue, double comparisonValue, string operatorValue)
        {
            bool output = false;

            switch (operatorValue)
            {
                case OperatorEquals:
                    output = currentValue == comparisonValue;
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
