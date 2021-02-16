using FlightStreamDeck.Core;
using System;
using System.Collections.Generic;

namespace FlightStreamDeck.Logics
{
    /// <summary>
    /// This evaluator assumes feedbackValue contains only a single SimConnect variable
    /// </summary>
    public class SimpleEvaluator : IEvaluator
    {
        public class Expression : IExpression
        {
            public Expression(TOGGLE_VALUE variable)
            {
                Variable = variable;
            }
            public TOGGLE_VALUE Variable { get; }
        }

        private readonly EnumConverter enumConverter;

        public SimpleEvaluator(EnumConverter enumConverter)
        {
            this.enumConverter = enumConverter;
        }

        public (IEnumerable<TOGGLE_VALUE>, IExpression) Parse(string feedbackValue)
        {
            var variable = enumConverter.GetVariableEnum(feedbackValue);
            if (variable == null) return (new List<TOGGLE_VALUE>(), null);

            return (new List<TOGGLE_VALUE> { variable.Value }, new Expression(variable.Value));
        }

        public bool Evaluate(Dictionary<TOGGLE_VALUE, double> values, IExpression expression)
        {
            if (expression is Expression simpleExpression)
            {
                if (values.ContainsKey(simpleExpression.Variable))
                {
                    return values[simpleExpression.Variable] != 0;
                }
                return false;
            }
            throw new ArgumentException($"{nameof(expression)} has to be of type {typeof(Expression).FullName}!", nameof(expression));
        }
    }
}
