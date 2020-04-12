using ColossalFramework;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System;

namespace Klyte.DynamicTextProps.Data
{

    public class DTPBuildingsDataLegacy : DataExtensorLegacyBase<DTPBuildingsData>
    {
        public override string SaveId => "K45_DTP_BGB";

        public override DTPBuildingsData Deserialize(byte[] dataByte)
        {
            var result = new DTPBuildingsData();
            string data = System.Text.Encoding.UTF8.GetString(dataByte);
            LogUtils.DoLog($"{GetType()} STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return result;
            }
            try
            {
                result.GlobalConfiguration = XmlUtils.DefaultXmlDeserialize<BoardGeneratorBuildingConfigXml>(data);
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"Error deserializing: {e.Message}\n{e.StackTrace}");
            }
            return result;
        }

    }
}