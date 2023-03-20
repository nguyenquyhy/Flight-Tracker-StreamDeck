using FlightStreamDeck.Logics;
using FlightStreamDeck.Logics.Actions.NavCom;
using System.Globalization;
using System.Threading;

namespace FlightStreamDeck.LogicsTests.Actions.NavCom;

[TestClass]
public class HzHandlerTest
{
    [TestMethod]
    public void Test_en_us()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

        var simVarManager = new SimVarManager(
            new LoggerFactory().CreateLogger<SimVarManager>(),
            new Mock<IFlightConnector>().Object
        );

        var handler = new HzHandler(
            new Mock<IEventRegistrar>().Object, new Mock<IEventDispatcher>().Object, simVarManager,
            "COM ACTIVE FREQUENCY:1", null, null, null, null, null, "118000", "128000", "118.000"
        );
        var display = handler.GetDisplayValues(new System.Collections.Generic.Dictionary<SimVarRegistration, double>
        {
            [new SimVarRegistration("COM ACTIVE FREQUENCY:1", "MHz")] = 119.000
        });

        Assert.AreEqual("119.000", display.activeString);
    }

    [TestMethod("For now, this should show the same as US due to some dependent logic.")]
    public void Test_fr_fr()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

        var simVarManager = new SimVarManager(
            new LoggerFactory().CreateLogger<SimVarManager>(),
            new Mock<IFlightConnector>().Object
        );

        var handler = new HzHandler(
            new Mock<IEventRegistrar>().Object, new Mock<IEventDispatcher>().Object, simVarManager,
            "COM ACTIVE FREQUENCY:1", null, null, null, null, null, "118000", "128000", "118.000"
        );
        var display = handler.GetDisplayValues(new System.Collections.Generic.Dictionary<SimVarRegistration, double>
        {
            [new SimVarRegistration("COM ACTIVE FREQUENCY:1", "MHz")] = 119.000
        });

        Assert.AreEqual("119.000", display.activeString);
    }
}
