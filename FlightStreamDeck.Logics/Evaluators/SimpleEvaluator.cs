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
            public Expression(ToggleValue variable)
            {
                Variable = variable;
            }
            public ToggleValue Variable { get; }
        }


        public (IEnumerable<ToggleValue>, IExpression) Parse(string feedbackValue)
        {
            var variable = new ToggleValue(feedbackValue);
            if (variable == null) return (new List<ToggleValue>(), null);

            return (new List<ToggleValue> {  variable }, new Expression(variable));
        }

        public bool Evaluate(List<ToggleValue> values, IExpression expression)
        {
            if (expression is Expression simpleExpression)
            {
                if (values.Contains(simpleExpression.Variable))
                {
                    return values.Find(x => x.Name == simpleExpression.Variable.Name).Value != 0;
                }
                return false;
            }
            throw new ArgumentException($"{nameof(expression)} has to be of type {typeof(Expression).FullName}!", nameof(expression));
        }
    }
}
