extern alias TLM;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using TLM::Klyte.TransportLinesManager.Extensions;
using TLM::Klyte.TransportLinesManager.ModShared;
using UnityEngine;

namespace Klyte.WriteTheSigns.ModShared
{
    internal class BridgeTLM : IBridgeTLM
    {
        public void Start()
        {
            TLMFacade.Instance.EventLineSymbolParameterChanged += () =>
            {
                WriteTheSignsMod.Controller.AtlasesLibrary.PurgeAllLines();
                RenderUtils.ClearCacheLineId();
            };
            TLMFacade.Instance.EventAutoNameParameterChanged += () =>
            {
                WriteTheSignsMod.Controller.BuildingPropsSingleton.ResetLines();
                RenderUtils.ClearCacheLineName();
            };;
            TLMFacade.Instance.EventVehicleIdentifierParameterChanged += RenderUtils.ClearCacheVehicleNumber;
        }

        public override Tuple<string, Color, string> GetLineLogoParameters(ushort lineID)
        {
            var result = TLMFacade.GetIconStringParameters(lineID);
            return Tuple.New(result.First, result.Second, result.Third);
        }

        public override string GetStopName(ushort stopId, ushort lineId) => TLMFacade.GetFullStationName(stopId, lineId, TransportSystemDefinition.GetDefinitionForLine(lineId).SubService);
        public override ushort GetStopBuildingInternal(ushort stopId, ushort lineId) => TLMFacade.GetStationBuilding(stopId, lineId);
        public override string GetLineSortString(ushort lineId) => TLMFacade.GetLineSortString(lineId, ref TransportManager.instance.m_lines.m_buffer[lineId]);

        public override string GetVehicleIdentifier(ushort vehicleId) => TLMFacade.Instance.GetVehicleIdentifier(vehicleId);
        public override string GetLineIdString(ushort lineId) => TLMFacade.GetLineStringId(lineId);
        public override void MapLineDestinations(ushort lineId)
        {
            TLMFacade.CalculateAutoName(lineId, out ushort startStation, out ushort endStation, out string startStationStr, out string endStationStr);
            FillStops(lineId, startStation, endStation, startStationStr, endStationStr);
        }

       
    }
}

