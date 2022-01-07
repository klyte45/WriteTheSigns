using Klyte.Commons.Utils;

namespace Klyte.WriteTheSigns.Rendering
{
    public class BuildingItemCache : IItemCache
    {
        public ushort buildingId;
        public long? Id { get => buildingId; set => buildingId = (ushort)(value ?? 0); }

        public FormatableString Name
        {
            get
            {
                if (name is null)
                {
                    name = new FormatableString(BuildingManager.instance.GetBuildingName(buildingId, InstanceID.Empty) ?? "");
                }
                return name;
            }
        }
        public ushort SegmentId
        {
            get
            {
                if (segmentId is null)
                {
                    UpdateAddressFields();
                }
                return segmentId ?? 0;
            }
        }


        public int AddressNumber
        {
            get
            {
                if (addressNumber is null)
                {
                    UpdateAddressFields();
                }
                return addressNumber ?? 0;
            }
        }
        private void UpdateAddressFields()
        {
            ref Building b = ref BuildingManager.instance.m_buildings.m_buffer[buildingId];
            SegmentUtils.GetNearestSegment(b.CalculateSidewalkPosition(), out _, out float targetLength, out ushort segmentIdFound);
            if (segmentIdFound > 0)
            {
                var hwData = WriteTheSignsMod.Controller.ConnectorADR.GetHighwayData(NetManager.instance.m_segments.m_buffer[segmentIdFound].m_nameSeed);
                addressNumber = SegmentUtils.CalculateBuildingAddressNumber(b.m_position, segmentIdFound, targetLength, b.m_position, hwData?.invertMileage ?? false, hwData?.mileageOffset ?? 0);
                segmentId = segmentIdFound;
            }
            else
            {
                addressNumber = 0;
                segmentId = 0;
            }
        }

        private FormatableString name = null;
        private ushort? segmentId;
        private int? addressNumber;
        public void PurgeCache(CacheErasingFlags cacheToPurge, InstanceID refID)
        {
            if (cacheToPurge.Has(CacheErasingFlags.BuildingName))
            {
                name = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.SegmentSize) || (cacheToPurge.Has(CacheErasingFlags.BuildingPosition) && refID.Building == buildingId))
            {
                segmentId = null;
                addressNumber = null;
            }
        }
    }

}
