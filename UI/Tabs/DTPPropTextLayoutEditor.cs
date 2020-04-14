using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using UnityEngine;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPPropTextLayoutEditor : UICustomControl
    {
        public static DTPPropTextLayoutEditor Instance { get; private set; }
        public UIScrollablePanel MainContainer { get; protected set; }

        protected UIHelperExtension m_uiHelperHS;

        private DTPPropPreviewRenderer m_previewRenderer;

        private UITextureSprite m_preview;

        private PropInfo m_currentInfo;

        private float m_targetZoom = 3;
        private float m_targetRotation = 0;
        private Vector2 m_targetCameraPosition = default;
        private Vector2 m_cameraPosition = default;

        public void Awake()
        {
            Instance = this;

            MainContainer = GetComponent<UIScrollablePanel>();

            KlyteMonoUtils.CreateUIElement(out m_preview, MainContainer.transform, "preview", new UnityEngine.Vector4(0, 0, MainContainer.width, 400));
            KlyteMonoUtils.CreateElement(out m_previewRenderer, MainContainer.transform);
            m_previewRenderer.Size = m_preview.size * 2f;
            m_preview.texture = m_previewRenderer.Texture;
            m_preview.eventMouseWheel += ChangeViewZoom;
            m_preview.eventMouseMove += OnMouseMove;
            m_previewRenderer.Zoom = m_targetZoom;
            m_previewRenderer.CameraRotation = 40;

            m_currentInfo = PrefabCollection<PropInfo>.FindLoaded("1679673551.Street Plate_Data");
        }

        private void OnMouseMove(UIComponent component, UIMouseEventParameter eventParam)
        {
            if ((eventParam.buttons & UIMouseButton.Left) != 0)
            {
                m_targetCameraPosition = Vector2.Max(new Vector2(-3, -3), Vector2.Min(new Vector2(3, 0), eventParam.moveDelta / 100 + m_targetCameraPosition));
            }
            else if ((eventParam.buttons & UIMouseButton.Right) != 0)
            {
                m_targetRotation += eventParam.moveDelta.x;
            }
        }
        private void ChangeViewZoom(UIComponent component, UIMouseEventParameter eventParam) => m_targetZoom = Mathf.Max(Mathf.Min(m_targetZoom + eventParam.wheelDelta, 6), 0.5f);

        public void Update()
        {
            if (m_currentInfo != default && MainContainer.isVisible)
            {
                if (Mathf.Abs(m_previewRenderer.Zoom - m_targetZoom) > 0.01f)
                {
                    m_previewRenderer.Zoom = Mathf.Lerp(m_previewRenderer.Zoom, m_targetZoom, 0.25f);
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

        private void RedrawModel()
        {
            if (m_currentInfo == default)
            {
                return;
            }
            m_preview.isVisible = true;
            m_previewRenderer.RenderProp(m_currentInfo, m_cameraPosition, Color.white);
        }
    }

}
