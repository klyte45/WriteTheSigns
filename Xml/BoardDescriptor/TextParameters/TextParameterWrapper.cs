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
            VariableValue = new TextParameterVariableWrapper(stringNoProtocol);
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

        public string GetTargetTextForBuilding(BoardInstanceBuildingXml descriptorBuilding, ushort buildingId, BoardTextDescriptorGeneralXml textDescriptor) => ParamType != ParameterType.VARIABLE
                ? ToString()
                : VariableValue.GetTargetTextForBuilding(descriptorBuilding, buildingId, textDescriptor);
        public string GetTargetTextForNet(OnNetInstanceCacheContainerXml descriptorProp, ushort segmentId, BoardTextDescriptorGeneralXml textDescriptor)
        {
            if (ParamType != ParameterType.VARIABLE)
            {
                return ToString();
            }
            return VariableValue.GetTargetTextForNet(descriptorProp, segmentId, textDescriptor);
        }
        public string GetOriginalVariableParam()
        {
            if (ParamType != ParameterType.VARIABLE)
            {
                return null;
            }
            return VariableValue.m_originalCommand;
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
