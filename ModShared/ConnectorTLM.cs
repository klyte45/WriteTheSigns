extern alias TLM;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using TLM::Klyte.TransportLinesManager.Extensions;
using TLM::Klyte.TransportLinesManager.ModShared;
using UnityEngine;

namespace Klyte.WriteTheSigns.Connectors
{
    internal class ConnectorTLM : IConnectorTLM
    {
        public void Start()
        {
            TLMShared.Instance.EventLineSymbolParameterChanged += () =>
            {
                WriteTheSignsMod.Controller.SpriteRenderingRules.PurgeAllLines();
                RenderUtils.ClearCacheLineId();
            };
            TLMShared.Instance.EventAutoNameParameterChanged += () =>
            {
                WriteTheSignsMod.Controller.BuildingPropsSingleton.ResetLines();
                RenderUtils.ClearCacheLineName();
            };;
            TLMShared.Instance.EventVehicleIdentifierParameterChanged += RenderUtils.ClearCacheVehicleNumber;
        }

        public override Tuple<string, Color, string> GetLineLogoParameters(ushort lineID)
        {
            var result = TLMShared.GetIconStringParameters(lineID);
            return Tuple.New(result.First, result.Second, result.Third);
        }

        public override string GetStopName(ushort stopId, ushort lineId) => TLMShared.GetFullStationName(stopId, lineId, TransportSystemDefinition.GetDefinitionForLine(lineId).SubService);
        public override ushort GetStopBuildingInternal(ushort stopId, ushort lineId) => TLMShared.GetStationBuilding(stopId, lineId);
        public override string GetLineSortString(ushort lineId) => TLMShared.GetLineSortString(lineId, ref TransportManager.instance.m_lines.m_buffer[lineId]);

        public override string GetVehicleIdentifier(ushort vehicleId) => TLMShared.Instance.GetVehicleIdentifier(vehicleId);
        public override string GetLineIdString(ushort lineId) => TLMShared.GetLineStringId(lineId);
        public override void MapLineDestinations(ushort lineId)
        {
            TLMShared.CalculateAutoName(lineId, out ushort startStation, out ushort endStation, out string startStationStr, out string endStationStr);
            FillStops(lineId, startStation, endStation, startStationStr, endStationStr);
        }

       
    }
}

