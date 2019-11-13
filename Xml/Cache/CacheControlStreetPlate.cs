using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{
    public class CacheControlStreetPlate : CacheControl
    {
        public Vector3 m_platePosition;
        public float m_streetDirection1;
        public float m_streetDirection2;
        public ushort m_segmentId1;
        public ushort m_segmentId2;
        public byte m_districtId1;
        public byte m_districtId2;
        public float m_distanceRef;
        public bool m_renderPlate;
        public Color m_cachedColor = Color.white;
        public Color m_cachedColor2 = Color.white;
        internal Color m_propColor = Color.white;
        internal uint m_fullNameSubInfoDrawTime2;
        internal uint m_fullNameSubInfoDrawTime;
        internal uint m_nameSubInfoDrawTime2;
        internal uint m_nameSubInfoDrawTime;
        internal Color m_cachedContrastColor;
        internal Color m_cachedContrastColor2;
    }

}
