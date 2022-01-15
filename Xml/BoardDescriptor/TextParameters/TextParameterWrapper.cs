using ColossalFramework;
using ColossalFramework.UI;
using Klyte.WriteTheSigns.Data;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static ColossalFramework.UI.UITextureAtlas;

namespace Klyte.WriteTheSigns.Xml
{
    public class TextParameterWrapper
    {
        public enum ParameterType
        {
            TEXT,
            IMAGE,
            FOLDER,
            VARIABLE,
            EMPTY
        }
        public TextParameterWrapper()
        {
            ParamType = ParameterType.TEXT;
            TextOrSpriteValue = string.Empty;
        }
        public TextParameterWrapper(string value, bool acceptLegacy = false) : base()
        {
            if (value is null)
            {
                ParamType = ParameterType.EMPTY;
                IsEmpty = true;
                return;
            }
            var inputMatches = Regex.Match(value, "^(folder|assetFolder|image|assetImage)://(([^/]+)/)?([^/]+)$|^var://([a-zA-Z0-9_]+/.*)?$");
            if (inputMatches.Success)
            {
                switch (value.Split(':')[0])
                {
                    case "folder":
                        SetLocalFolder(inputMatches.Groups[4].Value);
                        return;
                    case "assetFolder":
                        SetAssetFolder();
                        return;
                    case "image":
                        SetLocalImage(inputMatches.Groups[3].Value, inputMatches.Groups[4].Value);
                        return;
                    case "assetImage":
                        SetAssetImage(inputMatches.Groups[4].Value);
                        return;
                    case "var":
                        SetVariableFromString(inputMatches.Groups[5].Value);
                        return;
                    default:
                        SetPlainString(value);
                        return;
                }
            }
            else if (acceptLegacy && (inputMatches = Regex.Match(value, "^IMG_(.*)$")).Success)
            {
                ParamType = ParameterType.IMAGE;
                isLocal = true;
                TextOrSpriteValue = inputMatches.Groups[1].Value;
            }
            else
            {
                TextOrSpriteValue = value;
                ParamType = ParameterType.TEXT;
            }
        }

        public void SetLocalFolder(string folderName)
        {
            ParamType = ParameterType.FOLDER;
            isLocal = true;
            atlasName = folderName;
            if (atlasName == "<ROOT>")
            {
                atlasName = string.Empty;
            }
        }

        public void SetAssetFolder()
        {
            ParamType = ParameterType.FOLDER;
            isLocal = false;
            atlasName = string.Empty;
        }

        public void SetLocalImage(string folder, string file)
        {
            ParamType = ParameterType.IMAGE;
            isLocal = true;
            atlasName = folder;
            if (folder is null || atlasName == "<ROOT>")
            {
                atlasName = string.Empty;
            }
            textOrSpriteValue = file;
        }

        public void SetAssetImage(string name)
        {
            ParamType = ParameterType.IMAGE;
            isLocal = false;
            atlasName = string.Empty;
            textOrSpriteValue = name;
        }

        public string TextOrSpriteValue
        {
            get => textOrSpriteValue; set
            {
                IsEmpty = value is null;
                textOrSpriteValue = IsEmpty ? "" : value;
            }
        }
        private TextParameterVariableWrapper VariableValue { get; set; }
        public string AtlasName
        {
            get => atlasName; private set
            {
                atlasName = value;
                m_isDirtyImage = true;
            }
        }
        public bool IsLocal
        {
            get => isLocal; private set
            {
                isLocal = value;
                m_isDirtyImage = true;
            }
        }
        public ParameterType ParamType { get; private set; }
        public bool IsEmpty { get; private set; }

        private UITextureAtlas m_cachedAtlas;
        private string atlasName;
        private bool isLocal;
        private bool m_isDirtyImage = true;
        private PrefabInfo cachedPrefab;
        private ulong cachedPrefabId;
        private string textOrSpriteValue;

        public void SetVariableFromString(string stringNoProtocol)
        {
            ParamType = ParameterType.VARIABLE;
            VariableValue = new TextParameterVariableWrapper(stringNoProtocol, Rendering.TextRenderingClass.PlaceOnNet);
        }

        public void SetPlainString(string value)
        {
            VariableValue = null;
            TextOrSpriteValue = value;
            ParamType = ParameterType.TEXT;
        }

        public UITextureAtlas GetAtlas(PrefabInfo prefab)
        {
            UpdateCachedAtlas(prefab);
            return m_cachedAtlas;
        }

        private void UpdateCachedAtlas(PrefabInfo prefab)
        {
            if (m_isDirtyImage)
            {
                if (ParamType == ParameterType.FOLDER || ParamType == ParameterType.IMAGE)
                {
                    UpdatePrefabInfo(prefab);
                    WriteTheSignsMod.Controller.AtlasesLibrary.GetAtlas(isLocal ? atlasName : cachedPrefabId.ToString(), out m_cachedAtlas);
                }
                else
                {
                    m_cachedAtlas = null;
                }
                m_isDirtyImage = false;
            }
        }

        public SpriteInfo GetCurrentSpriteInfo(PrefabInfo prefab)
        {
            UpdateCachedAtlas(prefab);
            return m_cachedAtlas?[TextOrSpriteValue];
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return null;
            }

            switch (ParamType)
            {
                case ParameterType.FOLDER:
                    return $"{(isLocal ? "folder" : "assetFolder")}://{(isLocal && !atlasName.IsNullOrWhiteSpace() ? atlasName : "<ROOT>")}";
                case ParameterType.IMAGE:
                    return $"{(isLocal ? "image" : "assetImage")}://{(isLocal && !atlasName.IsNullOrWhiteSpace() ? atlasName + "/" : "<ROOT>/")}{TextOrSpriteValue}";
                case ParameterType.VARIABLE:
                    return $"var://{VariableValue.m_originalCommand}";
                default:
                    return TextOrSpriteValue;
            }
        }

        private void UpdatePrefabInfo(PrefabInfo prefab)
        {
            if (cachedPrefab != prefab)
            {
                ulong.TryParse((prefab?.name ?? "").Split('.')[0], out cachedPrefabId);
                cachedPrefab = prefab;
            }
        }

        #region renderingInfo

        private static DynamicSpriteFont GetTargetFont(BoardInstanceXml instance, BoardTextDescriptorGeneralXml textDescriptor)
            => FontServer.instance.FirstOf(new[]
            {
                textDescriptor.m_overrideFont,
                WTSEtcData.Instance.FontSettings.GetTargetFont(textDescriptor.m_fontClass),
                instance.DescriptorOverrideFont,
                WTSEtcData.Instance.FontSettings.GetTargetFont(instance.RenderingClass),
            }.Where(x => !x.IsNullOrWhiteSpace()));

        public static BasicRenderInformation GetRenderInfo(BoardInstanceXml instance, BoardTextDescriptorGeneralXml textDescriptor, ushort refId, int secIdx, int tercIdx, out IEnumerable<BasicRenderInformation> multipleOutput)
        {
            multipleOutput = null;
        Restart:
            switch (textDescriptor.textContent)
            {
                case TextContent.None:
                    int lastParam = 11;
                    textDescriptor.UpdateContentType(instance.RenderingClass, ref lastParam);
                    goto Restart;
                case TextContent.ParameterizedText:
                    return (instance.GetParameter(textDescriptor.m_parameterIdx) ?? textDescriptor.DefaultParameterValue) is TextParameterWrapper tpw
                        ? tpw.GetTargetText(instance, textDescriptor, GetTargetFont(instance, textDescriptor), refId, secIdx, tercIdx, out multipleOutput)
                        : GetTargetFont(instance, textDescriptor)?.DrawString(WriteTheSignsMod.Controller, $"<PARAM#{textDescriptor.m_parameterIdx} NOT SET>", default, FontServer.instance.ScaleEffective);
                case TextContent.ParameterizedSpriteFolder:
                    return (instance.GetParameter(textDescriptor.m_parameterIdx) ?? textDescriptor.DefaultParameterValue) is TextParameterWrapper tpw2
                        ? tpw2.GetSpriteFromCycle(textDescriptor, instance.TargetAssetParameter, refId, secIdx, tercIdx)
                        : WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsNotSet");
                case TextContent.ParameterizedSpriteSingle:
                    return (instance.GetParameter(textDescriptor.m_parameterIdx) ?? textDescriptor.DefaultParameterValue) is TextParameterWrapper tpw3
                        ? tpw3.GetSpriteFromParameter(instance.TargetAssetParameter)
                        : WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsNotSet");
                case TextContent.LinesNameList:
                    break;
                case TextContent.HwShield:
                    break;
                case TextContent.TimeTemperature:
                    break;
                case TextContent.LinesSymbols:
                    break;
            }
            return null;
        }

        private BasicRenderInformation GetSpriteFromParameter(PrefabInfo prop)
            => IsEmpty
                    ? null
                    : GetImageBRI(prop);
        public BasicRenderInformation GetImageBRI(PrefabInfo prefab)
        {
            if (ParamType == ParameterType.IMAGE)
            {
                if (isLocal)
                {
                    return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(atlasName, TextOrSpriteValue, true);
                }
                else
                {
                    UpdatePrefabInfo(prefab);
                    return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(cachedPrefabId.ToString(), TextOrSpriteValue, cachedPrefabId == default) ?? WriteTheSignsMod.Controller.AtlasesLibrary.GetFromAssetAtlases(cachedPrefabId, TextOrSpriteValue, true);
                }
            }
            else
            {
                return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsImageRequired");
            }

        }

        public BasicRenderInformation GetTargetText(BoardInstanceXml descriptorBuilding, BoardTextDescriptorGeneralXml textDescriptor, DynamicSpriteFont targetFont, ushort refId, int secId, int tercId, out IEnumerable<BasicRenderInformation> multipleOutput)
        {
            if (ParamType != ParameterType.VARIABLE)
            {
                multipleOutput = null;
                return targetFont?.DrawString(WriteTheSignsMod.Controller, ToString(), default, FontServer.instance.ScaleEffective);
            }
            else
            {
                return VariableValue.GetTargetText(descriptorBuilding, textDescriptor, targetFont, refId, secId, tercId, out multipleOutput);
            }
        }

        public string GetOriginalVariableParam() => ParamType != ParameterType.VARIABLE ? null : VariableValue.m_originalCommand;


        private BasicRenderInformation GetSpriteFromCycle(BoardTextDescriptorGeneralXml textDescriptor, PrefabInfo cachedPrefab, ushort refId, int boardIdx, int secIdx)
        {
            if (IsEmpty)
            {
                return null;
            }
            if (ParamType != TextParameterWrapper.ParameterType.FOLDER)
            {
                return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsFolderRequired");
            }
            if (textDescriptor.AnimationSettings.m_itemCycleFramesDuration < 1)
            {
                textDescriptor.AnimationSettings.m_itemCycleFramesDuration = 100;
            }
            return GetCurrentSprite(cachedPrefab, (int length) => (int)(((SimulationManager.instance.m_currentFrameIndex + textDescriptor.AnimationSettings.m_extraDelayCycleFrames + (refId * (1 + boardIdx) + (11345476 * secIdx))) % (length * textDescriptor.AnimationSettings.m_itemCycleFramesDuration) / textDescriptor.AnimationSettings.m_itemCycleFramesDuration)));
        }
        private BasicRenderInformation GetCurrentSprite(PrefabInfo prefab, Func<int, int> p)
        {
            if (ParamType == ParameterType.FOLDER)
            {
                if (isLocal)
                {
                    return WriteTheSignsMod.Controller.AtlasesLibrary.GetSlideFromLocal(atlasName, p, true);
                }
                else
                {
                    UpdatePrefabInfo(prefab);
                    return WriteTheSignsMod.Controller.AtlasesLibrary.GetSlideFromLocal(cachedPrefabId.ToString(), p, cachedPrefabId == default) ?? WriteTheSignsMod.Controller.AtlasesLibrary.GetSlideFromAsset(cachedPrefabId, p, true);
                }
            }
            else
            {
                return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsFolderRequired");
            }

        }
        #endregion
    }
}
