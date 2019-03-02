using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Overrides;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Managers
{
    public class BoardManager : Singleton<BoardManager>
    {
        public int[] m_submeshIndexes;
        public SubBuildingControl[] m_subBuildingObjs;
        public bool[] m_updatedIdsName;


        public static float textScale = 1f;
        public static float pixelRatio = 10f;
        public static Vector2 maxSize = new Vector2(2048f, 128f);

        protected void Awake()
        {
            m_submeshIndexes = new int[BuildingManager.MAX_BUILDING_COUNT];
            m_subBuildingObjs = new SubBuildingControl[BuildingManager.MAX_BUILDING_COUNT];
            m_updatedIdsName = new bool[BuildingManager.MAX_BUILDING_COUNT];
            TransportManagerOverrides.eventOnLineUpdated += onLineUpdated;
            TransportManager.instance.eventLineColorChanged += (x) => onLineUpdated();
            BuildingManagerOverrides.eventOnBuildingRenamed += onBuildingNameChanged;
        }
        protected void Reset()
        {
            m_subBuildingObjs = new SubBuildingControl[BuildingManager.MAX_BUILDING_COUNT];
            m_updatedIdsName = new bool[BuildingManager.MAX_BUILDING_COUNT];
        }

        private void onLineUpdated()
        {
            m_subBuildingObjs = new SubBuildingControl[BuildingManager.MAX_BUILDING_COUNT];
        }
        private void onBuildingNameChanged(ushort id)
        {
            m_updatedIdsName[id] = false;
            //var nextSubBuilding = BuildingManager.instance.m_buildings.m_buffer[id].m_subBuilding;
            //var count = 0;
            //while (nextSubBuilding != 0)
            //{
            //    m_updatedIdsName.Remove(nextSubBuilding);
            //    nextSubBuilding = BuildingManager.instance.m_buildings.m_buffer[nextSubBuilding].m_subBuilding;
            //    count++;
            //    if (count > 40000)
            //    {
            //        KCAIUtils.doErrorLog("INVALID LIST FOUND!");
            //        return;
            //    }
            //}
        }

        public class SubBuildingControl
        {
            public Color m_cachedColor = Color.white;
            public Color m_cachedContrastColor = Color.black;
        }
    }
}
