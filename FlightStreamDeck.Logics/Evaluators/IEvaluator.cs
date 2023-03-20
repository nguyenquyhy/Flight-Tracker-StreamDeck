using System.Collections.Generic;

namespace FlightStreamDeck.Logics;

public interface IExpression
{
    bool Evaluate(Dictionary<SimVarRegistration, double> values);
}

public interface IEvaluator
{
    (IEnumerable<SimVarRegistration>, IExpression?) Parse(string feedbackValue);
}
