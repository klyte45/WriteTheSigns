using ColossalFramework.UI;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Xml;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPEditorPropPreview : UICustomControl
    {

        public UIPanel MainContainer { get; protected set; }

        private DTPPropPreviewRenderer m_previewRenderer;
        private UIPanel m_previewPanel;
        private UITextureSprite m_preview;
        private UIPanel m_previewControls;
        private UIButton m_lockToSelection;
        private float m_targetRotation = 0;
        private Vector2 m_targetCameraPosition = default;
        private Vector2 m_cameraPosition = default;
        private bool m_viewLocked;


        private PropInfo CurrentInfo => DTPPropTextLayoutEditor.Instance.CurrentInfo;
        private Color32 CurrentSelectedColor => DTPPropTextLayoutEditor.Instance.CurrentSelectedColor;
        private int TabToPreview => DTPPropTextLayoutEditor.Instance.CurrentTab;
        private BoardDescriptorGeneralXml EditingInstance => DTPPropTextLayoutEditor.Instance.EditingInstance;

        public float TargetZoom { get; set; } = 3;

        private float m_maxZoom = 2f;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            MainContainer.padding = new RectOffset(8, 8, 8, 8);

            KlyteMonoUtils.CreateUIElement(out m_previewPanel, MainContainer.transform, "previewPanel", new UnityEngine.Vector4(0, 0, MainContainer.width - 66, 300));
            m_previewPanel.backgroundSprite = "GenericPanel";
            m_previewPanel.autoLayout = true;

            KlyteMonoUtils.CreateUIElement(out m_preview, m_previewPanel.transform, "preview", new UnityEngine.Vector4(0, 0, m_previewPanel.width, m_previewPanel.height));
            KlyteMonoUtils.CreateElement(out m_previewRenderer, MainContainer.transform);
            m_previewRenderer.Size = m_preview.size * 2f;
            m_preview.texture = m_previewRenderer.Texture;
            m_preview.eventMouseWheel += ChangeViewZoom;
            m_preview.eventMouseMove += OnMouseMove;
            m_previewRenderer.Zoom = TargetZoom;

            KlyteMonoUtils.CreateUIElement(out m_previewControls, MainContainer.transform, "controls", new UnityEngine.Vector4(0, 0, 50, 300));
            m_previewControls.padding = new RectOffset(5, 5, 5, 5);
            m_previewControls.autoLayout = true;
            m_previewControls.autoLayoutDirection = LayoutDirection.Vertical;


            KlyteMonoUtils.InitCircledButton(m_previewControls, out m_lockToSelection, CommonsSpriteNames.K45_Unlock, (x, y) => ToggleLock(), "K45_DTP_LOCK_UNLOCK_TO_CURRENT_ITEM");
            m_lockToSelection.focusedBgSprite = null;
            m_viewLocked = true;
            ToggleLock();

            KlyteMonoUtils.InitCircledButton(m_previewControls, out UIButton resetView, CommonsSpriteNames.K45_Reload, (x, y) => ResetCamera(), "K45_DTP_RESET_VIEW");
        }

        private void ToggleLock()
        {
            m_viewLocked = !m_viewLocked;
            if (m_viewLocked)
            {
                m_lockToSelection.normalFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Lock);
                m_lockToSelection.hoveredFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Unlock);
                m_lockToSelection.color = Color.red;
                m_lockToSelection.focusedColor = Color.red;
                m_lockToSelection.textColor = Color.yellow;
                m_lockToSelection.hoveredColor = Color.green;
                ResetCamera();
            }
            else
            {
                m_lockToSelection.normalFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Unlock);
                m_lockToSelection.hoveredFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(CommonsSpriteNames.K45_Lock);
                m_lockToSelection.color = Color.white;
                m_lockToSelection.focusedColor = Color.white;
                m_lockToSelection.textColor = Color.white;
                m_lockToSelection.hoveredColor = Color.red;
            }
        }

        internal void ResetCamera()
        {
            if (CurrentInfo != null)
            {
                m_maxZoom = Mathf.Max(Mathf.Pow(CurrentInfo.m_mesh.bounds.extents.y / CurrentInfo.m_mesh.bounds.extents.x, 1 / 3f) * 3f, 3f);
                TargetZoom = m_maxZoom;
                m_targetRotation = 0;
                Vector3 target = CurrentInfo.m_mesh.bounds.center;
                target.y *= -1;
                m_targetCameraPosition = target;
            }
        }
        private void OnMouseMove(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (CurrentInfo != default && !m_viewLocked)
            {
                if ((eventParam.buttons & UIMouseButton.Left) != 0)
                {
                    Vector3 min = CurrentInfo.m_mesh.bounds.min;
                    Vector3 max = CurrentInfo.m_mesh.bounds.max;
                    min.y = -max.y;
                    max.y = -CurrentInfo.m_mesh.bounds.min.y;
                    m_targetCameraPosition = Vector2.Max(min, Vector2.Min(max, new Vector2(-eventParam.moveDelta.x / component.width, eventParam.moveDelta.y / component.height) + m_targetCameraPosition));
                }
                else if ((eventParam.buttons & UIMouseButton.Right) != 0)
                {
                    m_targetRotation += eventParam.moveDelta.x;
                }
            }
        }
        private void ChangeViewZoom(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!m_viewLocked)
            {
                TargetZoom = Mathf.Max(Mathf.Min(TargetZoom + eventParam.wheelDelta * 0.125f, m_maxZoom), 1f / CurrentInfo.m_mesh.bounds.extents.magnitude / CurrentInfo.m_mesh.bounds.extents.magnitude);
            }
        }
        private void RedrawModel()
        {
            if (CurrentInfo == default)
            {
                return;
            }
            m_preview.isVisible = true;
            m_previewRenderer.RenderProp(CurrentInfo, m_cameraPosition, CurrentSelectedColor);
        }

        public void Update()
        {
            if (CurrentInfo != default && MainContainer.isVisible)
            {
                if (Mathf.Abs(m_previewRenderer.Zoom - TargetZoom) > 0.01f)
                {
                    m_previewRenderer.Zoom = Mathf.Lerp(m_previewRenderer.Zoom, TargetZoom, 0.25f);
                }
                if (Mathf.Abs(m_previewRenderer.CameraRotation - m_targetRotation) > 0.01f)
                {
                    m_previewRenderer.CameraRotation = Mathf.Lerp(m_previewRenderer.CameraRotation, m_targetRotation, 0.25f);
                }
                if (Mathf.Abs(m_cameraPosition.sqrMagnitude - m_targetCameraPosition.sqrMagnitude) > 0.0001f)
                {
                    m_cameraPosition = Vector2.Lerp(m_cameraPosition, m_targetCameraPosition, 0.25f);
                }

                RedrawModel();
            }
        }
    }

}
