using Klyte.WriteTheSigns.Xml;
using System.Linq;

namespace Klyte.WriteTheSigns.Utils
{
    internal static class WTSStopUtils
    {
        internal static StopInformation[] GetAllTargetStopInfo(BoardInstanceBuildingXml descriptor, ushort buildingId)
            => descriptor?.m_platforms?.SelectMany(platform =>
            {
                if (WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId] != null && WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId]?.ElementAtOrDefault(platform)?.Length > 0)
                {
                    StopInformation[] line = WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId][platform];
                    return line;
                }
                return new StopInformation[0];
            }).ToArray();

        internal static StopInformation[] GetTargetStopInfo(BoardInstanceBuildingXml descriptor, ushort buildingId)
        {
            foreach (int platform in descriptor.m_platforms)
            {
                if (WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId] != null && WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId].ElementAtOrDefault(platform)?.Length > 0)
                {
                    StopInformation[] line = WriteTheSignsMod.Controller.BuildingPropsSingleton.m_platformToLine[buildingId][platform];
                    return line;
                }
            }
            return m_emptyInfo;
        }
        internal static StopInformation GetStopDestinationData(ushort targetStopId)
        {
            var result = WriteTheSignsMod.Controller.BuildingPropsSingleton.m_stopInformation[targetStopId];
            if (result.m_lineId == 0)
            {
                var line = WriteTheSignsMod.Controller.ConnectorTLM.GetStopLine(targetStopId);
                if (line.lineId > 0)
                {
                    WriteTheSignsMod.Controller.ConnectorTLM.MapLineDestinations(line);
                    result = WriteTheSignsMod.Controller.BuildingPropsSingleton.m_stopInformation[targetStopId];
                }
                else
                {
                    result.m_lineId = ushort.MaxValue;
                }
            }
            return result;
        }


        internal static readonly StopInformation[] m_emptyInfo = new StopInformation[0];
    }
}

