using ColossalFramework;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Utils;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.Singleton
{
    internal class WTSCacheSingleton : SingletonLite<WTSCacheSingleton>
    {

        private readonly NonSequentialList<SegmentItemCache> m_cacheSegments = new NonSequentialList<SegmentItemCache>();
        private readonly NonSequentialList<VehicleItemCache> m_cacheVehicles = new NonSequentialList<VehicleItemCache>();
        private readonly NonSequentialList<CityTransportLineItemCache> m_cacheTransportLines = new NonSequentialList<CityTransportLineItemCache>();
        private readonly NonSequentialList<IntercityTransportLineItemCache> m_cacheIntercityTransportLines = new NonSequentialList<IntercityTransportLineItemCache>();
        private readonly NonSequentialList<DistrictItemCache> m_cacheDistricts = new NonSequentialList<DistrictItemCache>();
        private readonly NonSequentialList<ParkItemCache> m_cacheParks = new NonSequentialList<ParkItemCache>();
        private readonly NonSequentialList<BuildingItemCache> m_cacheBuildings = new NonSequentialList<BuildingItemCache>();

        private IEnumerator PurgeCache(CacheErasingFlags cacheFlags, InstanceID instanceID)
        {
            coroutineFlagsToErase |= cacheFlags;
            yield return 0;
            var objectsToIterate = m_cacheSegments.Values.Cast<IItemCache>()
                .Concat(m_cacheVehicles.Values.Cast<IItemCache>())
                .Concat(m_cacheTransportLines.Values.Cast<IItemCache>())
                .Concat(m_cacheIntercityTransportLines.Values.Cast<IItemCache>())
                .Concat(m_cacheDistricts.Values.Cast<IItemCache>())
                .Concat(m_cacheParks.Values.Cast<IItemCache>())
                .Concat(m_cacheBuildings.Values.Cast<IItemCache>()).ToList();
            for (int i = 0; i < objectsToIterate.Count; i++)
            {
                objectsToIterate[i].PurgeCache(coroutineFlagsToErase, instanceID);
                if (i % 100 == 99)
                {
                    yield return 0;
                }
            }
            coroutineFlagsToErase = 0;
            instance.lastCoroutine = null;
        }

        private CacheErasingFlags coroutineFlagsToErase;
        private Coroutine lastCoroutine;


        private static void DoClearCacheCoroutineStart(CacheErasingFlags flags)
        {
            if (!(instance.lastCoroutine is null))
            {
                WriteTheSignsMod.Controller.StopCoroutine(instance.lastCoroutine);
            }
            instance.lastCoroutine = WriteTheSignsMod.Controller?.StartCoroutine(instance.PurgeCache(flags, default));
        }

        public static void ClearCachePostalCode() => DoClearCacheCoroutineStart(CacheErasingFlags.PostalCodeParam);

        public static void ClearCacheSegmentNameParam() => DoClearCacheCoroutineStart(CacheErasingFlags.SegmentNameParam);

        public static void ClearCacheDistrictName() => DoClearCacheCoroutineStart(CacheErasingFlags.DistrictName | CacheErasingFlags.LineName);

        public static void ClearCacheCityName()
        {
            if (instance.m_cacheDistricts.TryGetValue(0, out DistrictItemCache cache))
            {
                cache.PurgeCache(CacheErasingFlags.DistrictName, default);
            }
        }

        public static void ClearCacheParkName() => DoClearCacheCoroutineStart(CacheErasingFlags.ParkName | CacheErasingFlags.LineName);
        public static void ClearCacheParkArea() => DoClearCacheCoroutineStart(CacheErasingFlags.ParkArea);
        public static void ClearCacheDistrictArea() => DoClearCacheCoroutineStart(CacheErasingFlags.DistrictArea);
        public static void ClearCacheBuildingName(ushort? buildingId)
        {
            if (buildingId is ushort buildingIdSh)
            {
                ClearCacheBuildingName(buildingIdSh);
            }
            else
            {
                DoClearCacheCoroutineStart(CacheErasingFlags.BuildingName | CacheErasingFlags.LineName);
            }
        }
        public static void ClearCacheBuildingName(ushort buildingId)
        {
            if (instance.m_cacheBuildings.TryGetValue(buildingId, out BuildingItemCache cache))
            {
                cache.PurgeCache(CacheErasingFlags.BuildingName, new InstanceID { Building = buildingId });
            }
        }
        public static void ClearCacheBuildingAll(ushort buildingId)
        {
            if (instance.m_cacheBuildings.TryGetValue(buildingId, out BuildingItemCache cache))
            {
                cache.PurgeCache(CacheErasingFlags.BuildingName | CacheErasingFlags.BuildingPosition, new InstanceID { Building = buildingId });
            }
        }

        public static void ClearCacheVehicleNumber(ushort vehicleID)
        {
            if (instance.m_cacheVehicles.TryGetValue(vehicleID, out VehicleItemCache cache))
            {
                cache.PurgeCache(CacheErasingFlags.VehicleParameters, new InstanceID { Vehicle = vehicleID });
            }
        }

        public static void ClearCacheSegmentSeed() => DoClearCacheCoroutineStart(CacheErasingFlags.SegmentNameParam | CacheErasingFlags.SegmentSize);

        public static void ClearCacheSegmentSize() => DoClearCacheCoroutineStart(CacheErasingFlags.SegmentSize);

        public static void ClearCacheSegmentSize(ushort segmentId)
        {
            if (instance.m_cacheSegments.TryGetValue(segmentId, out SegmentItemCache cache))
            {
                cache.PurgeCache(CacheErasingFlags.SegmentSize, new InstanceID { NetSegment = segmentId });
            }
        }
        public static void ClearCacheSegmentNameParam(ushort segmentId)
        {
            if (instance.m_cacheSegments.TryGetValue(segmentId, out SegmentItemCache cache))
            {
                cache.PurgeCache(CacheErasingFlags.SegmentNameParam, new InstanceID { NetSegment = segmentId });
            }
        }

        public static void ClearCacheVehicleNumber() => DoClearCacheCoroutineStart(CacheErasingFlags.VehicleParameters);

        public static void ClearCacheLineId() => DoClearCacheCoroutineStart(CacheErasingFlags.VehicleParameters | CacheErasingFlags.LineId | CacheErasingFlags.LineName);
        public static void ClearCacheLineName() => DoClearCacheCoroutineStart(CacheErasingFlags.LineName);
        public static void ClearCacheLineName(WTSLine line)
        {
            if (line.regional)
            {
                if (instance.m_cacheIntercityTransportLines.TryGetValue(line.lineId, out IntercityTransportLineItemCache cache))
                {
                    cache.PurgeCache(CacheErasingFlags.LineName, new InstanceID { NetNode = line.lineId });
                }
            }
            else
            {
                if (instance.m_cacheTransportLines.TryGetValue(line.lineId, out CityTransportLineItemCache cache))
                {
                    cache.PurgeCache(CacheErasingFlags.LineName, new InstanceID { TransportLine = line.lineId });
                }
            }

        }

        private T SafeGetter<T>(NonSequentialList<T> cacheList, long id) where T : IItemCache, new()
        {
            if (!cacheList.TryGetValue(id, out T result))
            {
                result = cacheList[id] = new T() { Id = id };
            }
            return result;
        }

        public SegmentItemCache GetSegment(ushort id) => SafeGetter(m_cacheSegments, id);
        public VehicleItemCache GetVehicle(ushort id) => SafeGetter(m_cacheVehicles, id);
        public ITransportLineItemCache GetATransportLine(WTSLine id) => id.regional ? GetIntercityTransportLine(id.lineId) : (ITransportLineItemCache)GetCityTransportLine(id.lineId);
        public CityTransportLineItemCache GetCityTransportLine(ushort id) => SafeGetter(m_cacheTransportLines, id);
        public IntercityTransportLineItemCache GetIntercityTransportLine(ushort id) => SafeGetter(m_cacheIntercityTransportLines, id);
        public DistrictItemCache GetDistrict(ushort id) => SafeGetter(m_cacheDistricts, id);
        public ParkItemCache GetPark(ushort id) => SafeGetter(m_cacheParks, id);
        public BuildingItemCache GetBuilding(ushort id) => SafeGetter(m_cacheBuildings, id);

        public static BasicRenderInformation GetTextData(string text, string prefix, string suffix, DynamicSpriteFont primaryFont, string overrideFont)
        {
            string str = $"{prefix}{text}{suffix}";
            return (FontServer.instance[overrideFont] ?? primaryFont ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY])?.DrawString(WriteTheSignsMod.Controller, str, default, FontServer.instance.ScaleEffective);
        }

    }
}

