using Klyte.WriteTheSigns.Singleton;
using System;

namespace Klyte.WriteTheSigns.Xml
{
    public class TextParameterVariableWrapper
    {
        public readonly string m_originalCommand;

        private enum VariableType
        {
            Invalid,
            Target
        }

        private enum VariableTargetSubType
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
            MileageKiloMeters,
            MileageMiles,
        }

        internal TextParameterVariableWrapper(string input)
        {
            m_originalCommand = input;
            var parameterPath = input.Split('/');
            if (parameterPath.Length > 0)
            {
                switch (parameterPath[0])
                {
                    case "target":
                        if (parameterPath.Length >= 3 && byte.TryParse(parameterPath[1], out byte targIdx) && targIdx <= 4)
                        {
                            if (Enum.Parse(typeof(VariableTargetSubType), parameterPath[2]) is VariableTargetSubType tt)
                            {
                                switch (tt)
                                {
                                    case VariableTargetSubType.MileageMeters:
                                    case VariableTargetSubType.MileageKiloMeters:
                                    case VariableTargetSubType.MileageMiles:
                                        if (parameterPath.Length >= 4)
                                        {
                                            numberFormat = parameterPath[3];
                                        }
                                        break;
                                }
                                targetSubtype = tt;
                                index = targIdx;
                                type = VariableType.Target;
                                break;
                            }
                        }
                        break;
                }
            }
        }

        private VariableType type = VariableType.Invalid;
        private byte index = 0;
        private VariableTargetSubType targetSubtype = VariableTargetSubType.None;
        private string numberFormat = "0";

        public string GetTargetTextForBuilding(BoardInstanceBuildingXml buildingDescriptor, ushort buildingId, BoardTextDescriptorGeneralXml textDescriptor)
        {
            switch (type)
            {

            }
            return m_originalCommand;
        }

        public string GetTargetTextForNet(OnNetInstanceCacheContainerXml propDescriptor, ushort segmentId, BoardTextDescriptorGeneralXml textDescriptor)
        {
            switch (type)
            {
                case VariableType.Target:
                    var targId = index == 0 ? segmentId : propDescriptor.GetTargetSegment(index);
                    if (targId == 0 || targetSubtype == VariableTargetSubType.None)
                    {
                        return $"{targetSubtype}@targ{index}";
                    }
                    else
                    {
                        var multiplier = 1f;
                        switch (targetSubtype)
                        {
                            case VariableTargetSubType.StreetSuffix: return WTSCacheSingleton.instance.GetSegment(targId).StreetName.Value;
                            case VariableTargetSubType.StreetNameComplete: return WTSCacheSingleton.instance.GetSegment(targId).FullStreetName.Value;
                            case VariableTargetSubType.StreetPrefix: return WTSCacheSingleton.instance.GetSegment(targId).StreetQualifier.Value;
                            case VariableTargetSubType.ParkOrDistrict:
                            case VariableTargetSubType.DistrictOrPark:
                                var segmentData = WTSCacheSingleton.instance.GetSegment(targId);
                                if (segmentData.DistrictId == 0 && segmentData.ParkId > 0 && targetSubtype == VariableTargetSubType.ParkOrDistrict)
                                {
                                    goto case VariableTargetSubType.Park;
                                }
                                else
                                {
                                    goto case VariableTargetSubType.District;
                                }
                            case VariableTargetSubType.District:
                                var segmentData2 = WTSCacheSingleton.instance.GetSegment(targId);
                                return segmentData2.OutsideConnectionId != 0
                                    ? WTSCacheSingleton.instance.GetBuilding(segmentData2.OutsideConnectionId).Name.Value
                                    : WTSCacheSingleton.instance.GetDistrict(segmentData2.DistrictId).Name.Value;
                            case VariableTargetSubType.Park: return WTSCacheSingleton.instance.GetPark(WTSCacheSingleton.instance.GetSegment(targId).ParkId).Name.Value;
                            case VariableTargetSubType.PostalCode: return WTSCacheSingleton.instance.GetSegment(targId).PostalCode;

                            case VariableTargetSubType.MileageKiloMeters:
                                multiplier = 0.001f;
                                goto case VariableTargetSubType.MileageMeters;
                            case VariableTargetSubType.MileageMiles:
                                multiplier = 1 / 1609f;
                                goto case VariableTargetSubType.MileageMeters;
                            case VariableTargetSubType.MileageMeters:
                                try
                                {
                                    return (WTSCacheSingleton.instance.GetSegment(targId).GetMetersAt(propDescriptor.SegmentPosition) * multiplier).ToString(numberFormat);
                                }
                                catch
                                {
                                    numberFormat = "0";
                                    return (WTSCacheSingleton.instance.GetSegment(targId).GetMetersAt(propDescriptor.SegmentPosition) * multiplier).ToString(numberFormat);
                                }

                            default:
                                goto Fallback;
                        }
                    }
            }
        Fallback:
            return m_originalCommand;
        }
    }
}
