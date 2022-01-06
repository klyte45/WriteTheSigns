using ColossalFramework;
using ColossalFramework.UI;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Utils;
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
            var inputMatches = Regex.Match(value, "^(folder|assetFolder|image|assetImage)://(([^/]+)/)?([^/]+)$|^var://([a-zA-Z0-9_/]+)$");
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


        public class TextParameterVariableWrapper
        {
            public readonly string m_originalCommand;

            private enum VariableType
            {
                Invalid,
                Target
            }

            internal TextParameterVariableWrapper(string input)
            {
                m_originalCommand = input;
                var parameterPath = input.Split('/');
                if (parameterPath.Length > 0)
                {
                    switch (parameterPath[0])
                    {
                        case "target":
                            if (parameterPath.Length == 3 && byte.TryParse(parameterPath[1], out byte targIdx) && targIdx <= 4)
                            {
                                if (Enum.Parse(typeof(TextType), parameterPath[2]) is TextType tt)
                                {
                                    switch (tt)
                                    {
                                        case TextType.StreetSuffix:
                                        case TextType.StreetNameComplete:
                                        case TextType.StreetPrefix:
                                        case TextType.District:
                                        case TextType.Park:
                                        case TextType.PostalCode:
                                        case TextType.ParkOrDistrict:
                                        case TextType.DistrictOrPark:
                                            commonTextType = tt;
                                            index = targIdx;
                                            type = VariableType.Target;
                                            break;
                                    }
                                }



                            }
                            break;
                    }
                }
            }

            private VariableType type = VariableType.Invalid;
            private byte index = 0;
            private TextType commonTextType = TextType.None;

            public string GetTargetTextForBuilding(BoardInstanceBuildingXml buildingDescriptor, ushort buildingId, BoardTextDescriptorGeneralXml textDescriptor)
            {
                switch (type)
                {

                }
                return m_originalCommand;
            }

            public string GetTargetTextForNet(OnNetInstanceCacheContainerXml propDescriptor, ushort segmentId, BoardTextDescriptorGeneralXml textDescriptor)
            {
                switch (type)
                {
                    case VariableType.Target:
                        var targId = index == 0 ? segmentId : propDescriptor.GetTargetSegment(index);
                        if (targId == 0 || commonTextType == TextType.None)
                        {
                            return $"{commonTextType}@targ{index}";
                        }
                        else
                        {
                            TextType targetType = commonTextType;
                            switch (commonTextType)
                            {
                                case TextType.ParkOrDistrict: targetType = WTSOnNetData.Instance.GetCachedDistrictParkId(targId) > 0 ? TextType.Park : TextType.District; break;
                                case TextType.DistrictOrPark: targetType = WTSOnNetData.Instance.GetCachedDistrictId(targId) == 0 && WTSOnNetData.Instance.GetCachedDistrictParkId(targId) > 0 ? TextType.Park : TextType.District; break;
                                case TextType.Park:
                                    if (WTSOnNetData.Instance.GetCachedDistrictParkId(targId) == 0)
                                    {
                                        return "";
                                    }
                                    break;
                            }
                            RenderUtils.CacheArrayTypes cacheArrayType;
                            switch (targetType)
                            {
                                case TextType.StreetSuffix: cacheArrayType = RenderUtils.CacheArrayTypes.SuffixStreetName; break;
                                case TextType.StreetNameComplete: cacheArrayType = RenderUtils.CacheArrayTypes.FullStreetName; break;
                                case TextType.StreetPrefix: cacheArrayType = RenderUtils.CacheArrayTypes.StreetQualifier; break;
                                case TextType.District: targId = WTSOnNetData.Instance.GetCachedDistrictId(targId); cacheArrayType = RenderUtils.CacheArrayTypes.Districts; break;
                                case TextType.Park: targId = WTSOnNetData.Instance.GetCachedDistrictParkId(targId); cacheArrayType = RenderUtils.CacheArrayTypes.Parks; break;
                                case TextType.PostalCode: cacheArrayType = RenderUtils.CacheArrayTypes.SuffixStreetName; break;
                                case TextType.ParkOrDistrict: cacheArrayType = RenderUtils.CacheArrayTypes.SuffixStreetName; break;
                                case TextType.DistrictOrPark: cacheArrayType = RenderUtils.CacheArrayTypes.SuffixStreetName; break;
                                default:
                                    goto Fallback;
                            }
                            return RenderUtils.GetTargetStringFor(targId, textDescriptor.m_allCaps, textDescriptor.m_applyAbbreviations, cacheArrayType);
                        }
                }
            Fallback:
                return m_originalCommand;
            }
        }
    }
}
