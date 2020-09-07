using FlightStreamDeck.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FlightStreamDeck.Logics
{
    public static class Helpers
    {
        internal const string comp_equals = "==";
        internal const string comp_not_equals = "!=";
        internal const string comp_greater_or_equals = ">=";
        internal const string comp_less_or_equals = "<=";
        internal const string comp_greater = ">";
        internal const string comp_less = "<";

        internal static readonly List<string> allowed_comparisons = new List<string> {
            comp_equals,
            comp_not_equals,
            comp_greater_or_equals,
            comp_less_or_equals,
            comp_greater,
            comp_less
        };
        public static string MaskedValue(this NumpadParams numpadParams)
        {
            var value = numpadParams.Value;
            var decIndex = numpadParams.Mask.IndexOf(".");

            if (value.Length > decIndex && decIndex >= 0)
            {
                value = value.Insert(decIndex, ".");
            }

            return value;
        }

        public static bool MinMaxRegexValid(this NumpadParams numpadParams, bool skipMinMaxCheck)
        {
            int minVal = InputStringToInt(numpadParams.MinPattern);
            int maxVal = InputStringToInt(numpadParams.MaxPattern);
            int curVal = InputStringToInt(numpadParams.Value);
            string regex = numpadParams.Regex;

            bool output = false;
            bool minMaxValid = minVal <= curVal && maxVal >= curVal;
            bool regexValid = RegexMatchValue(numpadParams.Value, regex);

            output = (minMaxValid && regexValid) || skipMinMaxCheck;

            return output;
        }

        private static bool RegexMatchValue(string value, string regex)
        {
            bool output = false;
            if (!string.IsNullOrEmpty(regex))
            {
                Regex rx = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                output = rx.Matches(value).Count > 0;
            }

            return output;
        }

        private static int InputStringToInt(string input)
        {
            string numericString = String.Empty;
            foreach (var c in input)
            {
                if ((c >= '0' && c <= '9')) numericString = String.Concat(numericString, c.ToString());
            }

            int.TryParse(numericString, System.Globalization.NumberStyles.Integer, null, out int output);

            return output;
        }



        public static TOGGLE_EVENT? GetEventValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            TOGGLE_EVENT result;
            if (Enum.TryParse(value, true, out result))
            {
                return result;
            }

            return null;
        }

        public static TOGGLE_VALUE? GetValueValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            TOGGLE_VALUE result;
            if (Enum.TryParse(value.Replace(":", "__").Replace(" ", "_"), true, out result))
            {
                return result;
            }

            return null;
        }

        public static Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string> GetValueValueComaprison(string value)
        {
            Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string> output = 
                new Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string>(null, null, string.Empty, string.Empty);

            TOGGLE_VALUE? result = GetValueValue(value);

            if (result == null && !string.IsNullOrEmpty(value))
            {
                IEnumerable<string> comparisonAttempt = allowed_comparisons.Where((string allowedComp) => value.Contains(allowedComp));

                if (comparisonAttempt.Count() >= 1)
                {
                    IEnumerable<string> splitInput = value.Split(comparisonAttempt.First());
                    TOGGLE_VALUE? leftSideEnum = GetValueValue(splitInput.First());
                    TOGGLE_VALUE? rightSideEnum = int.TryParse(splitInput.Last(), out int temp) ? null : GetValueValue(splitInput.Last());
                    string rightSideString = rightSideEnum == null ? splitInput.Last() : null;

                    if ( 
                        (leftSideEnum != null && rightSideEnum != null && leftSideEnum != rightSideEnum) ||
                        (leftSideEnum != null && !string.IsNullOrEmpty(rightSideString))
                    )
                    {
                        output =
                            new Tuple<TOGGLE_VALUE?, TOGGLE_VALUE?, string, string>(
                                leftSideEnum, 
                                rightSideEnum, 
                                rightSideString,
                                comparisonAttempt.First()
                        );
                    }
                }
            }

            return output;
        }

        public static bool CompareValues(string currentValue, string comparisonValue, string operatorValue)
        {
            bool output = false;

            int currentValueInt;
            bool successCurrent = Int32.TryParse(currentValue, out currentValueInt);
            int comparisonValueInt;
            bool successComparison = Int32.TryParse(comparisonValue, out comparisonValueInt);

            switch (operatorValue)
            {
                case comp_equals:
                    output = successCurrent && successComparison ? 
                        currentValueInt == comparisonValueInt : 
                        currentValue == comparisonValue;
                    break;
                case comp_not_equals:
                    output = successCurrent && successComparison ? 
                        currentValueInt != comparisonValueInt : 
                        currentValue != comparisonValue;
                    break;
                case comp_greater:
                    output = successCurrent && successComparison ? 
                        currentValueInt > comparisonValueInt : 
                        string.Compare(currentValue, comparisonValue) > 0;
                    break;
                case comp_less:
                    output = successCurrent && successComparison ? 
                        currentValueInt < comparisonValueInt : 
                        string.Compare(currentValue, comparisonValue) < 0;
                    break;
                case comp_greater_or_equals:
                    output = successCurrent && successComparison ? 
                        currentValueInt >= comparisonValueInt : 
                        string.Compare(currentValue, comparisonValue) >= 0;
                    break;
                case comp_less_or_equals:
                    output = successCurrent && successComparison ? 
                        currentValueInt <= comparisonValueInt : 
                        string.Compare(currentValue, comparisonValue) <= 0;
                    break;
            }

            return output;
        }
    }
}
