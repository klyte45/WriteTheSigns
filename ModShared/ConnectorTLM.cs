extern alias TLM;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using TLM::Klyte.TransportLinesManager.Extensors;
using TLM::Klyte.TransportLinesManager.ModShared;
using TLM::Klyte.TransportLinesManager.Utils;
using UnityEngine;

namespace Klyte.WriteTheSigns.Connectors
{
    internal class ConnectorTLM : MonoBehaviour, IConnectorTLM
    {
        public void Start()
        {
            TLMShared.Instance.EventLineSymbolParameterChanged += () =>
            {
                WriteTheSignsMod.Controller.SpriteRenderingRules.PurgeAllLines();
                RenderUtils.ClearCacheLineId();
            };
            TLMShared.Instance.EventAutoNameParameterChanged += WriteTheSignsMod.Controller.BuildingPropsSingleton.ResetLines;
        }

        public Tuple<string, Color, string> GetLineLogoParameters(ushort lineID)
        {
            var result = TLMShared.GetIconStringParameters(lineID);
            return Tuple.New(result.First, result.Second, result.Third);
        }

        public string GetStopName(ushort stopId, ushort lineId) => TLMShared.GetFullStationName(stopId, lineId, TransportSystemDefinition.GetDefinitionForLine(lineId).SubService);
        public ushort GetStopBuildingInternal(ushort stopId, ushort lineId) => TLMShared.GetStationBuilding(stopId, lineId);
        public string GetLineSortString(ushort lineId) => TLMShared.GetLineSortString(lineId, ref TransportManager.instance.m_lines.m_buffer[lineId]);

        public string GetVehicleIdentifier(ushort vehicleId) => TLMShared.Instance.GetVehicleIdentifier(vehicleId);
        public string GetLineIdString(ushort lineId) => TLMShared.GetLineStringId(lineId);
        public void MapLineDestinations(ushort lineId)
        {
            TLMShared.CalculateAutoName(lineId, out ushort startStation, out ushort endStation, out string startStationStr, out string endStationStr);
            ref TransportLine tl = ref TransportManager.instance.m_lines.m_buffer[lineId];
            ref NetNode[] nodes = ref NetManager.instance.m_nodes.m_buffer;

            var firstStop = startStation == 0 ? tl.m_stops : startStation;
            var nextStop = firstStop;
            var curStop = TransportLine.GetPrevStop(nextStop);
            ushort prevStop = TransportLine.GetPrevStop(curStop);
            var destination = endStation == 0 ? startStation : endStation;
            var destinationStr = endStation == 0 ? startStationStr : endStationStr;
            var buildingSing = WriteTheSignsMod.Controller.BuildingPropsSingleton;
            do
            {
                buildingSing.m_stopInformation[curStop] = new Xml.StopInformation
                {
                    m_lineId = lineId,
                    m_destinationId = destination,
                    m_nextStopId = nextStop,
                    m_previousStopId = prevStop,
                    m_stopId = curStop,
                    m_destinationString = destinationStr
                };
                prevStop = curStop;
                curStop = nextStop;
                nextStop = TransportLine.GetNextStop(nextStop);
                if (curStop == startStation && endStation > 0)
                {
                    destination = endStation;
                    destinationStr = endStationStr;
                }
                else if (curStop == endStation)
                {
                    destination = startStation;
                    destinationStr = startStationStr;
                }
            } while (nextStop != 0 && nextStop != startStation);
        }
    }
}

