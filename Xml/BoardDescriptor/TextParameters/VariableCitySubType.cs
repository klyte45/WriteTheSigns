using Klyte.WriteTheSigns.Singleton;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheSigns.Xml
{
    internal enum VariableCitySubType
    {
        None,
        CityName,
        CityPopulation,
    }

    internal static class VariableCitySubTypeExtensions
    {
        public static Dictionary<Enum, CommandLevel> ReadCommandTree()
        {
            Dictionary<Enum, CommandLevel> result = new Dictionary<Enum, CommandLevel>();
            foreach (var value in Enum.GetValues(typeof(VariableCitySubType)).Cast<VariableCitySubType>())
            {
                if (value == 0)
                {
                    continue;
                }

                result[value] = value.GetCommandLevel();
            }
            return result;
        }
        public static CommandLevel GetCommandLevel(this VariableCitySubType var)
        {
            switch (var)
            {
                case VariableCitySubType.CityPopulation:
                    return CommandLevel.m_numberFormatInt;
                case VariableCitySubType.CityName:
                    return CommandLevel.m_appendPrefix;
                default:
                    return null;
            }
        }
        public static bool ReadData(this VariableCitySubType var, string[] relativeParams, ref Enum subtype, ref string numberFormat, ref string stringFormat, ref string prefix, ref string suffix)
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
        public static string GetFormattedString(this VariableCitySubType var, TextParameterVariableWrapper varWrapper)
        {
            switch (var)
            {
                case VariableCitySubType.CityName:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetDistrict(0).Name);
                case VariableCitySubType.CityPopulation:
                    return varWrapper.TryFormat(WTSCacheSingleton.instance.GetDistrict(0).Population);
                default:
                    return null;
            }
        }

    }
}
