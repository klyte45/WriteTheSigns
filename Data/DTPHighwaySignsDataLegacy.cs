using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextProps.Data
{
    public class DTPHighwaySignsDataLegacy : DataExtensorLegacyBase<DTPHighwaySignsData>
    {
        public override string SaveId { get; } = "K45_DTP_HS";

        public override DTPHighwaySignsData Deserialize(byte[] dataByte)
        {
            string data = System.Text.Encoding.UTF8.GetString(dataByte);
            LogUtils.DoLog($"STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return null;
            }

            var result = new DTPHighwaySignsData();

            IEnumerable<IGrouping<ushort, Tuple<ushort, string>>> parsedData = ParseSerialization(data.Split(SERIALIZATION_ITM_SEPARATOR.ToCharArray()));
            result.ResetBoards();
            foreach (IGrouping<ushort, Tuple<ushort, string>> item in parsedData)
            {
                LogUtils.DoLog($"item: {item}");
                FillItem(result, item);
            }

            return result;
        }

        private static IEnumerable<IGrouping<ushort, Tuple<ushort, string>>> ParseSerialization(string[] data)
        {
            return data.Select(x =>
            {
                string[] dataArray = x.Split(SERIALIZATION_IDX_SEPARATOR.ToCharArray());
                return Tuple.New(ushort.Parse(dataArray[0]), dataArray[1]);

            }).GroupBy(x => x.First);
        }

        private static void FillItem(DTPHighwaySignsData result, IGrouping<ushort, Tuple<ushort, string>> item)
        {
            if (item.Key == 0)
            {
                return;
            }

            int count = item.Count();
            LogUtils.DoLog($"COUNT: {count}");
            result.BoardsContainers[item.Key] = new BoardBunchContainerHighwaySignXml
            {
                m_boardsData = new CacheControlHighwaySign[count]
            };
            int i = 0;
            item.ForEach((x) =>
            {
                result.BoardsContainers[item.Key].m_boardsData[i] = new CacheControlHighwaySign();
                result.BoardsContainers[item.Key].m_boardsData[i].Deserialize(x.Second);
                i++;
            });
        }

        public const string SERIALIZATION_IDX_SEPARATOR = "∂";
        public const string SERIALIZATION_ITM_SEPARATOR = "∫";
    }

}
