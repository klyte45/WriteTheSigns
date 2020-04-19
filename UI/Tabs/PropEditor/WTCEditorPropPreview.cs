using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Rendering;
using Klyte.WriteTheCity.Xml;
using SpriteFontPlus;
using UnityEngine;

namespace Klyte.WriteTheCity.UI
{

    internal class WTCEditorPropPreview : UICustomControl
    {

        public UIPanel MainContainer { get; protected set; }

        private WTCPropPreviewRenderer m_previewRenderer;
        private UIPanel m_previewPanel;
        private UITextureSprite m_preview;
        private UIPanel m_previewControls;
        private UIButton m_lockToSelection;
        private float m_targetRotation = 0;
        private Vector2 m_targetCameraPosition = default;
        private Vector3 m_cameraPosition = default;
        private bool m_viewLocked;

        private string m_overrideText = null;

        private PropInfo CurrentInfo => WTCPropTextLayoutEditor.Instance.CurrentInfo;
        private int TabToPreview => WTCPropTextLayoutEditor.Instance.CurrentTab - 1;
        private BoardDescriptorGeneralXml EditingInstance => WTCPropTextLayoutEditor.Instance.EditingInstance;

        private BoardTextDescriptorGeneralXml CurrentTextDescriptor => TabToPreview >= 0 && TabToPreview < EditingInstance.m_textDescriptors.Length ? EditingInstance.m_textDescriptors[TabToPreview] : default;

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


            KlyteMonoUtils.InitCircledButton(m_previewControls, out m_lockToSelection, CommonsSpriteNames.K45_Unlock, (x, y) => ToggleLock(), "K45_WTC_LOCK_UNLOCK_TO_CURRENT_ITEM");
            m_lockToSelection.focusedBgSprite = null;
            m_viewLocked = true;
            ToggleLock();

            KlyteMonoUtils.InitCircledButton(m_previewControls, out UIButton resetView, CommonsSpriteNames.K45_Reload, (x, y) => ResetCamera(), "K45_WTC_RESET_VIEW");

            UIHelperExtension.AddSpace(m_previewControls, 10);

            KlyteMonoUtils.InitCircledButton(m_previewControls, out UIButton useCurrentText, CommonsSpriteNames.K45_FontIcon, (x, y) => m_overrideText = null, "K45_WTC_USE_CURRENT_TEXT");
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use1lText, "x1", (x, y) => m_overrideText = "1", Locale.Get("K45_WTC_USE_1LENGHT_TEXT"));
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use10lText, "x10", (x, y) => m_overrideText = "Á" + new string('X', 8) + "j", Locale.Get("K45_WTC_USE_10LENGHT_TEXT"));
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use50lText, "x50", (x, y) => m_overrideText = "Á" + new string('L', 48) + "j", Locale.Get("K45_WTC_USE_50LENGHT_TEXT"));
            KlyteMonoUtils.InitCircledButtonText(m_previewControls, out UIButton use100lText, "x200", (x, y) => m_overrideText = "Á" + new string('C', 198) + "j", Locale.Get("K45_WTC_USE_200LENGHT_TEXT"));

            WTCPropTextLayoutEditor.Instance.CurrentTabChanged += (x) =>
            {
                ResetCamera();
            };
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
                    min = CurrentInfo.m_mesh.bounds.min;
                    max = CurrentInfo.m_mesh.bounds.max;
                    min.y = -max.y;
                    max.y = -CurrentInfo.m_mesh.bounds.min.y;
                    float multiplier = 1 / TargetZoom;
                    float moveMultiplier = 1;
                    if (CurrentTextDescriptor != null)
                    {
                        float regularMagn = CurrentInfo.m_mesh.bounds.extents.magnitude / WTCPropRenderingRules.SCALING_FACTOR;
                        Vector3 textExt = WTCPropRenderingRules.GetTextMesh(FontServer.instance[EditingInstance.FontName ?? WTCController.DEFAULT_FONT_KEY], CurrentTextDescriptor, 0, 0, 0, m_previewRenderer.GetDefaultInstance())?.m_mesh?.bounds.extents ?? default;

                        if (CurrentTextDescriptor.m_maxWidthMeters > 0)
                        {
                            textExt.x = Mathf.Min(textExt.x * CurrentTextDescriptor.m_textScale, CurrentTextDescriptor.m_maxWidthMeters / WTCPropRenderingRules.SCALING_FACTOR) / CurrentTextDescriptor.m_textScale;
                        }
                        float magnitude = Mathf.Min(regularMagn * 3, Mathf.Max(regularMagn, (textExt * CurrentTextDescriptor.m_textScale).magnitude)) / CurrentTextDescriptor.m_textScale;
                        multiplier *= magnitude / regularMagn / WTCPropRenderingRules.SCALING_FACTOR;
                        moveMultiplier /= WTCPropRenderingRules.SCALING_FACTOR;
                    }

                    m_targetCameraPosition = Vector2.Max(min * multiplier, Vector2.Min(max * multiplier, new Vector2(-eventParam.moveDelta.x / component.width * moveMultiplier, eventParam.moveDelta.y / component.height * moveMultiplier) + m_targetCameraPosition));
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
            m_previewRenderer.RenderProp(CurrentInfo, m_cameraPosition, new Vector3(0, CameraRotation), EditingInstance, CurrentTextDescriptor != null ? TabToPreview : -1, m_overrideText);
        }

        public void Update()
        {
            if (CurrentInfo != default && MainContainer.isVisible)
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
        }
    }

}
