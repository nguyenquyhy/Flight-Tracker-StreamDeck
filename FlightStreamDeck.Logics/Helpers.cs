using FlightStreamDeck.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace FlightStreamDeck.Logics
{
    public static class Helpers
    {
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
            if (value == null)
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
            if (value == null)
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
    }
}
