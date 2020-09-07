using FlightStreamDeck.Core;
using System.Collections.Generic;

namespace FlightStreamDeck.Logics
{
    public interface IExpression
    {

    }

    public interface IEvaluator
    {
        (IEnumerable<TOGGLE_VALUE>, IExpression) Parse(string feedbackValue);
        bool Evaluate(Dictionary<TOGGLE_VALUE, string> values, IExpression expression);
    }
}
