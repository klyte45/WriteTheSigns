using UnityEngine;

namespace SpriteFontPlus.Utility
{
    public class BasicRenderInformation
    {
        public Mesh m_mesh;
        public Vector2 m_sizeMetersUnscaled;
        public long m_materialGeneratedTick;
        public Material m_generatedMaterial;

        public override string ToString() => $"BRI [m={m_mesh?.bounds};sz={m_sizeMetersUnscaled}]";
    }


}
