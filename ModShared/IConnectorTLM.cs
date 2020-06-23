using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.WriteTheSigns.Connectors
{
    internal interface IConnectorTLM
    {
        Tuple<string, Color, string> GetLineLogoParameters(ushort lineID);
        ushort GetStopBuildingInternal(ushort stopId, ushort lineId);
        string GetStopName(ushort stopId, ushort lineId);
        string GetLineSortString(ushort lineId);
        string GetVehicleIdentifier(ushort vehicleId);
        string GetLineIdString(ushort lineId);
        void MapLineDestinations(ushort lineId);
    }
}