using FlightStreamDeck.Core;
using FlightStreamDeck.Logics;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FlightStreamDeck.SimConnectFSX
{
    public class SimConnectFlightConnector : IFlightConnector
    {
        public event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        public event EventHandler<ToggleValueUpdatedEventArgs> GenericValuesUpdated;

        private const int StatusDelayMilliseconds = 100;

        public event EventHandler Closed;

        private readonly List<TOGGLE_EVENT> genericEvents = new List<TOGGLE_EVENT>();
        private readonly HashSet<TOGGLE_VALUE> genericValues = new HashSet<TOGGLE_VALUE>();

        private readonly EventValueLibrary eventLib = new EventValueLibrary();

        private readonly object lockLists = new object();

        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;
        private readonly ILogger<SimConnectFlightConnector> logger;

        public IntPtr Handle { get; private set; }

        private SimConnect simconnect = null;
        private CancellationTokenSource cts = null;

        public SimConnectFlightConnector(ILogger<SimConnectFlightConnector> logger)
        {
            this.logger = logger;
        }

        // Simconnect client will send a win32 message when there is
        // a packet to process. ReceiveMessage must be called to
        // trigger the events. This model keeps simconnect processing on the main thread.
        public IntPtr HandleSimConnectEvents(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            isHandled = false;

            switch (message)
            {
                case WM_USER_SIMCONNECT:
                    {
                        if (simconnect != null)
                        {
                            try
                            {
                                this.simconnect.ReceiveMessage();
                            }
                            catch (Exception ex)
                            {
                                RecoverFromError(ex);
                            }

                            isHandled = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            return IntPtr.Zero;
        }

        // Set up the SimConnect event handlers
        public void Initialize(IntPtr Handle)
        {
            if (simconnect != null)
            {
                logger.LogWarning("Initialization is already done. Cancelled this request.");
                return;
            }
            simconnect = new SimConnect("Flight Tracker Stream Deck", Handle, WM_USER_SIMCONNECT, null, 0);

            // listen to connect and quit msgs
            simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(Simconnect_OnRecvOpen);
            simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(Simconnect_OnRecvQuit);

            // listen to exceptions
            simconnect.OnRecvException += Simconnect_OnRecvException;

            simconnect.OnRecvSimobjectDataBytype += Simconnect_OnRecvSimobjectDataBytypeAsync;
            simconnect.OnRecvSystemState += Simconnect_OnRecvSystemState;

            RegisterFlightStatusDefinition();

            simconnect.MapClientEventToSimEvent(EVENTS.AUTOPILOT_ON, "AUTOPILOT_ON");
            simconnect.MapClientEventToSimEvent(EVENTS.AUTOPILOT_OFF, "AUTOPILOT_OFF");
            simconnect.MapClientEventToSimEvent(EVENTS.AUTOPILOT_TOGGLE, "AP_MASTER");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_HDG_TOGGLE, "AP_PANEL_HEADING_HOLD");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_NAV_TOGGLE, "AP_NAV1_HOLD");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_APR_TOGGLE, "AP_APR_HOLD");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_ALT_TOGGLE, "AP_PANEL_ALTITUDE_HOLD");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_VS_TOGGLE, "AP_VS_HOLD");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_FLC_ON, "FLIGHT_LEVEL_CHANGE_ON");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_FLC_OFF, "FLIGHT_LEVEL_CHANGE_OFF");

            simconnect.MapClientEventToSimEvent(EVENTS.AP_HDG_SET, "HEADING_BUG_SET");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_HDG_INC, "HEADING_BUG_INC");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_HDG_DEC, "HEADING_BUG_DEC");

            simconnect.MapClientEventToSimEvent(EVENTS.AP_ALT_SET, "AP_ALT_VAR_SET_ENGLISH");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_ALT_INC, "AP_ALT_VAR_INC");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_ALT_DEC, "AP_ALT_VAR_DEC");

            simconnect.MapClientEventToSimEvent(EVENTS.AP_VS_SET, "AP_VS_VAR_SET_ENGLISH");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_VS_INC, "AP_VS_VAR_INC");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_VS_DEC, "AP_VS_VAR_DEC");

            simconnect.MapClientEventToSimEvent(EVENTS.AP_AIRSPEED_SET, "AP_SPD_VAR_SET");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_AIRSPEED_INC, "AP_SPD_VAR_INC");
            simconnect.MapClientEventToSimEvent(EVENTS.AP_AIRSPEED_DEC, "AP_SPD_VAR_DEC");

            simconnect.MapClientEventToSimEvent(EVENTS.QNH_SET, "KOHLSMAN_SET");
            simconnect.MapClientEventToSimEvent(EVENTS.QNH_INC, "KOHLSMAN_INC");
            simconnect.MapClientEventToSimEvent(EVENTS.QNH_DEC, "KOHLSMAN_DEC");

            simconnect.MapClientEventToSimEvent(EVENTS.AVIONICS_TOGGLE, "AVIONICS_MASTER_SET");

            isGenericValueRegistered = false;
            RegisterGenericValues();
            RegisterGenericEvents();
        }

        public void Send(string message)
        {
            simconnect?.Text(SIMCONNECT_TEXT_TYPE.PRINT_BLACK, 3, EVENTS.MESSAGE_RECEIVED, message);
        }

        public void ApOn()
        {
            SendCommand(EVENTS.AUTOPILOT_ON);
        }

        public void ApOff()
        {
            SendCommand(EVENTS.AUTOPILOT_OFF);
        }

        public void ApToggle()
        {
            SendCommand(EVENTS.AUTOPILOT_TOGGLE);
        }

        public void ApHdgToggle()
        {
            SendCommand(EVENTS.AP_HDG_TOGGLE);
        }

        public void ApNavToggle()
        {
            SendCommand(EVENTS.AP_NAV_TOGGLE);
        }

        public void ApAprToggle()
        {
            SendCommand(EVENTS.AP_APR_TOGGLE);
        }

        public void ApAltToggle()
        {
            SendCommand(EVENTS.AP_ALT_TOGGLE);
        }

        public void ApVsToggle()
        {
            SendCommand(EVENTS.AP_VS_TOGGLE);
        }

        public void ApFlcOn()
        {
            SendCommand(EVENTS.AP_FLC_ON);
        }

        public void ApFlcOff()
        {
            SendCommand(EVENTS.AP_FLC_OFF);
        }

        public void ApHdgSet(uint heading)
        {
            SendCommand(EVENTS.AP_HDG_SET, heading);
        }

        public void ApHdgInc()
        {
            SendCommand(EVENTS.AP_HDG_INC);
        }

        public void ApHdgDec()
        {
            SendCommand(EVENTS.AP_HDG_DEC);
        }

        public void ApAltSet(uint altitude)
        {
            SendCommand(EVENTS.AP_ALT_SET, altitude);
        }

        public void ApAltInc()
        {
            SendCommand(EVENTS.AP_ALT_INC);
        }

        public void ApAltDec()
        {
            SendCommand(EVENTS.AP_ALT_DEC);
        }

        public void ApVsSet(uint speed)
        {
            SendCommand(EVENTS.AP_VS_SET, speed);
        }

        public void ApAirSpeedSet(uint speed)
        {
            SendCommand(EVENTS.AP_AIRSPEED_SET, speed);
        }

        public void ApAirSpeedInc()
        {
            SendCommand(EVENTS.AP_AIRSPEED_INC);
        }

        public void ApAirSpeedDec()
        {
            SendCommand(EVENTS.AP_AIRSPEED_DEC);
        }

        public void QNHSet(uint qnh)
        {
            SendCommand(EVENTS.QNH_SET, qnh);
        }

        public void QNHInc()
        {
            SendCommand(EVENTS.QNH_INC);
        }

        public void QNHDec()
        {
            SendCommand(EVENTS.QNH_DEC);
        }


        public void AvMasterToggle(uint state)
        {
            SendCommand(EVENTS.AVIONICS_TOGGLE, state);
        }

        private void SendCommand(EVENTS sendingEvent, uint data = 0)
        {
            try
            {
                simconnect?.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, sendingEvent, data, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex) when (ex.Message == "0xC00000B0")
            {
                RecoverFromError(ex);
            }
        }

        private void SendGenericCommand(TOGGLE_EVENT sendingEvent, uint dwData = 0)
        {
            try
            {
                simconnect?.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, sendingEvent, dwData, GROUPID.MAX, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex) when (ex.Message == "0xC00000B0")
            {
                RecoverFromError(ex);
            }
        }

        public void CloseConnection()
        {
            try
            {
                logger.LogDebug("Trying to cancel request loop");
                cts?.Cancel();
                cts = null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cannot cancel request loop! Error: {ex.Message}");
            }
            try
            {
                // Dispose serves the same purpose as SimConnect_Close()
                simconnect?.Dispose();
                simconnect = null;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Cannot unsubscribe events! Error: {ex.Message}");
            }
        }

        private void RegisterFlightStatusDefinition()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "SIMULATION RATE",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE LATITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE LONGITUDE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE ALTITUDE",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE ALT ABOVE GROUND",
                "Feet",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE PITCH DEGREES",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE BANK DEGREES",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE HEADING DEGREES TRUE",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "PLANE HEADING DEGREES MAGNETIC",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "GROUND ALTITUDE",
                "Meters",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "GROUND VELOCITY",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AIRSPEED INDICATED",
                "Knots",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "VERTICAL SPEED",
                "Feet per minute",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "FUEL TOTAL QUANTITY",
                "Gallons",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AMBIENT WIND VELOCITY",
                "Feet per second",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AMBIENT WIND DIRECTION",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "SIM ON GROUND",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "STALL WARNING",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "OVERSPEED WARNING",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            #region Autopilot

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT MASTER",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT HEADING LOCK",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT HEADING LOCK DIR",
                "Degrees",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT NAV1 LOCK",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT APPROACH HOLD",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT ALTITUDE LOCK",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT ALTITUDE LOCK VAR",
                "Feet",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT VERTICAL HOLD",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT VERTICAL HOLD VAR",
                "Feet per minute",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT FLIGHT LEVEL CHANGE",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AUTOPILOT AIRSPEED HOLD VAR",
                "Knots",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            #endregion

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "KOHLSMAN SETTING MB",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "TRANSPONDER CODE:1",
                "Hz",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "COM ACTIVE FREQUENCY:1",
                "kHz",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "COM ACTIVE FREQUENCY:2",
                "kHz",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "AVIONICS MASTER SWITCH",
                "number",
                SIMCONNECT_DATATYPE.INT32,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "NAV OBS:1",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "NAV OBS:2",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus,
                "ADF CARD",
                "Degrees",
                SIMCONNECT_DATATYPE.FLOAT64,
                0.0f,
                SimConnect.SIMCONNECT_UNUSED);

            // IMPORTANT: register it with the simconnect managed wrapper marshaller
            // if you skip this step, you will only receive a uint in the .dwData field.
            simconnect.RegisterDataDefineStruct<FlightStatusStruct>(DEFINITIONS.FlightStatus);
        }

        private void Simconnect_OnRecvSimobjectDataBytypeAsync(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            // Must be general SimObject information
            switch (data.dwRequestID)
            {
                case (uint)DATA_REQUESTS.FLIGHT_STATUS:
                    {
                        var flightStatus = data.dwData[0] as FlightStatusStruct?;

                        if (flightStatus.HasValue)
                        {
                            logger.LogTrace("Get Aircraft status");
                            AircraftStatusUpdated?.Invoke(this, new AircraftStatusUpdatedEventArgs(
                                new AircraftStatus
                                {
                                    //SimTime = flightStatus.Value.SimTime,
                                    //SimRate = flightStatus.Value.SimRate,
                                    Latitude = flightStatus.Value.Latitude,
                                    Longitude = flightStatus.Value.Longitude,
                                    Altitude = flightStatus.Value.Altitude,
                                    AltitudeAboveGround = flightStatus.Value.AltitudeAboveGround,
                                    Pitch = flightStatus.Value.Pitch,
                                    Bank = flightStatus.Value.Bank,
                                    Heading = flightStatus.Value.MagneticHeading,
                                    TrueHeading = flightStatus.Value.TrueHeading,
                                    GroundSpeed = flightStatus.Value.GroundSpeed,
                                    IndicatedAirSpeed = flightStatus.Value.IndicatedAirSpeed,
                                    VerticalSpeed = flightStatus.Value.VerticalSpeed,
                                    FuelTotalQuantity = flightStatus.Value.FuelTotalQuantity,
                                    IsOnGround = flightStatus.Value.IsOnGround == 1,
                                    StallWarning = flightStatus.Value.StallWarning == 1,
                                    OverspeedWarning = flightStatus.Value.OverspeedWarning == 1,
                                    IsAutopilotOn = flightStatus.Value.IsAutopilotOn == 1,
                                    IsApHdgOn = flightStatus.Value.IsApHdgOn == 1,
                                    ApHeading = flightStatus.Value.ApHdg,
                                    IsApNavOn = flightStatus.Value.IsApNavOn == 1,
                                    IsApAprOn = flightStatus.Value.IsApAprOn == 1,
                                    IsApAltOn = flightStatus.Value.IsApAltOn == 1,
                                    ApAltitude = flightStatus.Value.ApAlt,
                                    IsApVsOn = flightStatus.Value.IsApVsOn == 1,
                                    IsApFlcOn = flightStatus.Value.IsApFlcOn == 1,
                                    ApAirspeed = flightStatus.Value.ApAirspeed,
                                    ApVs = flightStatus.Value.ApVs,
                                    QNHMbar = flightStatus.Value.QNHmbar,
                                    Transponder = flightStatus.Value.Transponder.ToString().PadLeft(4, '0'),
                                    FreqencyCom1 = flightStatus.Value.Com1,
                                    FreqencyCom2 = flightStatus.Value.Com2,
                                    IsAvMasterOn = flightStatus.Value.AvMasterOn == 1,
                                    Nav1OBS = flightStatus.Value.Nav1OBS,
                                    Nav2OBS = flightStatus.Value.Nav2OBS,
                                    ADFCard = flightStatus.Value.ADFCard,
                                }));
                        }
                        else
                        {
                            // Cast failed
                            logger.LogError("Cannot cast to FlightStatusStruct!");
                        }
                    }
                    break;

                case (uint)DATA_REQUESTS.TOGGLE_VALUE_DATA:
                    {
                        var result = new Dictionary<TOGGLE_VALUE, string>();
                        lock (lockLists)
                        {
                            if (data.dwDefineCount != genericValues.Count)
                            {
                                logger.LogError("Incompatible array count {actual}, expected {expected}. Skipping received data", data.dwDefineCount, genericValues.Count);
                                return;
                            }

                            var dataArray = data.dwData[0] as GenericValuesStruct?;

                            if (!dataArray.HasValue)
                            {
                                logger.LogError("Invalid data received");
                                return;
                            }

                            for (int i = 0; i < data.dwDefineCount; i++)
                            {
                                var genericValue = genericValues.ElementAt(i);
                                int decimals = eventLib.GetDecimals(genericValue);
                                double toggleValue = Math.Round(dataArray.Value.Get(i), decimals);
                                result.Add(genericValue, toggleValue.ToString("F" + decimals.ToString()));
                            }
                        }

                        GenericValuesUpdated?.Invoke(this, new ToggleValueUpdatedEventArgs(result));
                    }
                    break;
            }
        }

        private void Simconnect_OnRecvSystemState(SimConnect sender, SIMCONNECT_RECV_SYSTEM_STATE data)
        {
            switch (data.dwRequestID)
            {
                case (int)DATA_REQUESTS.FLIGHT_PLAN:
                    if (!string.IsNullOrEmpty(data.szString))
                    {
                        logger.LogInformation("Receive flight plan {flightPlan}", data.szString);
                    }
                    break;
            }
        }

        void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            logger.LogInformation("Connected to Flight Simulator");

            cts?.Cancel();
            cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(StatusDelayMilliseconds);
                        await smGeneric.WaitAsync();
                        try
                        {
                            cts?.Token.ThrowIfCancellationRequested();
                            simconnect?.RequestDataOnSimObjectType(DATA_REQUESTS.FLIGHT_STATUS, DEFINITIONS.FlightStatus, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                            if (genericValues.Count > 0 && isGenericValueRegistered)
                            {
                                simconnect?.RequestDataOnSimObjectType(DATA_REQUESTS.TOGGLE_VALUE_DATA, DEFINITIONS.GenericData, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                            }
                        }
                        finally
                        {
                            smGeneric.Release();
                        }
                    }
                }
                catch (TaskCanceledException) { }
            });
        }

        // The case where the user closes Flight Simulator
        void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            logger.LogInformation("Flight Simulator has exited");
            CloseConnection();
            Closed?.Invoke(this, new EventArgs());
        }

        void Simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            logger.LogError("Exception received: {error}", (SIMCONNECT_EXCEPTION)data.dwException);
            switch ((SIMCONNECT_EXCEPTION)data.dwException)
            {
                case SIMCONNECT_EXCEPTION.ERROR:
                    // Try to reconnect on unknown error
                    CloseConnection();
                    Closed?.Invoke(this, new EventArgs());
                    break;

                case SIMCONNECT_EXCEPTION.VERSION_MISMATCH:
                    // HACK: when sending an event repeatedly,
                    // SimConnect might sendd thihs error and stop reacting and responding.
                    // The workaround would be to force a reconnection.
                    CloseConnection();
                    Closed?.Invoke(this, new EventArgs());
                    break;
            }
        }

        private void RecoverFromError(Exception exception)
        {
            // 0xC000014B: CTD
            // 0xC00000B0: Sim has exited or any generic SimConnect error
            logger.LogError(exception, "Exception received");
            CloseConnection();
            Closed?.Invoke(this, new EventArgs());
        }

        #region Generic Buttons

        public void RegisterToggleEvent(TOGGLE_EVENT toggleAction)
        {
            if (simconnect == null) return;

            if (genericEvents.Contains(toggleAction))
            {
                return;
            }

            genericEvents.Add(toggleAction);
            logger.LogInformation("RegisterEvent {action} {simConnectAction}", toggleAction, toggleAction.EventToSimConnectEvent());
            simconnect.MapClientEventToSimEvent(toggleAction, toggleAction.EventToSimConnectEvent());
        }

        public void RegisterSimValue(TOGGLE_VALUE simValue)
        {
            var changed = false;
            lock (lockLists)
            {
                logger.LogInformation("Registering {value}", simValue);
                changed = genericValues.Add(simValue);
            }
            if (changed)
            {
                RegisterGenericValues();
            }
        }

        public void DeRegisterSimValue(TOGGLE_VALUE simValue)
        {
            var changed = false;
            lock (lockLists)
            {
                logger.LogInformation("De-Registering {value}", simValue);
                changed = genericValues.Remove(simValue);
            }
            if (changed)
            {
                RegisterGenericValues();
            }
        }

        public void RegisterSimValues(params TOGGLE_VALUE[] simValues)
        {
            var changed = false;
            lock (lockLists)
            {
                logger.LogInformation("Registering {values}", string.Join(", ", simValues));
                foreach (var simValue in simValues)
                {
                    changed |= genericValues.Add(simValue);
                }
            }
            if (changed)
            {
                RegisterGenericValues();
            }
        }

        public void DeRegisterSimValues(params TOGGLE_VALUE[] simValues)
        {
            var changed = false;
            lock (lockLists)
            {
                logger.LogInformation("De-Registering {values}", string.Join(", ", simValues));
                foreach (var simValue in simValues)
                {
                    changed |= genericValues.Remove(simValue);
                }
            }
            if (changed)
            {
                RegisterGenericValues();
            }
        }

        private CancellationTokenSource ctsGeneric = null;
        private readonly object lockGeneric = new object();
        private readonly SemaphoreSlim smGeneric = new SemaphoreSlim(1);
        private bool isGenericValueRegistered = false;

        private void RegisterGenericValues()
        {
            if (simconnect == null) return;

            CancellationTokenSource cts;
            lock (lockGeneric)
            {
                ctsGeneric?.Cancel();
                cts = ctsGeneric = new CancellationTokenSource();
            }

            Task.Run(async () =>
            {
                await smGeneric.WaitAsync();
                try
                {

                    await Task.Delay(500, cts.Token);
                    cts.Token.ThrowIfCancellationRequested();

                    if (simconnect == null) return;

                    if (isGenericValueRegistered)
                    {
                        logger.LogInformation("Clearing Data definition");
                        simconnect.ClearDataDefinition(DEFINITIONS.GenericData);
                        isGenericValueRegistered = false;
                    }

                    if (genericValues.Count == 0)
                    {
                        logger.LogInformation("Registration is not needed.");
                    }
                    else
                    {
                        var log = "Registering generic data structure:";

                        foreach (TOGGLE_VALUE simValue in genericValues)
                        {
                            string value = simValue.ToString().Replace("__", ":").Replace("_", " ");
                            log += string.Format("\n- {0} {1}", simValue, value);

                            simconnect.AddToDataDefinition(
                                DEFINITIONS.GenericData,
                                value,
                                eventLib.GetUnit(simValue),
                                SIMCONNECT_DATATYPE.FLOAT64,
                                0.0f,
                                SimConnect.SIMCONNECT_UNUSED
                            );
                        }

                        logger.LogInformation(log);

                        simconnect.RegisterDataDefineStruct<GenericValuesStruct>(DEFINITIONS.GenericData);

                        isGenericValueRegistered = true;
                    }
                }
                catch (TaskCanceledException)
                {
                    logger.LogDebug("Registration is cancelled.");
                }
                finally
                {
                    smGeneric.Release();
                }
            });
        }

        private void RegisterGenericEvents()
        {
            if (simconnect == null) return;

            foreach (var toggleAction in genericEvents)
            {
                logger.LogInformation("RegisterEvent {action} {simConnectAction}", toggleAction, toggleAction.EventToSimConnectEvent());
                simconnect.MapClientEventToSimEvent(toggleAction, toggleAction.EventToSimConnectEvent());
            }
        }

        public void Trigger(TOGGLE_EVENT toggleAction, uint data = 0)
        {
            logger.LogInformation("Toggle {action} {data}", toggleAction, data);
            SendGenericCommand(toggleAction, data);
        }

        #endregion
    }


}
