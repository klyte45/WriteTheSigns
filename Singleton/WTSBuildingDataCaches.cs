using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Xml;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Utils
{
    internal static class WTSBuildingDataCaches
    {
        internal static Dictionary<uint, ushort> m_stopsBuildingsCache = new Dictionary<uint, ushort>();

        internal static void PurgeBuildingCache(ushort buildingId)
        {
            foreach (KeyValuePair<uint, ushort> item in m_stopsBuildingsCache.Where(kvp => kvp.Value == buildingId).ToList())
            {
                m_stopsBuildingsCache.Remove(item.Key);
            }
            WTSCacheSingleton.ClearCacheBuildingAll(buildingId);
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

        public static ushort GetStopBuilding(ushort stopId, WTSLine line)
        {
            var id = line.GetUniqueStopId(stopId);
            if (!m_stopsBuildingsCache.TryGetValue(id, out ushort buildingId))
            {
                buildingId = WriteTheSignsMod.Controller.ConnectorTLM.GetStopBuildingInternal(stopId, line);
                m_stopsBuildingsCache[id] = buildingId;
            }
            return buildingId;
        }

        internal static readonly Color[] m_colorOrder = new Color[]
        {
            Color.red,
            Color.Lerp(Color.red, Color.yellow,0.5f),
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.Lerp(Color.blue, Color.magenta,0.5f),
            Color.magenta,
            Color.white,
            Color.black,
            Color.Lerp( Color.red,                                    Color.black,0.5f),
            Color.Lerp( Color.Lerp(Color.red, Color.yellow,0.5f),     Color.black,0.5f),
            Color.Lerp( Color.yellow,                                 Color.black,0.5f),
            Color.Lerp( Color.green,                                  Color.black,0.5f),
            Color.Lerp( Color.cyan,                                   Color.black,0.5f),
            Color.Lerp( Color.blue,                                   Color.black,0.5f),
            Color.Lerp( Color.Lerp(Color.blue, Color.magenta,0.5f),   Color.black,0.5f),
            Color.Lerp( Color.magenta,                                Color.black,0.5f),
            Color.Lerp( Color.white,                                  Color.black,0.25f),
            Color.Lerp( Color.white,                                  Color.black,0.75f)
        };


    }
}

