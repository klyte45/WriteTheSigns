using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwaySigns : BoardGeneratorParent<BoardGeneratorHighwaySigns, BoardBunchContainerHighwaySignXml, CacheControlHighwaySign, BasicRenderInformation, BoardDescriptorHigwaySignXml, BoardTextDescriptorHighwaySignsXml>
    {

        public Dictionary<string, UIFont> m_fontCache = new Dictionary<string, UIFont>();
        public BasicRenderInformation[] m_cachedExitTitles;
        private readonly Dictionary<string, BasicRenderInformation> m_textCache = new Dictionary<string, BasicRenderInformation>();
        public List<ushort> m_destroyQueue = new List<ushort>();
        public List<string> LoadedProps { get; private set; }

        private UIDynamicFont m_font;


        public static readonly TextType[] AVAILABLE_TEXT_TYPES = new TextType[]
        {
            TextType.OwnName,
            TextType.Fixed,
            TextType.StreetNameComplete
        };


        public override int ObjArraySize => NetManager.MAX_SEGMENT_COUNT;
        public override UIDynamicFont DrawFont => m_font;

        #region Initialize
        public override void Initialize()
        {
            m_cachedExitTitles = new BasicRenderInformation[50];
            //m_cachedDistanceMeshes = new BasicRenderInformation[100];
            LoadedProps = new List<string>();
            FileUtils.ForEachLoadedPrefab<PropInfo>((loaded) => LoadedProps.Add(loaded.name));
            LoadedProps.Sort((x, y) => GetPropName(x).CompareTo(GetPropName(y)));

            NetManagerOverrides.EventSegmentReleased += OnSegmentReleased;

            BuildSurfaceFont(out m_font, "Highway Gothic");

            #region Hooks
            System.Reflection.MethodInfo afterRenderSegment = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            AddRedirect(typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }), null, afterRenderSegment);
            #endregion
        }

        private static string GetPropName(string x) => x.EndsWith("_Data") ? Locale.Get("PROPS_TITLE", x) : x;

        public void Start()
        {
            if (m_loadedBoards != null)
            {
                m_boardsContainers = m_loadedBoards;
                m_loadedBoards = null;
            }
        }


        protected override void ResetImpl()
        {
            m_fontCache = new Dictionary<string, UIFont>();
            m_textCache.Clear();
        }

        private void OnSegmentReleased(ushort segmentId) => m_boardsContainers[segmentId] = null;
        #endregion

        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {

            Instance.AfterRenderSegmentImpl(cameraInfo, segmentID, layerMask);

        }

        public void AfterRenderSegmentImpl(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {
            if (m_boardsContainers[segmentID] == null)
            {
                return;
            }

            if (!m_boardsContainers[segmentID].cached)
            {
                UpdateCache(segmentID);
                m_boardsContainers[segmentID].cached = true;
            }

            RenderSign(cameraInfo, segmentID, layerMask);

        }

        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {
            for (int i = 0; i < m_boardsContainers[segmentID]?.m_boardsData?.Length; i++)
            {
                CacheControlHighwaySign sign = m_boardsContainers[segmentID]?.m_boardsData[i];
                if (sign?.descriptor == null)
                {
                    continue;
                }

                RenderPropMesh(ref sign.m_cachedProp, cameraInfo, segmentID, i, 0, layerMask, 0, sign.cachedPosition, Vector4.zero, ref sign.descriptor.m_propName, sign.cachedRotation, sign.descriptor.PropScale, ref sign.descriptor, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < sign.descriptor.m_textDescriptors?.Length; j++)
                    {
                        MaterialPropertyBlock block = NetManager.instance.m_materialBlock;
                        block.Clear();
                        RenderTextMesh(cameraInfo, segmentID, i, j, ref sign.descriptor, propMatrix, ref sign.descriptor.m_textDescriptors[j], ref sign, block);
                    }
                }

            }
        }

        private void UpdateCache(ushort segmentID)
        {

            for (int i = 0; i < m_boardsContainers[segmentID]?.m_boardsData?.Length; i++)
            {
                CacheControlHighwaySign sign = m_boardsContainers[segmentID]?.m_boardsData[i];
                if (sign?.descriptor == null)
                {
                    continue;
                }

                sign.m_cachedProp = null;
                bool segmentInverted = (NetManager.instance.m_segments.m_buffer[segmentID].m_flags & NetSegment.Flags.Invert) > 0;
                float effectiveSegmentPos = segmentInverted ? 1 - sign.descriptor.m_segmentPosition : sign.descriptor.m_segmentPosition;
                Vector3 bezierPos = NetManager.instance.m_segments.m_buffer[segmentID].GetBezier().Position(effectiveSegmentPos);

                NetManager.instance.m_segments.m_buffer[segmentID].GetClosestPositionAndDirection(bezierPos, out _, out Vector3 dir);
                float rotation = dir.GetAngleXZ();
                if (sign.descriptor.m_invertSign != segmentInverted)
                {
                    rotation += 180;
                }

                Vector3 rotationVectorX = VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation - 90));
                sign.cachedPosition = bezierPos + (rotationVectorX * (NetManager.instance.m_segments.m_buffer[segmentID].Info.m_halfWidth + sign.descriptor.m_propPosition.x)) + (VectorUtils.X_Y(KlyteMathUtils.DegreeToVector2(rotation)) * sign.descriptor.m_propPosition.z);
                sign.cachedPosition.y += sign.descriptor.m_propPosition.y;
                sign.cachedRotation = sign.descriptor.m_propRotation + new Vector3(0, rotation + 90);
            }
        }


        #region Serialize

        // Token: 0x04000019 RID: 25
        protected override string ID { get; } = "K45_DTP_HS";
        private static BoardBunchContainerHighwaySignXml[] m_loadedBoards;

        public override void Deserialize(string data)
        {
            LogUtils.DoLog($"STR: \"{data}\"");
            if (data.IsNullOrWhiteSpace())
            {
                return;
            }

            IEnumerable<IGrouping<ushort, Tuple<ushort, string>>> parsedData = ParseSerialization(data.Split(SERIALIZATION_ITM_SEPARATOR.ToCharArray()));
            m_loadedBoards = new BoardBunchContainerHighwaySignXml[NetManager.MAX_SEGMENT_COUNT];
            foreach (IGrouping<ushort, Tuple<ushort, string>> item in parsedData)
            {
                LogUtils.DoLog($"item: {item}");
                FillItem(item);
            }
        }

        private static void FillItem(IGrouping<ushort, Tuple<ushort, string>> item)
        {
            if (item.Key == 0)
            {
                return;
            }

            int count = item.Count();
            LogUtils.DoLog($"COUNT: {count}");
            m_loadedBoards[item.Key] = new BoardBunchContainerHighwaySignXml
            {
                m_boardsData = new CacheControlHighwaySign[count]
            };
            int i = 0;
            item.ForEach((x) =>
            {
                m_loadedBoards[item.Key].m_boardsData[i] = new CacheControlHighwaySign();
                m_loadedBoards[item.Key].m_boardsData[i].Deserialize(x.Second);
                i++;
            });
        }

        private static IEnumerable<IGrouping<ushort, Tuple<ushort, string>>> ParseSerialization(string[] data)
        {
            return data.Select(x =>
            {
                string[] dataArray = x.Split(SERIALIZATION_IDX_SEPARATOR.ToCharArray());
                return Tuple.New(ushort.Parse(dataArray[0]), dataArray[1]);

            }).GroupBy(x => x.First);
        }

        public override string Serialize()
        {
            LogUtils.DoLog($"m_boardsContainers: \"{ m_boardsContainers}\"");
            IEnumerable<string> list = m_boardsContainers.SelectMany(SerializeSelectMany) ?? new List<string>();
            LogUtils.DoLog($"list: \"{list?.Count()}\"");
            return string.Join(SERIALIZATION_ITM_SEPARATOR, list?.ToArray());
        }
        private static IEnumerable<string> SerializeSelectMany(BoardBunchContainerHighwaySignXml x, int i) => x?.m_boardsData?.Select(y => y == null ? null : $"{i}{SERIALIZATION_IDX_SEPARATOR}{ y.Serialize()}").Where(y => y != null) ?? new string[0];


        public const string SERIALIZATION_IDX_SEPARATOR = "∂";
        public const string SERIALIZATION_ITM_SEPARATOR = "∫";
        #endregion


        #region Upadate Data

        private BasicRenderInformation m_fixedOwnname = null;
        protected override BasicRenderInformation GetMeshFullStreetName(ushort idx, int boardIdx, int secIdx, ref BoardDescriptorHigwaySignXml descriptor) => GetFromCacheArray(idx, CacheArrayTypes.FullStreetName);
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int boardIdx, int secIdx, ref BoardDescriptorHigwaySignXml descriptor)
        {
            if (m_fixedOwnname == null)
            {
                RefreshTextData(ref m_fixedOwnname, "WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW", null);
            }
            return m_fixedOwnname;
        }

        protected override BasicRenderInformation GetMeshCurrentNumber(ushort id, int boardIdx, int kilometers, ref BoardDescriptorHigwaySignXml descriptor)
        {
            if (m_cachedExitTitles.Length <= kilometers + 1)
            {
                m_cachedExitTitles = new BasicRenderInformation[kilometers + 1];
            }
            if (m_cachedExitTitles[kilometers] == null || lastFontUpdateFrame > m_cachedExitTitles[kilometers].m_frameDrawTime)
            {
                LogUtils.DoLog($"!nameUpdated Node1 {kilometers}");
                RefreshTextData(ref m_cachedExitTitles[kilometers], $"Saída {kilometers}");
            }
            return m_cachedExitTitles[kilometers];

        }

        protected override BasicRenderInformation GetFixedTextMesh(ref BoardTextDescriptorHighwaySignsXml textDescriptor, ushort refID, int boardIdx, int secIdx, ref BoardDescriptorHigwaySignXml descriptor)
        {
            string txt = (textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText) ?? "";
            if (descriptor.m_textDescriptors[secIdx].m_cachedType != TextType.Fixed)
            {
                descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo = null;
                descriptor.m_textDescriptors[secIdx].m_cachedType = TextType.Fixed;
            }
            return GetTextRendered(secIdx, descriptor, txt);
        }

        #endregion

        public override Color? GetColor(ushort segmentId, int idx, int secIdx, BoardDescriptorHigwaySignXml descriptor) => m_boardsContainers[segmentId].m_boardsData[idx].descriptor.m_color;

        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default;
            result.NetNode = nodeId;
            return result;
        }

        public override Color GetContrastColor(ushort refID, int boardIdx, int secIdx, BoardDescriptorHigwaySignXml descriptor) => Color.black;


        private BasicRenderInformation GetTextRendered(int secIdx, BoardDescriptorHigwaySignXml descriptor, string txt)
        {
            if (txt.IsNullOrWhiteSpace())
            {
                return null;
            }
            if (!descriptor.m_textDescriptors[secIdx].m_overrideFont.IsNullOrWhiteSpace())
            {

                if (!m_fontCache.ContainsKey(descriptor.m_textDescriptors[secIdx].m_overrideFont))
                {
                    BuildSurfaceFont(out UIDynamicFont surfaceFont, descriptor.m_textDescriptors[secIdx].m_overrideFont);
                    if (surfaceFont.baseFont == null)
                    {
                        descriptor.m_textDescriptors[secIdx].m_overrideFont = null;

                        return GetCachedText(txt);
                    }
                    else
                    {
                        m_fontCache[descriptor.m_textDescriptors[secIdx].m_overrideFont] = surfaceFont;
                    }
                }

                if (descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo == null)
                {
                    descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo = RefreshTextData(txt, m_fontCache[descriptor.m_textDescriptors[secIdx].m_overrideFont]);
                }
                return descriptor.m_textDescriptors[secIdx].GeneratedFixedTextRenderInfo;
            }
            else
            {

                return GetCachedText(txt);
            }
        }

        private BasicRenderInformation GetCachedText(string txt)
        {
            if (!m_textCache.ContainsKey(txt))
            {
                m_textCache[txt] = RefreshTextData(txt);
            }
            return m_textCache[txt];
        }

    }
}
