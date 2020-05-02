using ColossalFramework;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Utils
{

    public static class WTSRoadNodeCommons
    {
        public static void GetIncomingOutcomingTraffic(ushort nodeID, out HashSet<ushort> incomingTraffic, out HashSet<ushort> outcomingTraffic, out int[] rotationOrder, out int[] angles)
        {
            incomingTraffic = new HashSet<ushort>();
            outcomingTraffic = new HashSet<ushort>();
            var appointingAngle = new List<Tuple<int, float>>();
            for (int i = 0; i < 8; i++)
            {
                ushort segmentIid = NetManager.instance.m_nodes.m_buffer[nodeID].GetSegment(i);
                if (segmentIid != 0)
                {
                    ref NetSegment segment = ref Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid];
                    bool invertFlagI = (segment.m_flags & NetSegment.Flags.Invert) != 0;
                    bool isStart = segment.m_startNode == nodeID;
                    bool isSegmentIinverted = invertFlagI == isStart != (SimulationManager.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.False);
                    if ((isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasBackwardVehicleLanes) || (!isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasForwardVehicleLanes))
                    {
                        incomingTraffic.Add(segmentIid);
                    }
                    if ((!isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasBackwardVehicleLanes) || (isSegmentIinverted && Singleton<NetManager>.instance.m_segments.m_buffer[segmentIid].Info.m_hasForwardVehicleLanes))
                    {
                        outcomingTraffic.Add(segmentIid);
                    }
                    appointingAngle.Add(Tuple.New(i, (isStart ? segment.m_startDirection : segment.m_endDirection).GetAngleXZ()));
                }
            }
            IOrderedEnumerable<Tuple<int, float>> x = appointingAngle.OrderBy(x => x.Second);
            rotationOrder = x.Select(x => x.First).ToArray();
            angles = x.Select(x => Mathf.RoundToInt(x.Second)).ToArray();
        }
        public static bool CheckSegmentInverted(ushort nodeID, ref NetSegment netSegmentJ)
        {
            bool invertFlagJ = (netSegmentJ.m_flags & NetSegment.Flags.Invert) != 0;
            bool isSegmentJinverted = invertFlagJ == (netSegmentJ.m_startNode == nodeID) == (SimulationManager.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.False);
            return isSegmentJinverted;
        }
    }
}
