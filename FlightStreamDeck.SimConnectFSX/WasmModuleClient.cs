using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Text;

namespace FlightStreamDeck.SimConnectFSX
{
	internal static class WasmModuleClient
	{
		public static void Stop(SimConnect simConnect)
		{
			if (simConnect != null)
			{
				SendWasmCmd(simConnect, "MF.SimVars.Clear");
			}
		}

		public static void GetLVarList(SimConnect simConnect)
		{
			if (simConnect != null)
			{
				SendWasmCmd(simConnect, "MF.LVars.List");
				DummyCommand(simConnect);
			}
		}

		public static void DummyCommand(SimConnect simConnect)
		{
			if (simConnect != null)
			{
				SendWasmCmd(simConnect, "MF.DummyCmd");
			}
		}

		public static void SendWasmCmd(SimConnect simConnect, string command)
		{
			simConnect?.SetClientData(SIMCONNECT_CLIENT_DATA_ID.MOBIFLIGHT_CMD, SIMCONNECT_CLIENT_DATA_ID.MOBIFLIGHT_LVARS, SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0u, new ClientDataString(command));
		}
	}
}
