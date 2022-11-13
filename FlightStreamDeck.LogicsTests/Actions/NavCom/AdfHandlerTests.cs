using FlightStreamDeck.Logics.Actions.NavCom;

namespace FlightStreamDeck.Logics.Tests.Actions.NavCom;

[TestClass]
public class AdfHandlerTests
{
    [TestMethod]
    public async Task TestSetAdf1200()
    {
        Mock<IFlightConnector> mockFlightConnector = new Mock<IFlightConnector>();

        var eventManager = new SimEventManager(
            new LoggerFactory().CreateLogger<SimEventManager>(), 
            mockFlightConnector.Object
        );

        var registration = eventManager.RegisterEvent(KnownEvents.ADF_STBY_SET.ToString());
        Assert.IsNotNull(registration);

        var adfHandler = new AdfHandler(
            mockFlightConnector.Object,
            eventManager,
            eventManager,
            Core.TOGGLE_VALUE.ADF_ACTIVE_FREQUENCY__1,
            Core.TOGGLE_VALUE.ADF_STANDBY_FREQUENCY__1,
            null,
            null,
            KnownEvents.ADF1_RADIO_SWAP,
            KnownEvents.ADF_STBY_SET
        );

        await adfHandler.TriggerAsync("1200", false);

        mockFlightConnector.Verify(connector => connector.Trigger(registration.Value.eventEnum, 301989888));
    }

    [TestMethod]
    public async Task TestSetAdf120()
    {
        Mock<IFlightConnector> mockFlightConnector = new Mock<IFlightConnector>();

        var eventManager = new SimEventManager(
            new LoggerFactory().CreateLogger<SimEventManager>(),
            mockFlightConnector.Object
        );

        var registration = eventManager.RegisterEvent(KnownEvents.ADF_STBY_SET.ToString());
        Assert.IsNotNull(registration);

        var adfHandler = new AdfHandler(
            mockFlightConnector.Object,
            eventManager,
            eventManager,
            Core.TOGGLE_VALUE.ADF_ACTIVE_FREQUENCY__1,
            Core.TOGGLE_VALUE.ADF_STANDBY_FREQUENCY__1,
            null,
            null,
            KnownEvents.ADF1_RADIO_SWAP,
            KnownEvents.ADF_STBY_SET
        );

        await adfHandler.TriggerAsync("120", false);

        mockFlightConnector.Verify(connector => connector.Trigger(registration.Value.eventEnum, 18874368));
    }
}
