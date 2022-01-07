using ColossalFramework;
using ColossalFramework.UI;
using SpriteFontPlus.Utility;
using System;
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
            VARIABLE
        }
        public TextParameterWrapper()
        {
            ParamType = ParameterType.TEXT;
            TextOrSpriteValue = string.Empty;
        }
        public TextParameterWrapper(string value, bool acceptLegacy = false)
        {
            var inputMatches = Regex.Match(value, "^(folder|assetFolder|image|assetImage)://(([^/]+)/)?([^/]+)$|^var://([a-zA-Z0-9_]+/.*)?$");
            if (inputMatches.Success)
            {
                switch (inputMatches.Groups[0].Value.Split(':')[0])
                {
                    case "folder":
                        ParamType = ParameterType.FOLDER;
                        isLocal = true;
                        atlasName = inputMatches.Groups[4].Value;
                        if (atlasName == "<ROOT>")
                        {
                            atlasName = string.Empty;
                        }
                        return;
                    case "assetFolder":
                        ParamType = ParameterType.FOLDER;
                        isLocal = false;
                        atlasName = string.Empty;
                        return;
                    case "image":
                        ParamType = ParameterType.IMAGE;
                        isLocal = true;
                        atlasName = inputMatches.Groups[3].Value;
                        if (atlasName == "<ROOT>")
                        {
                            atlasName = string.Empty;
                        }
                        TextOrSpriteValue = inputMatches.Groups[4].Value;
                        return;
                    case "assetImage":
                        ParamType = ParameterType.IMAGE;
                        isLocal = false;
                        atlasName = string.Empty;
                        TextOrSpriteValue = inputMatches.Groups[4].Value;
                        return;
                    case "var":
                        ParamType = ParameterType.VARIABLE;
                        VariableValue = new TextParameterVariableWrapper(inputMatches.Groups[5].Value);
                        return;
                    default:
                        TextOrSpriteValue = value;
                        ParamType = ParameterType.TEXT;
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

        private string TextOrSpriteValue { get; set; }
        private TextParameterVariableWrapper VariableValue { get; set; }
        public string AtlasName
        {
            get => atlasName; set
            {
                atlasName = value;
                m_isDirtyImage = true;
            }
        }
        public bool IsLocal
        {
            get => isLocal; set
            {
                isLocal = value;
                m_isDirtyImage = true;
            }
        }
        public ParameterType ParamType { get; set; }

        private UITextureAtlas m_cachedAtlas;
        private string atlasName;
        private bool isLocal;
        private bool m_isDirtyImage = true;

        private PrefabInfo cachedPrefab;
        private ulong cachedPrefabId;

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

        private void UpdatePrefabInfo(PrefabInfo prefab)
        {
            if (cachedPrefab != prefab)
            {
                ulong.TryParse((prefab?.name ?? "").Split('.')[0], out cachedPrefabId);
                cachedPrefab = prefab;
            }
        }

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

        public string GetTargetTextForBuilding(BoardInstanceBuildingXml descriptorBuilding, ushort buildingId, BoardTextDescriptorGeneralXml textDescriptor)
        {
            if (ParamType != ParameterType.VARIABLE)
            {
                return ToString();
            }
            return VariableValue.GetTargetTextForBuilding(descriptorBuilding, buildingId, textDescriptor);
        }
        public string GetTargetTextForNet(OnNetInstanceCacheContainerXml descriptorProp, ushort segmentId, BoardTextDescriptorGeneralXml textDescriptor)
        {
            if (ParamType != ParameterType.VARIABLE)
            {
                return ToString();
            }
            return VariableValue.GetTargetTextForNet(descriptorProp, segmentId, textDescriptor);
        }

        public override string ToString()
        {
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

        internal BasicRenderInformation GetCurrentSprite(PrefabInfo prefab, Func<int, int> p)
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

    }
}
