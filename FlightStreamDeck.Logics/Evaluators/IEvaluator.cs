using FlightStreamDeck.Core;
using System.Collections.Generic;

namespace FlightStreamDeck.Logics
{
    public interface IExpression
    {

    }

    public interface IEvaluator
    {
        (IEnumerable<ToggleValue>, IExpression) Parse(string feedbackValue);
        bool Evaluate(List<ToggleValue> values, IExpression expression);
    }
}
