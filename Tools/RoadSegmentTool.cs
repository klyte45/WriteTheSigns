using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons;
using System;
using System.Diagnostics;
using UnityEngine;

namespace Klyte.DynamicTextProps.Tools
{

    public class RoadSegmentTool : BasicNetTool<RoadSegmentTool>
    {
        public event Action<ushort> OnSelectSegment;

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {

            if (m_hoverSegment != 0)
            {
                Color toolColor = m_hoverColor;
                RenderOverlay(cameraInfo, toolColor,  m_hoverSegment);
                return;
            }

        }

        protected override void OnLeftClick()
        {
            if (m_hoverSegment != 0)
            {
                OnSelectSegment?.Invoke(m_hoverSegment);
                GetComponent<DefaultTool>().enabled = true;
            }
        }

        protected override void OnDisable()
        {
            OnSelectSegment = null;
            base.OnDisable();
        }



    }

}
