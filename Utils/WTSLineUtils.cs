using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheSigns.Utils
{
    internal class WTSLineUtils
    {
        private static Dictionary<uint, ushort> m_stopsBuildingsCache = new Dictionary<uint, ushort>();

        internal static void PurgeBuildingCache(ushort buildingId)
        {
            foreach (KeyValuePair<uint, ushort> item in m_stopsBuildingsCache.Where(kvp => kvp.Value == buildingId).ToList())
            {
                m_stopsBuildingsCache.Remove(item.Key);
            }
        }
        internal static void PurgeStopCache(ushort stopId)
        {
            foreach (KeyValuePair<uint, ushort> item in m_stopsBuildingsCache.Where(kvp => (kvp.Key & 0xFFFF) == stopId).ToList())
            {
                m_stopsBuildingsCache.Remove(item.Key);
            }
        }
        internal static void PurgeLineCache(ushort lineId)
        {
            foreach (KeyValuePair<uint, ushort> item in m_stopsBuildingsCache.Where(kvp => ((kvp.Key & 0xFFFF0000) >> 16) == lineId).ToList())
            {
                m_stopsBuildingsCache.Remove(item.Key);
            }
        }

        public static ushort GetStopBuilding(ushort stopId, ushort lineId)
        {
            uint id = (uint) ((lineId << 16) | stopId);
            if (!m_stopsBuildingsCache.TryGetValue(id, out ushort buildingId))
            {
                buildingId = WTSHookable.GetStopBuildingInternal(stopId, lineId);
                m_stopsBuildingsCache[id] = buildingId;
            }
            return buildingId;
        }
    }
}

