using ColossalFramework;
using ColossalFramework.Math;
using Klyte.Commons.Utils;
using System;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Utils
{
    internal static class DTBHookable
    {
        public static Func<ushort, string> GetStreetFullName = (ushort idx) => NetManager.instance.GetSegmentName(idx);

        public static Func<ushort, string> GetStreetSuffix = (ushort idx) =>
        {
            string result;
            if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) != 0)
            {
                LogUtils.DoLog($"!UpdateMeshStreetSuffix Custom");
                InstanceID id = default;
                id.NetSegment = idx;
                result = Singleton<InstanceManager>.instance.GetName(id);
            }
            else
            {
                LogUtils.DoLog($"!UpdateMeshStreetSuffix NonCustom {NetManager.instance.m_segments.m_buffer[idx].m_nameSeed}");
                if (NetManager.instance.m_segments.m_buffer[idx].Info.m_netAI is RoadBaseAI ai)
                {
                    Randomizer randomizer = new Randomizer(NetManager.instance.m_segments.m_buffer[idx].m_nameSeed);
                    randomizer.Int32(12);
                    result = ReflectionUtils.RunPrivateMethod<string>(ai, "GenerateStreetName", randomizer);
                    //}
                }
                else
                {
                    result = "???";
                }
            }

            return result;
        };

        private static Vector2? m_cachedPos;
        private static readonly Color[] randomColors = { Color.black, Color.gray, Color.white, Color.red, new Color32(0xFF, 0x88, 0, 0xFf), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };


        public static Func<ushort, Color> GetDistrictColor = (ushort districtId) => randomColors[districtId % randomColors.Length];
        public static Func<Vector2> GetStartPoint = () =>
        {
            if (m_cachedPos == null)
            {
                GameAreaManager.instance.GetStartTile(out int x, out int y);
                m_cachedPos = new Vector2((x - 2) * 1920, (y - 2) * 1920);
            }
            return m_cachedPos.GetValueOrDefault();
        };
    }
}

