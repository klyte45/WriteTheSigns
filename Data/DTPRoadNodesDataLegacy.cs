using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System;

namespace Klyte.DynamicTextProps.Data
{
    public class DTPRoadNodesDataLegacy : DataExtensorLegacyBase<DTPRoadNodesData>
    {
        public override string SaveId { get; } = "K45_DTP_SS";

        public override DTPRoadNodesData Deserialize(byte[] dataByte)
        {
            var result = new DTPRoadNodesData();
            string data = System.Text.Encoding.UTF8.GetString(dataByte);
            LogUtils.DoLog($"{GetType()} STR: \"{data}\"");
            if (!data.IsNullOrWhiteSpace())
            {
                try
                {
                    result.CurrentDescriptor = XmlUtils.DefaultXmlDeserialize<BoardDescriptorStreetSignXml>(data);
                }
                catch (Exception e)
                {
                    LogUtils.DoErrorLog($"Error deserializing: {e.Message}\n{e.StackTrace}");
                }
            }
            return result;
        }
    }
}