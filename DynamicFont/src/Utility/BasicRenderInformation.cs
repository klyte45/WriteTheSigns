using UnityEngine;

namespace SpriteFontPlus.Utility
{
    public class BasicRenderInformation
    {
        public Mesh m_mesh;
        public Vector2 m_sizeMetersUnscaled;
        public long m_materialGeneratedTick;
        public Material m_generatedMaterial;
        public Material m_generatedMaterialDayNight;
        public Material m_generatedMaterialBright;
        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;

        public override string ToString() => $"BRI [m={m_mesh?.bounds};sz={m_sizeMetersUnscaled}]";
    }

    public struct RangeVector
    {
        public float min;
        public float max;

        public float Offset => max - min;
        public float Center => max + min / 2;

        public override string ToString() => $"[min = {min}, max = {max}]";


    }
}
