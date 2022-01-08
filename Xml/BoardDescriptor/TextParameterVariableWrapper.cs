using ColossalFramework;
using Klyte.WriteTheSigns.Singleton;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Klyte.WriteTheSigns.Xml
{
    public class TextParameterVariableWrapper
    {
        public const string PROTOCOL_VARIABLE = "var://";

        internal static CommandLevel OnFilterParamImagesByText(string inputText, out string currentLocaleDesc)
        {
            if ((inputText?.Length ?? 0) >= 4 && inputText.StartsWith(PROTOCOL_VARIABLE))
            {
                var parameterPath = inputText.Substring(PROTOCOL_VARIABLE.Length).Split('/');
                return IterateInCommandTree(out currentLocaleDesc, parameterPath, null, new CommandLevel
                {
                    descriptionKey = "_VarLevelRoot",
                    nextLevelOptions = commandTree,
                    defaultValue = VariableType.Invalid
                }, 0);
            }
            else
            {
                currentLocaleDesc = null;
                return null;
            }
        }

        private static CommandLevel IterateInCommandTree(out string currentLocaleDesc, string[] parameterPath, Enum levelKey, CommandLevel currentLevel, int level)
        {
            if (level < parameterPath.Length - 1)
            {
                if (currentLevel.defaultValue != null)
                {
                    Enum varType = currentLevel.defaultValue;
                    try
                    {
                        varType = (Enum)Enum.Parse(varType.GetType(), parameterPath[level]);
                    }
                    catch
                    {

                    }
                    if (varType != currentLevel.defaultValue && currentLevel.nextLevelOptions.ContainsKey(varType))
                    {
                        return IterateInCommandTree(out currentLocaleDesc, parameterPath, varType, currentLevel.nextLevelOptions[varType], level + 1);
                    }
                }
                else
                {
                    if (!currentLevel.regexValidValues.IsNullOrWhiteSpace())
                    {
                        if (Regex.IsMatch(parameterPath[level], $"^{currentLevel.regexValidValues}$"))
                        {
                            return IterateInCommandTree(out currentLocaleDesc, parameterPath, null, currentLevel.nextLevelByRegex, level + 1);
                        }
                    }
                }
            }
            currentLocaleDesc = !(currentLevel.descriptionKey is null)
                ? currentLevel.descriptionKey 
                : !(levelKey is null)
                    ? CommandLevel.ToLocaleVar(levelKey) 
                    : !(currentLevel.defaultValue is null)
                        ? CommandLevel.ToLocaleVar(currentLevel.defaultValue) 
                        : null;
            currentLevel.level = level;
            return currentLevel;
        }

        internal class CommandLevel
        {
            public Enum defaultValue;
            public Dictionary<Enum, CommandLevel> nextLevelOptions;
            public string regexValidValues;
            public CommandLevel nextLevelByRegex;
            public string descriptionKey;

            public int level;

            public static string ToLocaleVar(Enum e) => $"{e.GetType().Name}.{e.ToString()}";
        }

        private static readonly CommandLevel m_numberFormatFloat = new CommandLevel
        {
            descriptionKey = "COMMON_NUMBERFORMAT_FLOAT"
        };
        private static readonly CommandLevel m_numberFormatInt = new CommandLevel
        {
            descriptionKey = "COMMON_NUMBERFORMAT_INT"
        };


        private static readonly Dictionary<Enum, CommandLevel> commandTree
            = new Dictionary<Enum, CommandLevel>
            {
                [VariableType.SegmentTarget] = new CommandLevel
                {
                    descriptionKey = "SegmentTarget__SelectReference",
                    regexValidValues = "[0-4]",
                    nextLevelByRegex = new CommandLevel
                    {
                        defaultValue = VariableTargetSubType.None,
                        nextLevelOptions = new Dictionary<Enum, CommandLevel>
                        {
                            [VariableTargetSubType.StreetSuffix] = null,
                            [VariableTargetSubType.StreetNameComplete] = null,
                            [VariableTargetSubType.StreetPrefix] = null,
                            [VariableTargetSubType.District] = null,
                            [VariableTargetSubType.Park] = null,
                            [VariableTargetSubType.PostalCode] = null,
                            [VariableTargetSubType.ParkOrDistrict] = null,
                            [VariableTargetSubType.DistrictOrPark] = null,
                            [VariableTargetSubType.MileageMeters] = m_numberFormatFloat,
                            [VariableTargetSubType.MileageKilometers] = m_numberFormatFloat,
                            [VariableTargetSubType.MileageMiles] = m_numberFormatFloat,
                            [VariableTargetSubType.DistrictAreaM2] = m_numberFormatFloat,
                            [VariableTargetSubType.DistrictAreaKm2] = m_numberFormatFloat,
                            [VariableTargetSubType.DistrictAreaMi2] = m_numberFormatFloat,
                            [VariableTargetSubType.ParkAreaM2] = m_numberFormatFloat,
                            [VariableTargetSubType.ParkAreaKm2] = m_numberFormatFloat,
                            [VariableTargetSubType.ParkAreaMi2] = m_numberFormatFloat,
                            [VariableTargetSubType.DistrictPopulation] = m_numberFormatInt,
                        }
                    }
                },
                [VariableType.CityData] = new CommandLevel
                {
                    defaultValue = VariableCitySubType.None,
                    nextLevelOptions = new Dictionary<Enum, CommandLevel>
                    {
                        [VariableCitySubType.CityName] = null,
                        [VariableCitySubType.CityPopulation] = m_numberFormatInt,
                    }
                },
            };
        public readonly string m_originalCommand;

        private enum VariableType
        {
            Invalid,
            SegmentTarget,
            CityData
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
            MileageKilometers,
            MileageMiles,
            DistrictAreaM2,
            DistrictAreaKm2,
            DistrictAreaMi2,
            ParkAreaM2,
            ParkAreaKm2,
            ParkAreaMi2,
            DistrictPopulation,
        }
        private enum VariableCitySubType
        {
            None,
            CityName,
            CityPopulation,
        }

        internal TextParameterVariableWrapper(string input)
        {
            m_originalCommand = input;
            var parameterPath = input.Split('/');
            if (parameterPath.Length > 0)
            {
                VariableType varType = VariableType.Invalid;
                try
                {
                    varType = (VariableType)Enum.Parse(typeof(VariableType), parameterPath[0]);
                }
                catch { }
                switch (varType)
                {
                    case VariableType.SegmentTarget:
                        if (parameterPath.Length >= 3 && byte.TryParse(parameterPath[1], out byte targIdx) && targIdx <= 4)
                        {
                            try
                            {
                                if (Enum.Parse(typeof(VariableTargetSubType), parameterPath[2]) is VariableTargetSubType tt)
                                {
                                    switch (tt)
                                    {
                                        case VariableTargetSubType.MileageMeters:
                                        case VariableTargetSubType.MileageKilometers:
                                        case VariableTargetSubType.MileageMiles:
                                        case VariableTargetSubType.DistrictAreaM2:
                                        case VariableTargetSubType.DistrictAreaKm2:
                                        case VariableTargetSubType.DistrictAreaMi2:
                                        case VariableTargetSubType.ParkAreaM2:
                                        case VariableTargetSubType.ParkAreaKm2:
                                        case VariableTargetSubType.ParkAreaMi2:
                                        case VariableTargetSubType.DistrictPopulation:
                                            if (parameterPath.Length >= 4)
                                            {
                                                numberFormat = parameterPath[3];
                                            }
                                            break;
                                    }
                                    targetSubtype = tt;
                                    index = targIdx;
                                    type = VariableType.SegmentTarget;
                                    break;
                                }
                            }
                            catch { }
                        }
                        break;
                    case VariableType.CityData:
                        if (parameterPath.Length >= 2)
                        {
                            try
                            {
                                if (Enum.Parse(typeof(VariableCitySubType), parameterPath[1]) is VariableCitySubType tt)
                                {
                                    switch (tt)
                                    {
                                        case VariableCitySubType.CityPopulation:
                                            if (parameterPath.Length >= 3)
                                            {
                                                numberFormat = parameterPath[2];
                                            }
                                            break;
                                    }
                                    targetCitySubtype = tt;
                                    type = VariableType.CityData;
                                    break;
                                }
                            }
                            catch { }
                        }
                        break;
                }
            }
        }

        private VariableType type = VariableType.Invalid;
        private byte index = 0;
        private VariableTargetSubType targetSubtype = VariableTargetSubType.None;
        private VariableCitySubType targetCitySubtype = VariableCitySubType.None;
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
                case VariableType.SegmentTarget:
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

                            case VariableTargetSubType.MileageKilometers:
                                multiplier = 0.001f;
                                goto case VariableTargetSubType.MileageMeters;
                            case VariableTargetSubType.MileageMiles:
                                multiplier = 1 / 1609f;
                                goto case VariableTargetSubType.MileageMeters;
                            case VariableTargetSubType.MileageMeters:
                                return TryFormat(WTSCacheSingleton.instance.GetSegment(targId).GetMetersAt(propDescriptor.SegmentPosition), multiplier);

                            case VariableTargetSubType.DistrictAreaKm2:
                                multiplier = 0.000001f;
                                goto case VariableTargetSubType.DistrictAreaM2;
                            case VariableTargetSubType.DistrictAreaMi2:
                                multiplier = 1f / 1609f / 1609f;
                                goto case VariableTargetSubType.DistrictAreaM2;
                            case VariableTargetSubType.DistrictAreaM2:
                                return TryFormat(WTSCacheSingleton.instance.GetDistrict(WTSCacheSingleton.instance.GetSegment(targId).DistrictId).AreaSqMeters, multiplier);

                            case VariableTargetSubType.ParkAreaKm2:
                                multiplier = 0.000001f;
                                goto case VariableTargetSubType.ParkAreaM2;
                            case VariableTargetSubType.ParkAreaMi2:
                                multiplier = 1f / 1609f / 1609f;
                                goto case VariableTargetSubType.ParkAreaM2;
                            case VariableTargetSubType.ParkAreaM2:
                                return TryFormat(WTSCacheSingleton.instance.GetPark(WTSCacheSingleton.instance.GetSegment(targId).ParkId).AreaSqMeters, multiplier);


                            case VariableTargetSubType.DistrictPopulation:
                                return TryFormat(WTSCacheSingleton.instance.GetDistrict(WTSCacheSingleton.instance.GetSegment(targId).DistrictId).Population);

                            default:
                                goto Fallback;
                        }
                    }
                case VariableType.CityData:
                    switch (targetCitySubtype)
                    {
                        case VariableCitySubType.CityName:
                            return WTSCacheSingleton.instance.GetDistrict(0).Name.Value;
                        case VariableCitySubType.CityPopulation:
                            return TryFormat(WTSCacheSingleton.instance.GetDistrict(0).Population);
                        default:
                            goto Fallback;
                    }

            }
        Fallback:
            return m_originalCommand;
        }

        private string TryFormat(float value, float multiplier)
        {
            try
            {
                return (value * multiplier).ToString(numberFormat);
            }
            catch
            {
                numberFormat = "0";
                return (value * multiplier).ToString(numberFormat);
            }
        }
        private string TryFormat(long value)
        {
            try
            {
                return value.ToString(numberFormat);
            }
            catch
            {
                numberFormat = "0";
                return value.ToString(numberFormat);
            }
        }
    }
}
