using ICities;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSOnNetData")]
    public class WTSOnNetData : DataExtensionBase<WTSOnNetData>
    {
        [XmlIgnore]
        public OnNetGroupDescriptorXml[] m_boardsContainers = new OnNetGroupDescriptorXml[NetManager.MAX_SEGMENT_COUNT];
        [XmlElement("BoardContainers")]
        public SimpleNonSequentialList<OnNetGroupDescriptorXml> BoardContainersExport
        {
            get
            {
                var res = new SimpleNonSequentialList<OnNetGroupDescriptorXml>();
                for (int i = 0; i < m_boardsContainers.Length; i++)
                {
                    if (m_boardsContainers[i] != null && m_boardsContainers[i].HasAnyBoard())
                    {
                        res[i] = m_boardsContainers[i];
                    }
                }
                return res;
            }

            set
            {
                LoadDefaults(null);
                foreach (var kv in value.Keys)
                {
                    m_boardsContainers[kv] = value[kv];
                }
            }
        }

        public override string SaveId => "K45_WTS_WTSOnNetData";



        public override void LoadDefaults(ISerializableData serializableData)
        {
            base.LoadDefaults(serializableData);
            m_boardsContainers = new OnNetGroupDescriptorXml[NetManager.MAX_SEGMENT_COUNT];
        }

        [XmlAttribute("defaultFont")]
        public virtual string DefaultFont { get; set; }

        public void OnSegmentChanged(ushort segmentId)
        {
            clearCacheQueue.Add(segmentId);

            if (currentCacheCoroutine is null)
            {
                currentCacheCoroutine = WriteTheSignsMod.Controller?.StartCoroutine(ClearCacheQueue());
            }
        }
        private readonly HashSet<ushort> clearCacheQueue = new HashSet<ushort>();
        private Coroutine currentCacheCoroutine;
        private IEnumerator ClearCacheQueue()
        {
            do
            {
                var list = clearCacheQueue.ToList();
                foreach (var segmentId in list)
                {
                    if (BoardContainersExport.TryGetValue(segmentId, out OnNetGroupDescriptorXml descriptorXml))
                    {
                        foreach (var board in descriptorXml.BoardsData)
                        {
                            board.m_cachedPositions = null;
                            board.m_cachedRotations = null;
                        }
                    }
                    WTSCacheSingleton.ClearCacheSegmentSize(segmentId);
                    WTSCacheSingleton.ClearCacheSegmentNameParam(segmentId);
                    clearCacheQueue.Remove(segmentId);
                    yield return 0;
                }
            } while (clearCacheQueue.Count > 0);
            currentCacheCoroutine = null;
        }
    }

}
