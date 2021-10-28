extern alias TLM;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using System.Collections.Generic;
using System.Linq;
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
            TLMFacade.Instance.EventAutoNameParameterChanged += OnAutoNameParameterChanged;
            TLMFacade.Instance.EventVehicleIdentifierParameterChanged += RenderUtils.ClearCacheVehicleNumber;
            TLMFacade.Instance.EventLineDestinationsChanged += (lineId) =>
            {
                WriteTheSignsMod.Controller.BuildingPropsSingleton.ResetLines();
                RenderUtils.ClearCacheLineName(new WTSLine(lineId, false));
            };
            TLMFacade.Instance.EventRegionalLineParameterChanged += (lineId) =>
            {
                WriteTheSignsMod.Controller.BuildingPropsSingleton.ResetLines();
                RenderUtils.ClearCacheLineName(new WTSLine(lineId, true));
                WriteTheSignsMod.Controller.AtlasesLibrary.PurgeLine(new WTSLine(lineId, true));
            };
        }

        public override Tuple<string, Color, string> GetLineLogoParameters(WTSLine lineObj)
        {
            var result = TLMFacade.GetIconStringParameters((ushort)lineObj.lineId, lineObj.regional);
            return Tuple.New(result.First, result.Second, result.Third);
        }

        public override string GetStopName(ushort stopId, WTSLine lineObj)
            => TLMFacade.GetFullStationName(
                stopId,
                (ushort)lineObj.lineId,
                lineObj.regional,
                TransportSystemDefinition.GetDefinitionForLine((ushort)lineObj.lineId, lineObj.regional)?.SubService ?? (lineObj.regional ? NetManager.instance.m_nodes.m_buffer[lineObj.lineId].Info.GetSubService() : default)
                );
        public override ushort GetStopBuildingInternal(ushort stopId, WTSLine lineObj) => TLMFacade.GetStationBuilding(stopId, (ushort)lineObj.lineId, lineObj.regional);
        public override string GetLineSortString(WTSLine lineObj) => TLMFacade.GetLineSortString((ushort)lineObj.lineId, lineObj.regional);

        public override string GetVehicleIdentifier(ushort vehicleId) => TLMFacade.Instance.GetVehicleIdentifier(vehicleId);
        public override WTSLine GetVehicleLine(ushort vehicleId) => new WTSLine(TLMFacade.Instance.GetVehicleLine(vehicleId, out bool regional), regional);
        public override string GetLineIdString(WTSLine lineObj) => TLMFacade.GetLineStringId((ushort)lineObj.lineId, lineObj.regional);
        public override void MapLineDestinations(WTSLine lineObj)
        {
            TLMFacade.CalculateAutoName((ushort)lineObj.lineId, lineObj.regional, out List<TLMFacade.DestinationPoco> destinations);
            FillStops(lineObj, destinations.Select(x => new DestinationPoco { stopId = x.stopId, stopName = x.stopName }).ToList());
        }

        public override WTSLine GetStopLine(ushort stopId) => new WTSLine(TLMFacade.GetStopLine(stopId, out bool isBuilding), isBuilding);
        internal override string GetLineName(WTSLine lineObj) => TLMFacade.GetLineName((ushort)lineObj.lineId, lineObj.regional);
        internal override Color GetLineColor(WTSLine lineObj) => TLMFacade.GetLineColor((ushort)lineObj.lineId, lineObj.regional);

        public class DestinationPoco
        {
            public string stopName;
            public ushort stopId;
        }
    }
}

