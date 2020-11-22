using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSPropLayoutEditorPreview : UICustomControl
    {

        public UIPanel MainContainer { get; protected set; }

        private WTSPropPreviewRenderer m_previewRenderer;
        private UIPanel m_previewPanel;
        private UITextureSprite m_preview;
        private UIPanel m_previewControls;
        private UIButton m_lockToSelection;
        private float m_targetRotation = 0;
        private Vector2 m_targetCameraPosition = default;
        private Vector3 m_cameraPosition = default;
        private bool m_viewLocked;

        private string m_overrideText = null;

        private PropInfo CurrentInfo => WTSPropLayoutEditor.Instance.CurrentPropInfo;
        private int TabToPreview => WTSPropLayoutEditor.Instance.CurrentTab - 1;
        private BoardDescriptorGeneralXml EditingInstancePreview => WTSPropLayoutEditor.Instance.EditingInstance;

        private BoardTextDescriptorGeneralXml CurrentTextDescriptor => TabToPreview >= 0 && TabToPreview < EditingInstancePreview.TextDescriptors.Length ? EditingInstancePreview.TextDescriptors[TabToPreview] : default;

        public float TargetZoom { get; set; } = 3;
        public float CameraRotation { get; private set; }

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
            m_previewPanel.disabledColor = Color.black;

            KlyteMonoUtils.CreateUIElement(out m_preview, m_previewPanel.transform, "preview", new UnityEngine.Vector4(0, 0, m_previewPanel.width, m_previewPanel.height));
            KlyteMonoUtils.CreateElement(out m_previewRenderer, MainContainer.transform);
            m_previewRenderer.Size = m_preview.size * 2f;
            m_preview.texture = m_previewRenderer.Texture;
            m_preview.eventMouseWheel += ChangeViewZoom;
            m_preview.eventMouseMove += OnMouseMove;
            m_previewRenderer.Zoom = TargetZoom;
            m_preview.disabledColor = Color.black;

            KlyteMonoUtils.CreateUIElement(out m_previewControls, MainContainer.transform, "controls", new UnityEngine.Vector4(0, 0, 50, 300));
            m_previewControls.padding = new RectOffset(5, 5, 5, 5);
            m_previewControls.autoLayout = true;
            m_previewControls.autoLayoutDirection = LayoutDirection.Vertical;


            KlyteMonoUtils.InitCircledButton(m_previewControls, out m_lockToSelection, CommonsSpriteNames.K45_Unlock, (x, y) => ToggleLock(), "K45_WTS_LOCK_UNLOCK_TO_CURRENT_ITEM");
            m_lockToSelection.focusedBgSprite = null;
            m_viewLocked = true;
            ToggleLock();

            KlyteMonoUtils.InitCircledButton(m_previewControls, out UIButton resetView, CommonsSpriteNames.K45_Reload, (x, y) => ResetCamera(), "K45_WTS_RESET_VIEW");

            UIHelperExtension.AddSpace(m_previewControls, 10);

            KlyteMonoUtils.InitCircledButton(m_previewControls, out UIButton useCurrentText, CommonsSpriteNames.K45_FontIcon, (x, y) => m_overrideText = null, "K45_WTS_USE_CURRENT_TEXT");
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use1lText, "x1", (x, y) => m_overrideText = "1", Locale.Get("K45_WTS_USE_1LENGHT_TEXT"));
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use10lText, "x10", (x, y) => m_overrideText = "Á" + new string('X', 8) + "j", Locale.Get("K45_WTS_USE_10LENGHT_TEXT"));
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use50lText, "x50", (x, y) => m_overrideText = "Á" + new string('L', 48) + "j", Locale.Get("K45_WTS_USE_50LENGHT_TEXT"));
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use100lText, "x200", (x, y) => m_overrideText = "Á" + new string('C', 198) + "j", Locale.Get("K45_WTS_USE_200LENGHT_TEXT"));

            WTSPropLayoutEditor.Instance.CurrentTabChanged += (x) => ResetCamera();
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
                    Vector3 min;
                    Vector3 max;
                    max = new Vector3(Mathf.Max(Mathf.Abs(CurrentInfo.m_mesh.bounds.max.x - CurrentInfo.m_mesh.bounds.min.x), Mathf.Abs(CurrentInfo.m_mesh.bounds.max.z - CurrentInfo.m_mesh.bounds.min.z)), Mathf.Max(Mathf.Abs(CurrentInfo.m_mesh.bounds.max.y - CurrentInfo.m_mesh.bounds.min.y)), Mathf.Max(Mathf.Abs(CurrentInfo.m_mesh.bounds.max.x - CurrentInfo.m_mesh.bounds.min.x), Mathf.Abs(CurrentInfo.m_mesh.bounds.max.z - CurrentInfo.m_mesh.bounds.min.z)));
                    min = -max;
                    min.y = -max.y;
                    max.y = -CurrentInfo.m_mesh.bounds.min.y;
                    float moveMultiplier = 1;
                    if (CurrentTextDescriptor != null)
                    {
                        Vector3 textExt = WTSDynamicTextRenderingRules.GetTextMesh(CurrentTextDescriptor, 0, 0, 0, m_previewRenderer.GetDefaultInstance(), m_previewRenderer.GetDefaultInstance().Descriptor, out _)?.m_mesh?.bounds.extents ?? default;

                        if (CurrentTextDescriptor.m_maxWidthMeters > 0)
                        {
                            textExt.x = Mathf.Min(textExt.x * CurrentTextDescriptor.m_textScale, CurrentTextDescriptor.m_maxWidthMeters / WTSDynamicTextRenderingRules.SCALING_FACTOR) / CurrentTextDescriptor.m_textScale;
                        }
                        moveMultiplier /= WTSDynamicTextRenderingRules.SCALING_FACTOR;
                    }

                    m_targetCameraPosition = Vector2.Max(min, Vector2.Min(max, new Vector2(-eventParam.moveDelta.x / component.width * moveMultiplier, eventParam.moveDelta.y / component.height * moveMultiplier) + m_targetCameraPosition));
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
                TargetZoom = Mathf.Max(Mathf.Min(TargetZoom + eventParam.wheelDelta * 0.125f, m_maxZoom), 1f / CurrentInfo.m_mesh.bounds.extents.magnitude / CurrentInfo.m_mesh.bounds.extents.magnitude * Mathf.Min(1, CurrentTextDescriptor?.m_textScale ?? 1));
            }
        }
        private void RedrawModel()
        {
            if (CurrentInfo == default)
            {
                return;
            }
            m_preview.isVisible = true;
            m_previewControls.isVisible = true;
            m_previewRenderer.RenderPrefab(CurrentInfo, m_cameraPosition, new Vector3(0, CameraRotation), EditingInstancePreview?.TextDescriptors, CurrentTextDescriptor != null ? TabToPreview : -1, m_overrideText, EditingInstancePreview);
        }

        public void Update()
        {
            if (CurrentInfo != default && MainContainer.isEnabled)
            {
                if (Mathf.Abs(m_previewRenderer.Zoom - TargetZoom) > 0.01f)
                {
                    m_previewRenderer.Zoom = Mathf.Lerp(m_previewRenderer.Zoom, TargetZoom, 0.25f);
                }
                if (Mathf.Abs(CameraRotation - m_targetRotation) > 0.01f)
                {
                    CameraRotation = Mathf.Lerp(CameraRotation, m_targetRotation, 0.25f);
                }
                if (Mathf.Abs(m_cameraPosition.sqrMagnitude - m_targetCameraPosition.sqrMagnitude) > 0.0001f)
                {
                    m_cameraPosition = Vector2.Lerp(m_cameraPosition, m_targetCameraPosition, 0.25f);
                }

                RedrawModel();
            }
            else
            {
                m_preview.isVisible = false;
                m_previewControls.isVisible = false;
            }


        }
    }

}
