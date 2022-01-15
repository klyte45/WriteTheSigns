using Klyte.Commons;
using Klyte.WriteTheSigns.UI;
using UnityEngine;

namespace Klyte.WriteTheSigns.Tools
{

    public class SegmentEditorPickerTool : BaseNetTool<SegmentEditorPickerTool>
    {
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {

            if (m_hoverSegment != 0)
            {
                Color toolColor = m_hoverColor;
                RenderOverlayUtils.RenderNetSegmentOverlay(cameraInfo, toolColor, m_hoverSegment);
                return;
            }

        }

        protected override void OnLeftClick()
        {
            if (m_hoverSegment != 0)
            {
                WTSOnNetLiteUI.Instance.CurrentSegmentId = m_hoverSegment;
                ToolsModifierControl.SetTool<DefaultTool>();
            }
        }
        protected override void OnRightClick() =>
                ToolsModifierControl.SetTool<DefaultTool>();

        protected override void OnEnable() => WTSOnNetLiteUI.Instance.CurrentSegmentId = 0;

    }

}
