using System;
using System.Collections.Generic;

namespace Klyte.WriteTheSigns.Xml
{
    internal enum VariableType
    {
        Invalid,
        SegmentTarget,
        CityData
    }

    internal static class VariableTypeExtension
    {
        public static CommandLevel GetCommandTree(this VariableType var)
        {
            switch (var)
            {
                case VariableType.SegmentTarget:
                    return new CommandLevel
                    {
                        descriptionKey = "SegmentTarget__SelectReference",
                        regexValidValues = "[0-4]",
                        nextLevelByRegex = new CommandLevel
                        {
                            defaultValue = VariableSegmentTargetSubType.None,
                            nextLevelOptions = VariableSegmentTargetSubTypeExtensions.ReadCommandTree()
                        }
                    };
                case VariableType.CityData:
                    return new CommandLevel
                    {
                        defaultValue = VariableCitySubType.None,
                        nextLevelOptions = VariableCitySubTypeExtensions.ReadCommandTree()
                    };
                default:
                    return null;
            }
        }
    }
}
