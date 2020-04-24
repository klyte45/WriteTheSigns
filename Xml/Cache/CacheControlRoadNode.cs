﻿using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public class CacheRoadNodeItem
    {
        public Vector3 m_platePosition;
        public float m_streetDirection;
        public ushort m_segmentId;
        public byte m_districtId;
        public byte m_districtParkId;
        public float m_distanceRef;
        public bool m_renderPlate;
        public Color m_cachedColor = Color.white;
        internal Color m_propColor = Color.white;
        internal Color m_cachedContrastColor;
        public BoardInstanceRoadNodeXml m_currentDescriptor;
        public PropInfo m_cachedProp;
    }

}
