using Klyte.WriteTheSigns.Rendering;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Klyte.WriteTheSigns.Xml
{
    public class TextParameterVariableWrapper
    {
        public readonly string m_originalCommand;

        internal TextParameterVariableWrapper(string input)
        {
            m_originalCommand = input;
            var parameterPath = CommandLevel.GetParameterPath(input);
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
                                if (Enum.Parse(typeof(VariableSegmentTargetSubType), parameterPath[2]) is VariableSegmentTargetSubType tt
                                    && tt.ReadData(parameterPath.Skip(3).ToArray(), ref subtype, ref numberFormat, ref stringFormat, ref prefix, ref suffix))
                                {
                                    index = targIdx;
                                    type = VariableType.SegmentTarget;
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
                                    subtype = tt;
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
        private Enum subtype = VariableSegmentTargetSubType.None;
        private string numberFormat = "0";
        private string stringFormat = "";
        private string prefix = "";
        private string suffix = "";

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

                    if (targId == 0 || !(subtype is VariableSegmentTargetSubType targetSubtype) || targetSubtype == VariableSegmentTargetSubType.None)
                    {
                        return $"{prefix}{subtype}@targ{index}{suffix}";
                    }
                    else
                    {
                        return $"{prefix}{targetSubtype.GetFormattedString(propDescriptor, targId, this) ?? m_originalCommand}{suffix}";
                    }
                case VariableType.CityData:
                    if ((subtype is VariableCitySubType targetCitySubtype))
                    {
                        return $"{prefix}{targetCitySubtype.GetFormattedString(this) ?? m_originalCommand}{suffix}";
                    }
                    break;
            }
            return m_originalCommand;
        }

        internal string TryFormat(float value, float multiplier)
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
        internal string TryFormat(long value)
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
        internal string TryFormat(FormatableString value) => value.GetFormatted(stringFormat);
    }
}
