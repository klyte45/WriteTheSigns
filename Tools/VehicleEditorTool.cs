using Klyte.Commons;
using System;
using UnityEngine;

namespace Klyte.WriteTheSigns.Tools
{

    public class VehicleEditorTool : BaseVehicleTool<VehicleEditorTool>
    {
        public event Action<ushort> OnVehicleSelect;
        public event Action<ushort> OnParkedVehicleSelect;

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (m_hoverVehicle != 0)
            {
                m_trailersAlso = false;
                Color toolColor = m_hoverColor;
                RenderOverlay(cameraInfo, toolColor, m_hoverVehicle, false);
                return;
            }
            if (m_hoverParkedVehicle != 0)
            {
                m_trailersAlso = false;
                Color toolColor = m_hoverColor;
                RenderOverlay(cameraInfo, toolColor, m_hoverParkedVehicle, true);
                return;
            }

        }

        protected override void OnLeftClick()
        {
            if (m_hoverVehicle != 0 && !(OnVehicleSelect is null))
            {
                OnVehicleSelect.Invoke(m_hoverVehicle);
                ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.toolController.GetComponent<DefaultTool>();
            }
            else if (m_hoverParkedVehicle != 0 && !(OnParkedVehicleSelect is null))
            {
                OnParkedVehicleSelect.Invoke(m_hoverParkedVehicle);
                ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.toolController.GetComponent<DefaultTool>();
            }
        }

        protected override void OnDisable()
        {
            OnVehicleSelect = null;
            base.OnDisable();
        }

    }

}
