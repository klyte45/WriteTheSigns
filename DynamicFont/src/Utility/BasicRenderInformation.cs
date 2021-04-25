using UnityEngine;

namespace SpriteFontPlus.Utility
{
    public class BasicRenderInformation
    {
        public Mesh m_mesh;
        public Vector2 m_sizeMetersUnscaled;
        public long m_materialGeneratedTick;
        public Material m_generatedMaterial;
        public RangeVector m_YAxisOverflows;
        public RangeVector m_fontBaseLimits;
        public string m_refText;

        public override string ToString() => $"BRI [m={m_mesh?.bounds};sz={m_sizeMetersUnscaled}]";

        internal long GetSize() => GetMeshSize();

        private long GetMeshSize()
        {
            unsafe
            {
                return
                    sizeof(Color32) * (m_mesh.colors32?.Length ?? 0)
                    + sizeof(int) * 4
                    + sizeof(Bounds)
                    + sizeof(BoneWeight) * (m_mesh.boneWeights?.Length ?? 0)
                    + sizeof(Matrix4x4) * (m_mesh.bindposes?.Length ?? 0)
                    + sizeof(Vector3) * (m_mesh.vertices?.Length ?? 0)
                    + sizeof(Vector3) * (m_mesh.normals?.Length ?? 0)
                    + sizeof(Vector4) * (m_mesh.tangents?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv2?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv3?.Length ?? 0)
                    + sizeof(Vector2) * (m_mesh.uv4?.Length ?? 0)
                    + sizeof(Color) * (m_mesh.colors?.Length ?? 0)
                    + sizeof(int) * (m_mesh.triangles?.Length ?? 0)
                    + sizeof(bool)
                    ;
            }
        }
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
