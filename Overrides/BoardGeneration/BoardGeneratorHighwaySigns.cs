using ColossalFramework.Globalization;
using ColossalFramework.Math;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public class BoardGeneratorHighwaySigns : BoardGeneratorParent<BoardGeneratorHighwaySigns, BoardBunchContainerHighwaySignXml, DTPHighwaySignsData, BoardDescriptorHigwaySignXml, BoardTextDescriptorHighwaySignsXml>
    {
        public List<ushort> m_destroyQueue = new List<ushort>();
        public List<string> LoadedProps { get; private set; }


        public static readonly TextType[] AVAILABLE_TEXT_TYPES = new TextType[]
        {
            TextType.OwnName,
            TextType.Fixed,
            TextType.StreetNameComplete
        };



        #region Initialize
        public override void Initialize()
        {
            LoadedProps = new List<string>();
            FileUtils.ForEachLoadedPrefab<PropInfo>((loaded) => LoadedProps.Add(loaded.name));
            LoadedProps.Sort((x, y) => GetPropName(x).CompareTo(GetPropName(y)));

            NetManagerOverrides.EventSegmentReleased += OnSegmentReleased;


            #region Hooks
            System.Reflection.MethodInfo afterRenderSegment = GetType().GetMethod("AfterRenderSegment", RedirectorUtils.allFlags);
            AddRedirect(typeof(NetSegment).GetMethod("RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int) }), null, afterRenderSegment);
            #endregion
        }

        private static string GetPropName(string x) => x.EndsWith("_Data") ? Locale.Get("PROPS_TITLE", x) : x;

        public void Start()
        {
        }



        private void OnSegmentReleased(ushort segmentId) => Data.BoardsContainers[segmentId] = null;
        #endregion

        public static void AfterRenderSegment(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {

            Instance.AfterRenderSegmentImpl(cameraInfo, segmentID, layerMask);

        }

        public void AfterRenderSegmentImpl(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {
            if (Data.BoardsContainers[segmentID] == null)
            {
                return;
            }

            if (!Data.BoardsContainers[segmentID].cached)
            {
                UpdateCache(segmentID);
                Data.BoardsContainers[segmentID].cached = true;
            }

            RenderSign(cameraInfo, segmentID, layerMask);

        }

        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask)
        {
            for (int i = 0; i < Data.BoardsContainers[segmentID]?.m_boardsData?.Length; i++)
            {
                CacheControlHighwaySign sign = Data.BoardsContainers[segmentID]?.m_boardsData[i];
                if (sign?.descriptor == null)
                {
                    continue;
                }

                RenderPropMesh(ref sign.m_cachedProp, cameraInfo, segmentID, i, 0, layerMask, 0, sign.cachedPosition, Vector4.zero, ref sign.descriptor.m_propName, sign.cachedRotation, sign.descriptor.PropScale, sign.descriptor, out Matrix4x4 propMatrix, out bool rendered);
                if (rendered)
                {
                    for (int j = 0; j < sign.descriptor.m_textDescriptors?.Length; j++)
                    {
                        MaterialPropertyBlock block = NetManager.instance.m_materialBlock;
                        block.Clear();
                        RenderTextMesh(cameraInfo, segmentID, i, j, sign.descriptor, propMatrix, sign.descriptor.m_textDescriptors[j], block);
                    }
                }

            }
        }

        private void UpdateCache(ushort segmentID)
        {

            for (int i = 0; i < Data.BoardsContainers[segmentID]?.m_boardsData?.Length; i++)
            {
                CacheControlHighwaySign sign = Data.BoardsContainers[segmentID]?.m_boardsData[i];
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


        #region Upadate Data

        protected override BasicRenderInformation GetMeshFullStreetName(ushort idx, int boardIdx, int secIdx, BoardDescriptorHigwaySignXml descriptor) => RenderUtils.GetFromCacheArray(idx, RenderUtils.CacheArrayTypes.FullStreetName, DrawFont);
        protected override BasicRenderInformation GetOwnNameMesh(ushort buildingID, int boardIdx, int secIdx, BoardDescriptorHigwaySignXml descriptor) => RenderUtils.GetTextData("WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW", DrawFont);
        protected override BasicRenderInformation GetMeshCurrentNumber(ushort id, int boardIdx, int kilometers, BoardDescriptorHigwaySignXml descriptor) => RenderUtils.GetTextData($"Saída {kilometers}", DrawFont);

        protected override BasicRenderInformation GetFixedTextMesh(BoardTextDescriptorHighwaySignsXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardDescriptorHigwaySignXml descriptor)
        {
            string txt = (textDescriptor.m_isFixedTextLocalized ? Locale.Get(textDescriptor.m_fixedText, textDescriptor.m_fixedTextLocaleKey) : textDescriptor.m_fixedText) ?? "";

            return RenderUtils.GetTextData(txt, DrawFont, descriptor.m_textDescriptors[secIdx].m_overrideFont);
        }

        #endregion

        public override Color? GetColor(ushort segmentId, int idx, int secIdx, BoardDescriptorHigwaySignXml descriptor) => Data.BoardsContainers[segmentId].m_boardsData[idx].descriptor.m_color;

        protected override InstanceID GetPropRenderID(ushort nodeId)
        {
            InstanceID result = default;
            result.NetNode = nodeId;
            return result;
        }

        public override Color GetContrastColor(ushort refID, int boardIdx, int secIdx, BoardDescriptorHigwaySignXml descriptor) => Color.black;
        protected override void ResetImpl() { }
    }
}
