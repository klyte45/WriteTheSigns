using Klyte.Commons.Utils;
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
        protected static void FillStops(ushort lineId, ushort startStation, ushort endStation, string startStationStr, string endStationStr)
        {
            ref TransportLine tl = ref TransportManager.instance.m_lines.m_buffer[lineId];
            ref NetNode[] nodes = ref NetManager.instance.m_nodes.m_buffer;

            var firstStop = startStation == 0 ? tl.m_stops : startStation;
            var curStop = firstStop;
            var nextStop = TransportLine.GetNextStop(curStop);
            ushort prevStop = TransportLine.GetPrevStop(curStop);
            var destination = endStation == 0 ? startStation : endStation;
            var destinationStr = endStation == 0 ? startStationStr : endStationStr;
            var buildingSing = WriteTheSignsMod.Controller.BuildingPropsSingleton;
            do
            {
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
                buildingSing.m_stopInformation[curStop] = new Xml.StopInformation
                {
                    m_lineId = lineId,
                    m_destinationId = destination,
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
    }
}