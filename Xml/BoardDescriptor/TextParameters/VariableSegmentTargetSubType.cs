using Klyte.WriteTheSigns.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheSigns.Xml
{
    internal enum VariableSegmentTargetSubType
    {
        None,
        StreetSuffix,
        StreetNameComplete,
        StreetPrefix,
        District,
        Park,
        PostalCode,
        ParkOrDistrict,
        DistrictOrPark,
        MileageMeters,
        MileageKilometers,
        MileageMiles,
        DistrictAreaM2,
        DistrictAreaKm2,
        DistrictAreaMi2,
        ParkAreaM2,
        ParkAreaKm2,
        ParkAreaMi2,
        DistrictPopulation,
        Direction,
        HwCodeShort,
        HwCodeLong,
        HwDettachedPrefix,
        HwIdentifierSuffix,
        DistanceFromReferenceKilometers,
        DistanceFromReferenceMeters,
        DistanceFromReferenceMiles,
    }

    internal static class VariableSegmentTargetSubTypeExtensions
    {
        public static Dictionary<Enum, CommandLevel> ReadCommandTree()
        {
            Dictionary<Enum, CommandLevel> result = new Dictionary<Enum, CommandLevel>();
            foreach (var value in Enum.GetValues(typeof(VariableSegmentTargetSubType)).Cast<VariableSegmentTargetSubType>())
            {
                if (value == 0)
                {
                    continue;
                }

                result[value] = value.GetCommandLevel();
            }
            return result;
        }
        public static CommandLevel GetCommandLevel(this VariableSegmentTargetSubType var)
        {
            switch (var)
            {
                case VariableSegmentTargetSubType.MileageMeters:
                case VariableSegmentTargetSubType.MileageKilometers:
                case VariableSegmentTargetSubType.MileageMiles:
                case VariableSegmentTargetSubType.DistrictAreaM2:
                case VariableSegmentTargetSubType.DistrictAreaKm2:
                case VariableSegmentTargetSubType.DistrictAreaMi2:
                case VariableSegmentTargetSubType.ParkAreaM2:
                case VariableSegmentTargetSubType.ParkAreaKm2:
                case VariableSegmentTargetSubType.ParkAreaMi2:
                case VariableSegmentTargetSubType.DistanceFromReferenceKilometers:
                case VariableSegmentTargetSubType.DistanceFromReferenceMeters:
                case VariableSegmentTargetSubType.DistanceFromReferenceMiles:
                    return CommandLevel.m_numberFormatFloat;
                case VariableSegmentTargetSubType.DistrictPopulation:
                    return CommandLevel.m_numberFormatInt;
                case VariableSegmentTargetSubType.StreetSuffix:
                case VariableSegmentTargetSubType.StreetNameComplete:
                case VariableSegmentTargetSubType.StreetPrefix:
                case VariableSegmentTargetSubType.District:
                case VariableSegmentTargetSubType.Park:
                case VariableSegmentTargetSubType.ParkOrDistrict:
                case VariableSegmentTargetSubType.DistrictOrPark:
                    return CommandLevel.m_stringFormat;
                case VariableSegmentTargetSubType.PostalCode:
                case VariableSegmentTargetSubType.Direction:
                case VariableSegmentTargetSubType.HwCodeShort:
                case VariableSegmentTargetSubType.HwCodeLong:
                case VariableSegmentTargetSubType.HwDettachedPrefix:
                case VariableSegmentTargetSubType.HwIdentifierSuffix:
                    return CommandLevel.m_appendPrefix;
                default:
                    return null;
            }
        }

        public static bool ReadData(this VariableSegmentTargetSubType var, string[] relativeParams, ref Enum subtype, ref string numberFormat, ref string stringFormat, ref string prefix, ref string suffix)
        {
            var cmdLevel = var.GetCommandLevel();
            if (cmdLevel is null)
            {
                return false;
            }

            cmdLevel.ParseFormatting(relativeParams, ref numberFormat, ref stringFormat, ref prefix, ref suffix);
            subtype = var;
            return true;
        }

        public static string GetFormattedString(this VariableSegmentTargetSubType var, OnNetInstanceCacheContainerXml propDescriptor, ushort targId, TextParameterVariableWrapper varWrapper)
        {
            var multiplier = 1f;
            switch (var)
            {
                case VariableSegmentTargetSubType.StreetSuffix: return varWrapper.TryFormat(WTSCacheSingleton.instance.GetSegment(targId).StreetName);
                case VariableSegmentTargetSubType.StreetNameComplete: return varWrapper.TryFormat(WTSCacheSingleton.instance.GetSegment(targId).FullStreetName);
                case VariableSegmentTargetSubType.StreetPrefix: return varWrapper.TryFormat(WTSCacheSingleton.instance.GetSegment(targId).StreetQualifier);
                case VariableSegmentTargetSubType.ParkOrDistrict:
                case VariableSegmentTargetSubType.DistrictOrPark:
                    var segmentData = WTSCacheSingleton.instance.GetSegment(targId);
                    if (segmentData.DistrictId == 0 && segmentData.ParkId > 0 && var == VariableSegmentTargetSubType.ParkOrDistrict)
                    {
                        goto case VariableSegmentTargetSubType.Park;
                    }
                    else
                    {
                        goto case VariableSegmentTargetSubType.District;
                    }
                case VariableSegmentTargetSubType.District:
                    var segmentData2 = WTSCacheSingleton.instance.GetSegment(targId);
                    return varWrapper.TryFormat(segmentData2.OutsideConnectionId != 0
                        ? WTSCacheSingleton.instance.GetBuilding(segmentData2.OutsideConnectionId).Name
                        : WTSCacheSingleton.instance.GetDistrict(segmentData2.DistrictId).Name);
                case VariableSegmentTargetSubType.Park: return varWrapper.TryFormat(WTSCacheSingleton.instance.GetPark(WTSCacheSingleton.instance.GetSegment(targId).ParkId).Name);
                case VariableSegmentTargetSubType.PostalCode: return WTSCacheSingleton.instance.GetSegment(targId).PostalCode;

                case VariableSegmentTargetSubType.MileageKilometers:
                    multiplier = 0.001f;
                    goto case VariableSegmentTargetSubType.MileageMeters;
                case VariableSegmentTargetSubType.MileageMiles:
                    multiplier = 1 / 1609f;
                    goto case VariableSegmentTargetSubType.MileageMeters;
                case VariableSegmentTargetSubType.MileageMeters:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetSegment(targId).GetMetersAt(propDescriptor.SegmentPosition), multiplier);

                case VariableSegmentTargetSubType.DistanceFromReferenceKilometers:
                    multiplier = 0.001f;
                    goto case VariableSegmentTargetSubType.DistanceFromReferenceMeters;
                case VariableSegmentTargetSubType.DistanceFromReferenceMiles:
                    multiplier = 1 / 1609f;
                    goto case VariableSegmentTargetSubType.DistanceFromReferenceMeters;
                case VariableSegmentTargetSubType.DistanceFromReferenceMeters:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetSegment(targId).DistanceFromCenter, multiplier);

                case VariableSegmentTargetSubType.DistrictAreaKm2:
                    multiplier = 0.000001f;
                    goto case VariableSegmentTargetSubType.DistrictAreaM2;
                case VariableSegmentTargetSubType.DistrictAreaMi2:
                    multiplier = 1f / 1609f / 1609f;
                    goto case VariableSegmentTargetSubType.DistrictAreaM2;
                case VariableSegmentTargetSubType.DistrictAreaM2:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetDistrict(WTSCacheSingleton.instance.GetSegment(targId).DistrictId).AreaSqMeters, multiplier);

                case VariableSegmentTargetSubType.ParkAreaKm2:
                    multiplier = 0.000001f;
                    goto case VariableSegmentTargetSubType.ParkAreaM2;
                case VariableSegmentTargetSubType.ParkAreaMi2:
                    multiplier = 1f / 1609f / 1609f;
                    goto case VariableSegmentTargetSubType.ParkAreaM2;
                case VariableSegmentTargetSubType.ParkAreaM2:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetPark(WTSCacheSingleton.instance.GetSegment(targId).ParkId).AreaSqMeters, multiplier);

                case VariableSegmentTargetSubType.DistrictPopulation:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetDistrict(WTSCacheSingleton.instance.GetSegment(targId).DistrictId).Population);


                case VariableSegmentTargetSubType.Direction: return WTSCacheSingleton.instance.GetSegment(targId).Direction;
                case VariableSegmentTargetSubType.HwCodeShort: return WTSCacheSingleton.instance.GetSegment(targId).HwCodeShort;
                case VariableSegmentTargetSubType.HwCodeLong: return WTSCacheSingleton.instance.GetSegment(targId).HwCodeLong;
                case VariableSegmentTargetSubType.HwDettachedPrefix: return WTSCacheSingleton.instance.GetSegment(targId).HwDettachedPrefix;
                case VariableSegmentTargetSubType.HwIdentifierSuffix: return WTSCacheSingleton.instance.GetSegment(targId).HwIdentifierSuffix;

                default:
                    return null;
            }
        }
    }
}
