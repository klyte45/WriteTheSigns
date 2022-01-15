using ColossalFramework.Globalization;
using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.WriteTheSigns.Rendering
{
    public class SegmentItemCache : IItemCache
    {
        public ushort segmentId;
        public long? Id { get => segmentId; set => segmentId = (ushort)(value ?? 0); }
        public FormatableString FullStreetName
        {
            get
            {
                if (fullStreetName is null)
                {
                    fullStreetName = new FormatableString(WriteTheSignsMod.Controller.ConnectorADR.GetStreetFullName(segmentId));
                }
                return fullStreetName;
            }
        }
        public FormatableString StreetName
        {
            get
            {
                if (streetName is null)
                {
                    streetName = new FormatableString((NetManager.instance.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.CustomName) == 0
                        ? WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffix(segmentId)
                        : WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffixCustom(segmentId));
                }
                return streetName;
            }
        }
        public FormatableString StreetQualifier
        {
            get
            {
                if (streetQualifier is null)
                {
                    streetQualifier = new FormatableString((NetManager.instance.m_segments.m_buffer[segmentId].m_flags & NetSegment.Flags.CustomName) == 0
                        ? WriteTheSignsMod.Controller.ConnectorADR.GetStreetQualifier(segmentId)
                        : WriteTheSignsMod.Controller.ConnectorADR.GetStreetQualifierCustom(segmentId));
                }
                return streetQualifier;
            }
        }

        public string PostalCode
        {
            get
            {
                if (postalCode is null)
                {
                    postalCode = WriteTheSignsMod.Controller.ConnectorADR.GetStreetPostalCode(NetManager.instance.m_segments.m_buffer[segmentId].m_middlePosition, segmentId);
                }
                return postalCode;
            }
        }

        public byte ParkId
        {
            get
            {
                if (parkId is null)
                {
                    parkId = DistrictManager.instance.GetPark(NetManager.instance.m_segments.m_buffer[segmentId].m_middlePosition);
                }
                return parkId ?? 0;
            }
        }

        public byte DistrictId
        {
            get
            {
                if (districtId is null)
                {
                    districtId = DistrictManager.instance.GetDistrict(NetManager.instance.m_segments.m_buffer[segmentId].m_middlePosition);
                }
                return districtId ?? 0;
            }
        }
        public string Direction
        {
            get
            {
                if (direction8 is null)
                {
                    direction8 = WriteTheSignsMod.Controller.ConnectorADR.GetDirection(segmentId);
                }
                return Locale.Get("K45_CMNS_CARDINALPOINT_SHORT", CardinalPoint.m_cardinal16[(direction8 ?? 0) * 2]);
            }
        }

        public int StartMileageMeters
        {
            get
            {
                if (startMileageMeters is null)
                {
                    FillHwParams();
                }
                return startMileageMeters ?? 0;
            }
        }

        public int EndMileageMeters
        {
            get
            {
                if (endMileageMeters is null)
                {
                    FillHwParams();
                }
                return endMileageMeters ?? 0;
            }
        }

        public ushort OutsideConnectionId
        {
            get
            {
                if (outsideConnectionId is null)
                {
                    ref NetSegment currSegment = ref NetManager.instance.m_segments.m_buffer[segmentId];
                    ref NetNode nodeEnd = ref NetManager.instance.m_nodes.m_buffer[currSegment.m_endNode];
                    ref NetNode nodeStart = ref NetManager.instance.m_nodes.m_buffer[currSegment.m_startNode];
                    outsideConnectionId = nodeEnd.m_building > 0 && BuildingManager.instance.m_buildings.m_buffer[nodeEnd.m_building].Info.m_buildingAI is OutsideConnectionAI
                        ? nodeEnd.m_building
                        : nodeStart.m_building > 0 && BuildingManager.instance.m_buildings.m_buffer[nodeStart.m_building].Info.m_buildingAI is OutsideConnectionAI
                            ? nodeStart.m_building
                            : (ushort?)0;
                }
                return outsideConnectionId ?? 0;
            }
        }

        public float DistanceFromCenter
        {
            get
            {
                if (distanceFromCenter is null)
                {
                    distanceFromCenter = WriteTheSignsMod.Controller.ConnectorADR.GetDistanceFromCenter(segmentId);
                }
                return distanceFromCenter ?? 0;
            }
        }
        public string HwCodeShort
        {
            get
            {
                if (hwCodeShort is null)
                {
                    FillHwParams();
                }
                return hwCodeShort;
            }
        }


        public string HwCodeLong
        {
            get
            {
                if (hwCodeLong is null)
                {
                    FillHwParams();
                }
                return hwCodeLong;
            }
        }
        public string HwDettachedPrefix
        {
            get
            {
                if (hwDettachedPrefix is null)
                {
                    FillHwParams();
                }
                return hwDettachedPrefix;
            }
        }
        public string HwIdentifierSuffix
        {
            get
            {
                if (hwIdentifierSuffix is null)
                {
                    FillHwParams();
                }
                return hwIdentifierSuffix;
            }
        }

        private FormatableString fullStreetName;
        private FormatableString streetName;
        private FormatableString streetQualifier;
        private string postalCode;
        private byte? parkId;
        private byte? districtId;
        private int? startMileageMeters;
        private int? endMileageMeters;
        private ushort? outsideConnectionId;
        private byte? direction8;
        private string hwCodeShort;
        private string hwCodeLong;
        private string hwDettachedPrefix;
        private string hwIdentifierSuffix;
        private float? distanceFromCenter;


        private void FillHwParams()
        {
            var paramsHw = WriteTheSignsMod.Controller.ConnectorADR.GetHighwayData(NetManager.instance.m_segments.m_buffer[segmentId].m_nameSeed);
            hwDettachedPrefix = paramsHw?.detachedStr ?? "";
            hwIdentifierSuffix = paramsHw?.hwIdentifier ?? "";
            hwCodeShort = paramsHw?.shortCode ?? "";
            hwCodeLong = paramsHw?.longCode ?? "";
            startMileageMeters = SegmentUtils.GetNumberAt(0, segmentId, paramsHw?.mileageSrc ?? SegmentUtils.MileageStartSource.DEFAULT, paramsHw?.mileageOffset ?? 0, out _);
            endMileageMeters = SegmentUtils.GetNumberAt(1, segmentId, paramsHw?.mileageSrc ?? SegmentUtils.MileageStartSource.DEFAULT, paramsHw?.mileageOffset ?? 0, out _);
        }

        public void PurgeCache(CacheErasingFlags cacheToPurge, InstanceID refId)
        {
            if (cacheToPurge.Has(CacheErasingFlags.SegmentNameParam))
            {
                fullStreetName = null;
                streetName = null;
                streetQualifier = null;
                direction8 = null;
                hwCodeShort = null;
                hwCodeLong = null;
                hwDettachedPrefix = null;
                hwIdentifierSuffix = null;
            }

            if (cacheToPurge.Has(CacheErasingFlags.PostalCodeParam))
            {
                postalCode = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.BuildingName | CacheErasingFlags.SegmentSize | CacheErasingFlags.OutsideConnections))
            {
                outsideConnectionId = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.DistrictArea))
            {
                districtId = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.ParkArea))
            {
                parkId = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.SegmentSize))
            {
                startMileageMeters = null;
                endMileageMeters = null;
                distanceFromCenter = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.PostalCodeParam))
            {
                distanceFromCenter = null;
            }
        }

        internal float GetMetersAt(float pos) => Mathf.Lerp(StartMileageMeters, EndMileageMeters, pos);
    }

}
