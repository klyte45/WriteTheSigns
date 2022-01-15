using Klyte.WriteTheSigns.Rendering;

namespace Klyte.WriteTheSigns.Xml
{
    internal enum VariableType
    {
        Invalid,
        SegmentTarget,
        CityData,
        CurrentBuilding,
        CurrentVehicle,
        CurrentSegment
    }

    internal static class VariableTypeExtension
    {
        public static bool Supports(this VariableType var, TextRenderingClass? renderingClass)
        {
            if (renderingClass is null)
            {
                return true;
            }
            switch (var)
            {
                case VariableType.CurrentBuilding:
                    return renderingClass == TextRenderingClass.Buildings;
                case VariableType.SegmentTarget:
                    return renderingClass == TextRenderingClass.PlaceOnNet;
                case VariableType.CurrentVehicle:
                    return renderingClass == TextRenderingClass.Vehicle;
                case VariableType.CurrentSegment:
                    return renderingClass == TextRenderingClass.RoadNodes || renderingClass == TextRenderingClass.PlaceOnNet;
                default:
                    return true;
            }
        }
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
                case VariableType.CurrentBuilding:
                    return new CommandLevel
                    {
                        defaultValue = VariableBuildingSubType.None,
                        nextLevelOptions = VariableBuildingSubTypeExtensions.ReadCommandTree()
                    };
                case VariableType.CurrentSegment:
                    return new CommandLevel
                    {
                        defaultValue = VariableSegmentTargetSubType.None,
                        nextLevelOptions = VariableSegmentTargetSubTypeExtensions.ReadCommandTree()
                    };
                case VariableType.CurrentVehicle:
                    return new CommandLevel
                    {
                        defaultValue = VariableVehicleSubType.None,
                        nextLevelOptions = VariableVehicleSubTypeExtensions.ReadCommandTree()
                    };
                default:
                    return null;
            }
        }
    }
}
