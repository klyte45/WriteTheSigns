using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheSigns.Xml
{
    internal enum VariableBuildingSubType
    {
        None,
        OwnName,
        NextStopLine,
        PrevStopLine,
        LastStopLine,
        PlatformNumber
    }

    internal static class VariableBuildingSubTypeExtensions
    {
        public static Dictionary<Enum, CommandLevel> ReadCommandTree()
        {
            Dictionary<Enum, CommandLevel> result = new Dictionary<Enum, CommandLevel>();
            foreach (var value in Enum.GetValues(typeof(VariableBuildingSubType)).Cast<VariableBuildingSubType>())
            {
                if (value == 0)
                {
                    continue;
                }

                result[value] = value.GetCommandLevel();
            }
            return result;
        }
        public static CommandLevel GetCommandLevel(this VariableBuildingSubType var)
        {
            switch (var)
            {
                case VariableBuildingSubType.OwnName:
                case VariableBuildingSubType.NextStopLine:
                case VariableBuildingSubType.PrevStopLine:
                case VariableBuildingSubType.LastStopLine:
                case VariableBuildingSubType.PlatformNumber:
                    return CommandLevel.m_stringFormat;
                default:
                    return null;
            }
        }
        public static bool ReadData(this VariableBuildingSubType var, string[] relativeParams, ref Enum subtype, ref string numberFormat, ref string stringFormat, ref string prefix, ref string suffix)
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
        public static string GetFormattedString(this VariableBuildingSubType var, IEnumerable<int> platforms, ushort buildingId, TextParameterVariableWrapper varWrapper)
        {
            if (buildingId == 0)
            {
                return null;
            }
            switch (var)
            {
                case VariableBuildingSubType.OwnName:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetBuilding(buildingId).Name);
                case VariableBuildingSubType.NextStopLine:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetBuilding(WTSStopUtils.GetTargetStopInfo(platforms, buildingId).FirstOrDefault().NextStopBuildingId).Name);
                case VariableBuildingSubType.PrevStopLine:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetBuilding(WTSStopUtils.GetTargetStopInfo(platforms, buildingId).FirstOrDefault().PrevStopBuildingId).Name);
                case VariableBuildingSubType.LastStopLine:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetBuilding(WTSStopUtils.GetTargetStopInfo(platforms, buildingId).FirstOrDefault().DestinationBuildingId).Name);
                case VariableBuildingSubType.PlatformNumber:
                    return varWrapper.TryFormat(platforms.FirstOrDefault() + 1);
                default:
                    return null;
            }
        }

    }
}
