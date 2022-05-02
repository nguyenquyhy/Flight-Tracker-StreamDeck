using FlightStreamDeck.Core;
using FlightStreamDeck.Logics;
using Microsoft.Extensions.Logging;
using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FlightStreamDeck.SimConnectFSX
{
    public class SimConnectFlightConnector : IFlightConnector
    {
        public event EventHandler<AircraftStatusUpdatedEventArgs> AircraftStatusUpdated;
        public event EventHandler<ToggleValueUpdatedEventArgs> GenericValuesUpdated;
        public event EventHandler<InvalidEventRegisteredEventArgs> InvalidEventRegistered;
        public event EventHandler Closed;

        // Extra SimConnect functions via native pointer
        IntPtr hSimConnect;
        [DllImport("SimConnect.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int /* HRESULT */ SimConnect_GetLastSentPacketID(IntPtr hSimConnect, out uint /* DWORD */ dwSendID);

        private const int StatusDelayMilliseconds = 100;

        /// <summary>
        /// This is a reference counter to make sure we do not deregister variables that are still in use.
        /// </summary>
        private readonly Dictionary<(TOGGLE_VALUE variables, string? unit), int> genericValues = new();

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
        public IntPtr HandleSimConnectEvents(int message, ref bool isHandled)
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

            // Get direct access to the SimConnect handle, to use functions otherwise not supported.
            FieldInfo fiSimConnect = typeof(SimConnect).GetField("hSimConnect", BindingFlags.NonPublic | BindingFlags.Instance);
            hSimConnect = (IntPtr)fiSimConnect.GetValue(simconnect);

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

        private void SendGenericCommand(Enum sendingEvent, uint dwData = 0)
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
            void AddToFlightStatusDefinition(string simvar, string unit, SIMCONNECT_DATATYPE type)
            {
                simconnect.AddToDataDefinition(DEFINITIONS.FlightStatus, simvar, unit, type, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            }

            AddToFlightStatusDefinition("SIMULATION RATE", "number", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("PLANE LATITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE LONGITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE ALTITUDE", "Feet", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE ALT ABOVE GROUND", "Feet", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE PITCH DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE BANK DEGREES", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("PLANE HEADING DEGREES MAGNETIC", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("GROUND ALTITUDE", "Meters", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("VERTICAL SPEED", "Feet per minute", SIMCONNECT_DATATYPE.FLOAT64);

            AddToFlightStatusDefinition("FUEL TOTAL QUANTITY", "Gallons", SIMCONNECT_DATATYPE.FLOAT64);

            AddToFlightStatusDefinition("AMBIENT WIND VELOCITY", "Feet per second", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("AMBIENT WIND DIRECTION", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);

            AddToFlightStatusDefinition("SIM ON GROUND", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("STALL WARNING", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("OVERSPEED WARNING", "number", SIMCONNECT_DATATYPE.INT32);

            #region Autopilot

            AddToFlightStatusDefinition("AUTOPILOT MASTER", "number", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("AUTOPILOT HEADING LOCK", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("AUTOPILOT HEADING LOCK DIR", "Degrees", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("AUTOPILOT NAV1 LOCK", "number", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("AUTOPILOT APPROACH HOLD", "number", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("AUTOPILOT ALTITUDE LOCK", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("AUTOPILOT ALTITUDE LOCK VAR", "Feet", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("AUTOPILOT ALTITUDE LOCK VAR:1", "Feet", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("AUTOPILOT VERTICAL HOLD", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("AUTOPILOT VERTICAL HOLD VAR", "Feet per minute", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("AUTOPILOT FLIGHT LEVEL CHANGE", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("AUTOPILOT AIRSPEED HOLD VAR", "Knots", SIMCONNECT_DATATYPE.INT32);

            #endregion

            AddToFlightStatusDefinition("KOHLSMAN SETTING MB", "number", SIMCONNECT_DATATYPE.INT32);

            AddToFlightStatusDefinition("TRANSPONDER CODE:1", "Hz", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("COM ACTIVE FREQUENCY:1", "kHz", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("COM ACTIVE FREQUENCY:2", "kHz", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("AVIONICS MASTER SWITCH", "number", SIMCONNECT_DATATYPE.INT32);
            AddToFlightStatusDefinition("NAV OBS:1", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("NAV OBS:2", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);
            AddToFlightStatusDefinition("ADF CARD", "Degrees", SIMCONNECT_DATATYPE.FLOAT64);

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
                                    ApAltitude0 = flightStatus.Value.ApAlt0,
                                    ApAltitude1 = flightStatus.Value.ApAlt1,
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
                        var result = new Dictionary<(TOGGLE_VALUE variable, string? unit), double>();
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
                                var genericValue = genericValues.Keys.ElementAt(i);
                                result.Add(genericValue, dataArray.Value.Get(i));
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
            logger.LogError("Exception received: {error} from {sendID}", (SIMCONNECT_EXCEPTION)data.dwException, data.dwSendID);
            switch ((SIMCONNECT_EXCEPTION)data.dwException)
            {
                case SIMCONNECT_EXCEPTION.ERROR:
                    // Try to reconnect on unknown error
                    CloseConnection();
                    Closed?.Invoke(this, new EventArgs());
                    break;

                case SIMCONNECT_EXCEPTION.NAME_UNRECOGNIZED:
                    InvalidEventRegistered?.Invoke(this, new InvalidEventRegisteredEventArgs(data.dwSendID));
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
            // 0xC000014B: STATUS_PIPE_BROKEN
            logger.LogError(exception, "Exception received");
            CloseConnection();
            Closed?.Invoke(this, new EventArgs());
        }

        private uint GetLastSendID()
        {
            SimConnect_GetLastSentPacketID(hSimConnect, out uint dwSendID);
            return dwSendID;
        }

        #region Generic Buttons

        public uint? RegisterToggleEvent(Enum eventEnum, string eventName)
        {
            if (simconnect == null) return null;

            logger.LogInformation("RegisterEvent {action} {simConnectAction}", eventEnum, eventName);
            simconnect.MapClientEventToSimEvent(eventEnum, eventName);

            return GetLastSendID();
        }

        public void RegisterSimValues(params (TOGGLE_VALUE variables, string? unit)[] simValues)
        {
            var changed = false;
            lock (lockLists)
            {
                logger.LogInformation("Registering {values}", string.Join(", ", simValues));
                foreach (var simValue in simValues)
                {
                    if (genericValues.ContainsKey(simValue))
                    {
                        genericValues[simValue]++;
                    }
                    else
                    {
                        genericValues.Add(simValue, 1);
                        changed = true;
                    }
                }
            }
            if (changed)
            {
                RegisterGenericValues();
            }
        }

        public void DeRegisterSimValues(params (TOGGLE_VALUE variables, string? unit)[] simValues)
        {
            var changed = false;
            lock (lockLists)
            {
                logger.LogInformation("De-Registering {values}", string.Join(", ", simValues));
                foreach (var simValue in simValues)
                {
                    if (genericValues.ContainsKey(simValue))
                    {
                        var currentCount = genericValues[simValue];
                        if (currentCount > 1)
                        {
                            genericValues[simValue]--;
                        }
                        else
                        {
                            genericValues.Remove(simValue);
                            changed = true;
                        }
                    }
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

                        foreach ((var simValue, var unit) in genericValues.Keys)
                        {
                            string value = simValue.ToString().Replace("__", ":").Replace("_", " ");
                            var simUnit = EventValueLibrary.GetUnit(simValue, unit);
                            log += string.Format("\n- {0} {1} {2}", simValue, value, simUnit);

                            simconnect.AddToDataDefinition(
                                DEFINITIONS.GenericData,
                                value,
                                simUnit,
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

        public void Trigger(Enum eventEnum, uint data)
        {
            logger.LogInformation("Toggle {event} {data}", eventEnum, data);
            SendGenericCommand(eventEnum, data);
        }

        #endregion
    }


}
