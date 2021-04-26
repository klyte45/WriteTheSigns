using ColossalFramework.Threading;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.WriteTheSigns.ModShared.IBridgeADR;

namespace Klyte.WriteTheSigns.Sprites
{
    public class WTSHighwayShieldsAtlasLibrary : MonoBehaviour
    {

        public void Awake() => ResetHwShieldAtlas();

        #region Highway shields
        private UITextureAtlas m_hwShieldsAtlas;
        private Material m_hwShieldsMaterial;
        private bool HwShieldIsDirty { get; set; }
        private Dictionary<ushort, BasicRenderInformation> HighwayShieldsCache { get; } = new Dictionary<ushort, BasicRenderInformation>();

        private void ResetHwShieldAtlas()
        {
            m_hwShieldsAtlas = ScriptableObject.CreateInstance<UITextureAtlas>();
            m_hwShieldsAtlas.material = new Material(UIView.GetAView().defaultAtlas.material.shader);
        }
        public void PurgeShields()
        {
            HighwayShieldsCache.Clear();
            ResetHwShieldAtlas();
            m_hwShieldsMaterial = null;
            HwShieldIsDirty = true;
        }

        public BasicRenderInformation DrawHwShield(ushort seedId)
        {
            if (HighwayShieldsCache.TryGetValue(seedId, out BasicRenderInformation bri))
            {
                if (bri != null)
                {
                    return bri;
                }
            }
            else
            {
                HighwayShieldsCache[seedId] = null;
                if (WriteTheSignsMod.Controller.ConnectorADR.AddressesAvailable)
                {
                    StartCoroutine(WriteHwShieldTextureCoroutine(seedId));
                }
            }

            return null;

        }
        private IEnumerator WriteHwShieldTextureCoroutine(ushort seedId)
        {

            string id = $"{seedId}";

            if (m_hwShieldsAtlas[id] is null)
            {
                yield return 0;
                while (!CheckHwShieldCoroutineCanContinue())
                {
                    yield return null;
                }
                var hwData = WriteTheSignsMod.Controller.ConnectorADR.GetHighwayData(seedId);
                if (hwData is null)
                {
                    yield break;
                }
                WTSHighwayShieldsSingleton.GetTargetDescriptor(hwData.layoutName ?? "", out ConfigurationSource src, out HighwayShieldDescriptor layoutDescriptor);
                if (src == ConfigurationSource.NONE)
                {
                    yield break;
                }

                var drawingCoroutine = CoroutineWithData.From(this, RenderHighwayShield(
                    FontServer.instance[layoutDescriptor.FontName] ?? FontServer.instance[WTSEtcData.Instance.FontSettings.HighwayShieldsFont] ?? FontServer.instance[WTSController.DEFAULT_FONT_KEY],
                    layoutDescriptor, hwData));
                yield return drawingCoroutine.Coroutine;
                while (!CheckHwShieldCoroutineCanContinue())
                {
                    yield return null;
                }
                TextureAtlasUtils.RegenerateTextureAtlas(m_hwShieldsAtlas, new List<UITextureAtlas.SpriteInfo>
                {
                    new UITextureAtlas.SpriteInfo
                    {
                        name = id,
                        texture = drawingCoroutine.result
                    }
                });
                HwShieldIsDirty = true;
                m_hwShieldsMaterial = null;
                HighwayShieldsCache.Clear();
                StopAllCoroutines();
                yield break;
            }
            yield return 0;
            var bri = new BasicRenderInformation
            {
                m_YAxisOverflows = new RangeVector { min = 0, max = 20 },
            };

            yield return 0;
            WTSAtlasesLibrary.BuildMeshFromAtlas(id, bri, m_hwShieldsAtlas);
            yield return 0;
            if (m_hwShieldsMaterial is null)
            {
                m_hwShieldsMaterial = new Material(m_hwShieldsAtlas.material)
                {
                    shader = WTSController.DEFAULT_SHADER_TEXT,
                };
            }
            WTSAtlasesLibrary.RegisterMeshSingle(seedId, bri, HighwayShieldsCache, m_hwShieldsAtlas, HwShieldIsDirty, m_hwShieldsMaterial);
            HwShieldIsDirty = false;
            yield break;
        }
        private static bool CheckHwShieldCoroutineCanContinue()
        {
            if (m_lastCoroutineStepHS != SimulationManager.instance.m_currentTickIndex)
            {
                m_lastCoroutineStepHS = SimulationManager.instance.m_currentTickIndex;
                m_coroutineCounterHS = 0;
            }
            if (m_coroutineCounterHS >= 1)
            {
                return false;
            }
            m_coroutineCounterHS++;
            return true;
        }

        private static IEnumerator<Texture2D> RenderHighwayShield(DynamicSpriteFont defaultFont, HighwayShieldDescriptor descriptor, AdrHighwayParameters parameters)
        {
            if (defaultFont is null)
            {
                defaultFont = FontServer.instance[WTSEtcData.Instance.FontSettings.GetTargetFont(FontClass.HighwayShields)];
            }

            UITextureAtlas.SpriteInfo spriteInfo = descriptor.BackgroundImageParameter.GetCurrentSpriteInfo(null);
            if (spriteInfo is null)
            {
                LogUtils.DoWarnLog("HW: Background info is invalid for hw shield descriptor " + descriptor.SaveName);
                yield break;
            }
            else
            {

                int shieldHeight = spriteInfo.texture.height;
                int shieldWidth = spriteInfo.texture.width;
                var shieldTexture = new Texture2D(shieldWidth, shieldHeight);
                var targetColor = descriptor.BackgroundColorIsFromHighway && parameters.hwColor != default ? parameters.hwColor : descriptor.BackgroundColor;
                shieldTexture.SetPixels(spriteInfo.texture.GetPixels().Select(x => x.MultiplyChannelsButAlpha(targetColor)).ToArray());
                TextureScaler.scale(shieldTexture, WTSAtlasLoadingUtils.MAX_SIZE_IMAGE_IMPORT, WTSAtlasLoadingUtils.MAX_SIZE_IMAGE_IMPORT);
                Color[] formTexturePixels = shieldTexture.GetPixels();

                foreach (var textDescriptor in descriptor.TextDescriptors)
                {
                    if (!textDescriptor.GetTargetText(parameters, out string text))
                    {
                        continue;
                    }

                    Texture2D overlayTexture;
                    if (text is null && textDescriptor.m_textType == TextType.GameSprite)
                    {
                        var spriteTexture = textDescriptor.m_spriteParam?.GetCurrentSpriteInfo(null)?.texture;
                        if (spriteTexture is null)
                        {
                            continue;
                        }
                        overlayTexture = new Texture2D(spriteTexture.width, spriteTexture.height);
                        overlayTexture.SetPixels(spriteTexture.GetPixels());
                        overlayTexture.Apply();
                    }
                    else if (text is null)
                    {
                        continue;
                    }
                    else
                    {
                        var targetFont = FontServer.instance[textDescriptor.m_overrideFont] ?? FontServer.instance[WTSEtcData.Instance.FontSettings.GetTargetFont(textDescriptor.m_fontClass)] ?? defaultFont;
                        overlayTexture = targetFont.DrawTextToTexture(text, textDescriptor.m_charSpacingFactor);
                    }

                    if (overlayTexture is null)
                    {
                        continue;
                    }

                    Color textColor;
                    switch (textDescriptor.ColoringConfig.ColorSource)
                    {
                        case BoardTextDescriptorGeneralXml.ColoringSettings.ColoringSource.Contrast:
                            textColor = KlyteMonoUtils.ContrastColor(targetColor);
                            break;
                        case BoardTextDescriptorGeneralXml.ColoringSettings.ColoringSource.Parent:
                            textColor = targetColor;
                            break;
                        case BoardTextDescriptorGeneralXml.ColoringSettings.ColoringSource.Fixed:
                        default:
                            textColor = textDescriptor.ColoringConfig.m_cachedColor;
                            break;
                    }

                    Color[] overlayColorArray = overlayTexture.GetPixels().Select(x => new Color(textColor.r, textColor.g, textColor.b, x.a)).ToArray();

                    var textAreaSize = textDescriptor.GetAreaSize(shieldWidth, shieldHeight, overlayTexture.width, overlayTexture.height, true);
                    TextureScaler.scale(overlayTexture, Mathf.FloorToInt(textAreaSize.z), Mathf.FloorToInt(textAreaSize.w));

                    Color[] textColors = overlayTexture.GetPixels();
                    int textWidth = overlayTexture.width;
                    int textHeight = overlayTexture.height;
                    Destroy(overlayTexture);


                    Task<Tuple<Color[], int, int>> task = ThreadHelper.taskDistributor.Dispatch(() =>
                    {
                        int topMerge = Mathf.RoundToInt(textAreaSize.y);
                        int leftMerge = Mathf.RoundToInt(textAreaSize.x);
                        try
                        {
                            TextureRenderUtils.MergeColorArrays(colorOr: formTexturePixels,
                                                                widthOr: shieldWidth,
                                                                colors: textColors.Select(x => x.MultiplyChannelsButAlpha(textColor)).ToArray(),
                                                                startX: leftMerge,
                                                                startY: topMerge,
                                                                sizeX: textWidth,
                                                                sizeY: textHeight);
                        }
                        catch (Exception e)
                        {
                            LogUtils.DoErrorLog($"Exception while writing text in the shield: {e.Message}\n{e.StackTrace}\n\nDescriptor:{JsonUtility.ToJson(descriptor)}\ntextDescriptor: {textDescriptor?.SaveName}");
                        }
                        return Tuple.New(formTexturePixels, shieldWidth, shieldHeight);
                    });
                    while (!task.hasEnded || m_coroutineCounterHS > 1)
                    {
                        if (task.hasEnded)
                        {
                            m_coroutineCounterHS++;
                        }
                        yield return null;
                        if (m_lastCoroutineStepHS != SimulationManager.instance.m_currentTickIndex)
                        {
                            m_lastCoroutineStepHS = SimulationManager.instance.m_currentTickIndex;
                            m_coroutineCounterHS = 0;
                        }
                    }
                    m_coroutineCounterHS++;
                    formTexturePixels = task.result.First;
                }
                shieldTexture.SetPixels(formTexturePixels);
                shieldTexture.Apply();
                yield return shieldTexture;
            }
        }


        private static uint m_lastCoroutineStepHS = 0;
        private static uint m_coroutineCounterHS = 0;
        #endregion
    }
}