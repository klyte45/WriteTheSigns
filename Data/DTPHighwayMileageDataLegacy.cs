using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System;

namespace Klyte.DynamicTextProps.Data
{

    public class DTPHighwayMileageDataLegacy : DataExtensorLegacyBase<DTPHighwayMileageData>
    {
        public override string SaveId { get; } = "K45_DTP_MM";

        public override DTPHighwayMileageData Deserialize(byte[] dataByte)
        {
            var result = new DTPHighwayMileageData();
            string data = System.Text.Encoding.UTF8.GetString(dataByte);
            LogUtils.DoLog($"{GetType()} STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return result;
            }
            try
            {
                result.CurrentDescriptor = XmlUtils.DefaultXmlDeserialize<BoardDescriptorMileageMarkerXml>(data);
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"Error deserializing: {e.Message}\n{e.StackTrace}");
            }
            return result;
        }
    }

}