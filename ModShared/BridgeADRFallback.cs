using ColossalFramework.Math;
using Klyte.Commons.Utils;
using System.Collections;
using UnityEngine;

namespace Klyte.WriteTheSigns.ModShared
{
    internal class BridgeADRFallback : IBridgeADR
    {

        public override string GetStreetSuffix(ushort idx)
        {
            string result;
            LogUtils.DoLog($"!UpdateMeshStreetSuffix NonCustom {NetManager.instance.m_segments.m_buffer[idx].m_nameSeed}");
            if (NetManager.instance.m_segments.m_buffer[idx].Info.m_netAI is RoadBaseAI ai)
            {
                var randomizer = new Randomizer(NetManager.instance.m_segments.m_buffer[idx].m_nameSeed);
                randomizer.Int32(12);
                result = ReflectionUtils.RunPrivateMethod<string>(ai, "GenerateStreetName", randomizer);
            }
            else
            {
                result = "???";
            }

            return result;
        }

        private Vector2? m_cachedPos;
        private readonly Color[] m_randomColors = { Color.black, Color.gray, Color.white, Color.red, new Color32(0xFF, 0x88, 0, 0xFf), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };

        public override bool AddressesAvailable { get; } = false;

        public override bool GetAddressStreetAndNumber(Vector3 sidewalk, Vector3 midPosBuilding, out int number, out string streetName) => SegmentUtils.GetBasicAddressStreetAndNumber(sidewalk, midPosBuilding, out number, out streetName);
        public override Color GetDistrictColor(ushort districtId) => m_randomColors[districtId % m_randomColors.Length];
        public override Vector2 GetStartPoint()
        {
            if (m_cachedPos == null)
            {
                GameAreaManager.instance.GetStartTile(out int x, out int y);
                m_cachedPos = new Vector2((x - 2) * 1920, (y - 2) * 1920);
            }
            return m_cachedPos.GetValueOrDefault();
        }

        public override string GetStreetQualifier(ushort idx) => WriteTheSignsMod.Controller.ConnectorADR.GetStreetFullName(idx).Replace(WriteTheSignsMod.Controller.ConnectorADR.GetStreetSuffix(idx), "");
        public override string GetStreetPostalCode(Vector3 position, ushort idx) => idx.ToString("D5");
        public override AdrHighwayParameters GetHighwayData(ushort seedId) => null;
        public override IEnumerator ListAllAvailableHighwayTypes(string filterText, Wrapper<string[]> result) => null;
        public override AdrHighwayParameters GetHighwayTypeData(string typeName) => null;
        internal override byte GetDirection(ushort segmentId) => SegmentUtils.GetCardinalDirectionSegment(segmentId, SegmentUtils.MileageStartSource.DEFAULT);
    }
}

