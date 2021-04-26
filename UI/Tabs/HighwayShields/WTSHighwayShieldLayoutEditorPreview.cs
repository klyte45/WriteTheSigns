using ColossalFramework.UI;
using Klyte.Commons.UI.Sprites;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.ModShared;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{

    internal class WTSHighwayShieldLayoutEditorPreview : UICustomControl
    {

        public UIPanel MainContainer { get; protected set; }

        private UITextureSprite m_bg;
        private UITemplateList<UITextureSprite> m_layers;
        private UIPanel m_previewPanel;
        private UIPanel m_previewControls;
        private UIButton m_lockToSelection;
        private float m_targetRotation = 0;
        private Vector2 m_targetCameraPosition = default;
        private Vector3 m_cameraPosition = default;
        private bool m_viewLocked;

        private string m_overrideText = null;

        public UISprite OverrideSprite { get; private set; }
        private HighwayShieldDescriptor EditingInstancePreview => WTSHighwayShieldEditor.Instance.EditingInstance;

        public void Awake()
        {
            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Horizontal;

            KlyteMonoUtils.CreateUIElement(out m_previewPanel, MainContainer.transform, "previewPanel", new UnityEngine.Vector4(0, 0, 0, 300));
            m_previewPanel.autoLayout = false;
            m_previewPanel.disabledColor = Color.black;
            m_previewPanel.clipChildren = false;

            KlyteMonoUtils.CreateUIElement(out m_bg, m_previewPanel.transform, "previewSubPanel", new UnityEngine.Vector4(75, 0, WTSAtlasLoadingUtils.MAX_SIZE_IMAGE_IMPORT, WTSAtlasLoadingUtils.MAX_SIZE_IMAGE_IMPORT));
            m_bg.autoSize = false;
            m_bg.transform.localScale = Vector3.one * Mathf.Min(MainContainer.width, MainContainer.height) / WTSAtlasLoadingUtils.MAX_SIZE_IMAGE_IMPORT;
            m_bg.clipChildren = true;

            KlyteMonoUtils.CreateUIElement(out UIPanel overrideSpriteContainer, MainContainer.transform, "overrideSpriteContainer", new UnityEngine.Vector4(0, 0, MainContainer.width - 66, 300));
            overrideSpriteContainer.autoLayout = true;
            overrideSpriteContainer.autoLayoutDirection = LayoutDirection.Horizontal;

            KlyteMonoUtils.CreateUIElement(out UIPanel overrideSpriteSubContainer, overrideSpriteContainer.transform, "overrideSpriteSubContainer", new UnityEngine.Vector4(0, 0, overrideSpriteContainer.width, overrideSpriteContainer.height));
            overrideSpriteSubContainer.backgroundSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(LineIconSpriteNames.K45_SquareIcon, true);
            overrideSpriteSubContainer.autoLayout = false;
            overrideSpriteSubContainer.color = Color.gray;
            overrideSpriteSubContainer.autoLayoutDirection = LayoutDirection.Horizontal;

            OverrideSprite = overrideSpriteSubContainer.AddUIComponent<UISprite>();
            OverrideSprite.size = new Vector2(300, 300);
            OverrideSprite.relativePosition = new Vector2(75, 0);
            overrideSpriteSubContainer.isVisible = false;

            RegisterTemplate();
            m_layers = new UITemplateList<UITextureSprite>(m_bg, TEMPLATE_PREVIEW_LAYER_NAME);

            WTSHighwayShieldEditor.Instance.CurrentTabChanged += (x) => ReloadData();
        }

        private const string TEMPLATE_PREVIEW_LAYER_NAME = "K45_WTS_UITEXSPRITE_LAYER_TEMPLATE";

        private void RegisterTemplate()
        {
            var go = new GameObject();
            var uiTex = go.AddComponent<UITextureSprite>();
            uiTex.pivot = UIPivotPoint.Arbitrary;
            UITemplateUtils.GetTemplateDict()[TEMPLATE_PREVIEW_LAYER_NAME] = uiTex;
        }

        public void ReloadData()
        {
            var layers = m_layers.SetItemCount(EditingInstancePreview?.TextDescriptors?.Count ?? 0);
            IBridgeADR.AdrHighwayParameters parameters = null;
            if (layers.Length > 0)
            {
                parameters = WriteTheSignsMod.Controller.ConnectorADR.GetHighwayTypeData(EditingInstancePreview.SaveName);
            }
            for (var i = 0; i < layers.Length; i++)
            {
                var desc = EditingInstancePreview.TextDescriptors[i];
                var layer = m_layers.items[i];
                layer.relativePosition = desc.OffsetUV - new Vector2(desc.PivotUV.x * desc.OffsetUV.x, desc.PivotUV.y * desc.OffsetUV.y);
                var texture = desc.IsSpriteText()
                    ? desc.m_spriteParam?.GetCurrentSpriteInfo(null)?.texture
                    : desc.GetTargetText(parameters, out string text)
                        ? (FontServer.instance[desc.m_overrideFont] ?? FontServer.instance[WTSEtcData.Instance.FontSettings.GetTargetFont(desc.m_fontClass)] ?? FontServer.instance[WTSEtcData.Instance.FontSettings.GetTargetFont(FontClass.HighwayShields)] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY])
                            .DrawTextToTexture(text, desc.m_charSpacingFactor)
                        : null;
                if (texture is null)
                {
                    layer.texture = null;
                    continue;
                }
                var area = desc.GetAreaSize(m_bg.width, m_bg.height, texture.width, texture.height);
                layer.texture = texture;

                switch (desc.ColoringConfig.ColorSource)
                {
                    case BoardTextDescriptorGeneralXml.ColoringSettings.ColoringSource.Fixed:
                        layer.color = desc.ColoringConfig.m_cachedColor;
                        break;
                    case BoardTextDescriptorGeneralXml.ColoringSettings.ColoringSource.Contrast:
                        layer.color = KlyteMonoUtils.ContrastColor(EditingInstancePreview.BackgroundColor);
                        break;
                    case BoardTextDescriptorGeneralXml.ColoringSettings.ColoringSource.Parent:
                        layer.color = EditingInstancePreview.BackgroundColor;
                        break;
                }
                layer.area = area;
                layer.transform.localScale = Vector3.one;
            }
            if (EditingInstancePreview != null)
            {
                m_bg.texture = EditingInstancePreview.BackgroundImageParameter?.GetCurrentSpriteInfo(null)?.texture;
                m_bg.color = EditingInstancePreview.BackgroundColor;
            }
            else
            {
                m_bg.texture = null;
            }


        }
    }

}
