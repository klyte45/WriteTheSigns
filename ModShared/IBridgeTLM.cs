﻿using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.ModShared
{
    internal abstract class IBridgeTLM : MonoBehaviour
    {
        public abstract Tuple<string, Color, string> GetLineLogoParameters(ushort lineID);
        public abstract ushort GetStopBuildingInternal(ushort stopId, ushort lineId);
        public abstract string GetStopName(ushort stopId, ushort lineId);
        public abstract string GetLineSortString(ushort lineId);
        public abstract string GetVehicleIdentifier(ushort vehicleId);
        public abstract string GetLineIdString(ushort lineId);
        public abstract void MapLineDestinations(ushort lineId);
        protected static void FillStops(ushort lineId, List<BridgeTLM.DestinationPoco> destinations)
        {
            ref TransportLine tl = ref TransportManager.instance.m_lines.m_buffer[lineId];
            ref NetNode[] nodes = ref NetManager.instance.m_nodes.m_buffer;

            if (destinations.Count == 0)
            {
                destinations.Add(new BridgeTLM.DestinationPoco { stopId = destinations[0].stopId, stopName = null });
            }

            var firstStop = destinations[0].stopId;
            var curStop = firstStop;
            var nextStop = TransportLine.GetNextStop(curStop);
            ushort prevStop = TransportLine.GetPrevStop(curStop);
            var destinationId = destinations[1 % destinations.Count].stopId;
            var destinationStr = destinations[1 % destinations.Count].stopName;
            var buildingSing = WriteTheSignsMod.Controller.BuildingPropsSingleton;
            do
            {
                var destinationInfo = destinations.Where(x => x.stopId == curStop).FirstOrDefault();
                if (!(destinationInfo is null))
                {
                    var nextDest = destinations.IndexOf(destinationInfo) + 1;
                    destinationId = destinations[nextDest % destinations.Count].stopId;
                    destinationStr = destinations[nextDest % destinations.Count].stopName;
                    if (destinationStr is null)
                    {
                        destinationStr = WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(destinationId, lineId);
                    }
                }

                buildingSing.m_stopInformation[curStop] = new Xml.StopInformation
                {
                    m_lineId = lineId,
                    m_destinationId = destinationId,
                    m_nextStopId = nextStop,
                    m_previousStopId = prevStop,
                    m_stopId = curStop,
                    m_destinationString = destinationStr
                };
                //  LogUtils.DoWarnLog($"([{tl.m_lineNumber}] - {tl.Info}){buildingSing.m_stopInformation[curStop] }  || { GetStopName(curStop, lineId)} || FS={firstStop} ({startStation}-{endStation})");
                prevStop = curStop;
                curStop = nextStop;
                nextStop = TransportLine.GetNextStop(nextStop);

            } while (curStop != 0 && curStop != firstStop);
        }

        internal void OnAutoNameParameterChanged()
        {
            WriteTheSignsMod.Controller.BuildingPropsSingleton.ResetLines();
            RenderUtils.ClearCacheLineName();
        }
    }
}