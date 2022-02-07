using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSBuildingsData")]
    public class WTSBuildingsData : WTSBaseData<WTSBuildingsData, BoardBunchContainerBuilding[]>
    {
        public override int ObjArraySize => BuildingManager.MAX_BUILDING_COUNT;

        public override string SaveId => "K45_WTS_WTSBuildingsData";

        public override int BoardCount => 1;

        public override int SubBoardCount => 1;

        [XmlElement]
        public SimpleXmlDictionary<string, BuildingGroupDescriptorXml> CityDescriptors = new SimpleXmlDictionary<string, BuildingGroupDescriptorXml>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> GlobalDescriptors = new SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml>();
        [XmlIgnore]
        public SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml> AssetsDescriptors = new SimpleXmlDictionary<string, ExportableBuildingGroupDescriptorXml>();

        public void CleanCache() => ResetBoards();
        internal void OnBuildingChangedPosition(ushort buildingId)
        {
            cooldown = 10;
            clearCacheQueue.Add(buildingId);

            if (currentCacheCoroutine is null)
            {
                currentCacheCoroutine = WriteTheSignsMod.Controller?.StartCoroutine(ClearCacheQueue());
            }
        }
        private int cooldown = 0;
        private readonly HashSet<ushort> clearCacheQueue = new HashSet<ushort>();
        private Coroutine currentCacheCoroutine;
        private IEnumerator ClearCacheQueue()
        {
            yield return 0;
            do
            {

                var list = clearCacheQueue.ToList();
                foreach (var buildingId in list)
                {
                    if (--cooldown > 0)
                    {
                        yield return 0;
                    }
                    if (BoardsContainers[buildingId, 0, 0] is BoardBunchContainerBuilding[] bbcb)
                    {
                        foreach (var item in bbcb)
                        {
                            item.ClearCache();
                        }
                    }
                    WTSBuildingDataCaches.PurgeBuildingCache(buildingId);
                    clearCacheQueue.Remove(buildingId);
                    yield return 0;
                }
            } while (clearCacheQueue.Count > 0);
            currentCacheCoroutine = null;
        }
    }

}
