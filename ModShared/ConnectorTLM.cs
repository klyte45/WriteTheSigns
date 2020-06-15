extern alias TLM;
using Klyte.Commons.Utils;
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
            TLMShared.Instance.EventLineSymbolParameterChanged += WriteTheSignsMod.Controller.TransportLineRenderingRules.PurgeAllLines;
            TLMShared.Instance.EventLineSymbolParameterChanged += WriteTheSignsMod.Controller.TransportLineRenderingRules.PurgeAllLines;
        }

        public Tuple<string, Color, string> GetLineLogoParameters(ushort lineID)
        {
            var result = TLMLineUtils.GetIconStringParameters(lineID);
            return Tuple.New(result.First, result.Second, result.Third);
        }

        public string GetStopName(ushort stopId, ushort lineId) => TLMLineUtils.getStationName(stopId, lineId, TransportSystemDefinition.GetDefinitionForLine(lineId).SubService);

        public ushort GetStopBuildingInternal(ushort stopId, ushort lineId) => TLMLineUtils.getStationBuilding(stopId, TransportSystemDefinition.GetDefinitionForLine(lineId).SubService, true);
        public string GetLineSortString(ushort lineId) => TLMLineUtils.GetLineSortString(lineId, ref TransportManager.instance.m_lines.m_buffer[lineId]);
    }
}

