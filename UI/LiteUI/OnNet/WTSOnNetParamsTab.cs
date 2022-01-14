using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.LiteUI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    public class WTSOnNetParamsTab
    {
        private const string f_base = "K45_WTS_OnNetInstanceCacheContainerXml_";
        private const string f_imageProtocolSelect = f_base + "ImageProtocol";
        private const string f_imageSearchText = f_base + "SearchText";

        private readonly string[] v_protocolsImg = new[] { Locale.Get("K45_WTS_IMAGESRC_ASSET"), Locale.Get("K45_WTS_IMAGESRC_LOCAL") };
        private readonly string[] v_protocolsFld = new[] { Locale.Get("K45_WTS_FOLDERSRC_ASSET"), Locale.Get("K45_WTS_FOLDERSRC_LOCAL") };
        private readonly string[] v_protocolsTxt = new[] { Locale.Get("K45_WTS_PARAMTYPE_PLAINTEXT"), Locale.Get("K45_WTS_PARAMTYPE_VARIABLE") };
        private const string v_emptyPlaceholderOption = "<color=#FF00FF>--Empty--</color>";

        private Vector2 m_tabViewScroll;
        private Vector2 m_leftPanelScroll;
        private Vector2 m_rightPanelScroll;
        private State m_currentState = State.List;
        private int m_currentEditingParam = 0;

        private string Text = Color.cyan.ToRGB();
        private string Image = Color.green.ToRGB();
        private string Folder = Color.yellow.ToRGB();

        #region Basic Behavior
        internal bool ShowTabsOnTop() => m_currentState == State.List;
        public void Reset()
        {
            m_currentState = State.List;
            m_currentEditingParam = 0;
        }
        public void DrawArea(OnNetInstanceCacheContainerXml item, Rect areaRect)
        {
            switch (m_currentState)
            {
                case State.List:
                    DrawListing(item);
                    break;
                case State.GettingImage:
                    DrawImagePicker(item, areaRect);
                    break;
                case State.GettingFolder:
                    DrawFolderPicker(item, areaRect);
                    break;
                case State.GettingText:
                    DrawVariablePicker(item, areaRect);
                    break;
            }
        }


        private void DrawListing(OnNetInstanceCacheContainerXml item) => GUIKlyteCommons.DoInScroll(ref m_tabViewScroll, () =>
        {
            var paramsUsed = item.GetAllParametersUsedWithData();
            if ((paramsUsed?.Count ?? 0) > 0)
            {
                foreach (var kv in paramsUsed)
                {
                    var contentTypes = kv.Value.GroupBy(x => x.m_textType).Select(x => x.Key);
                    if (contentTypes.Count() > 1)
                    {
                        GUILayout.Label(string.Format(Locale.Get($"K45_WTS_ONNETEDITOR_TEXTPARAM"), kv.Key));
                        GUILayout.Label(Locale.Get($"K45_WTS_ONNETEDITOR_INVALIDPARAMSETTINGS_DIFFERENTKINDSAMEPARAM"));
                        GUILayout.Space(4);
                        continue;
                    }
                    var targetContentType = contentTypes.First();
                    string target = "FFFFFF";
                    switch (targetContentType)
                    {
                        case TextType.ParameterizedGameSpriteIndexed:
                            target = Image;
                            break;
                        case TextType.ParameterizedGameSprite:
                            target = Folder;
                            break;
                        case TextType.ParameterizedText:
                            target = Text;
                            break;
                    }
                    var usedByText = string.Join("\n", kv.Value.Select(x => "\t\u2022 " + (x.ParameterDisplayName.IsNullOrWhiteSpace() ? x.SaveName : x.ParameterDisplayName)).ToArray());
                    GUIKlyteCommons.DoInHorizontal(() =>
                    {
                        GUILayout.Label(string.Format(Locale.Get($"K45_WTS_ONNETEDITOR_TEXTPARAM"), kv.Key) + $"\n<color=#{target}>{Locale.Get("K45_WTS_BOARD_TEXT_TYPE_DESC", targetContentType.ToString())}</color>\n\n{usedByText}");
                        var param = item.GetTextParameter(kv.Key);
                        if (GUILayout.Button(param is null ? "<color=#FF00FF>--NULL--</color>" : param.IsEmpty ? "<color=#FFFF00>--EMPTY--</color>" : param.ToString(), GUILayout.ExpandHeight(true)))
                        {
                            if (param is null)
                            {
                                item.SetTextParameter(kv.Key, null);
                                param = item.GetTextParameter(kv.Key);
                            }
                            GoToPicker(kv.Key, targetContentType, param, item);
                        }
                    });

                }

            }
        });
        private void DrawImagePicker(OnNetInstanceCacheContainerXml item, Rect areaRect) => DrawSelectorView(item, areaRect, DrawTopImage, DrawLeftPanelImage, DrawRightPanelImage, OnClearImage);
        private void DrawFolderPicker(OnNetInstanceCacheContainerXml item, Rect areaRect) => DrawSelectorView(item, areaRect, DrawTopFolder, DrawLeftPanelFolder, DrawRightPanelFolder, OnClearFolder);
        private void DrawVariablePicker(OnNetInstanceCacheContainerXml item, Rect areaRect) => DrawSelectorView(item, areaRect, DrawTopVariable, DrawLeftPanelVariable, DrawRightPanelVariable, OnClearVariable, m_isVariable);

        #endregion

        #region Param editor commons
        private void DrawSelectorView(OnNetInstanceCacheContainerXml item, Rect areaRect, DrawInAreaFunc topFunc, DrawInAreaFunc leftPanelFunc, Action rightPanelFunc, Action onClearFunc, bool showRightPanel = true)
        {
            topFunc(ref areaRect);
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUIKlyteCommons.DoInScroll(ref m_leftPanelScroll, false, true, () =>
                {
                    leftPanelFunc(ref areaRect);
                }, GUILayout.Width(areaRect.width / 2), GUILayout.Height(areaRect.height - 80));
                GUIKlyteCommons.DoInVertical(() =>
                {
                    if (showRightPanel)
                    {
                        GUILayout.Label(GetCurrentParamString(), new GUIStyle(GUI.skin.label) { normal = new GUIStyleState() { textColor = Color.yellow } });
                        GUILayout.Space(8);
                        GUIKlyteCommons.DoInScroll(ref m_rightPanelScroll, rightPanelFunc);
                    }
                }, GUILayout.Width(areaRect.width / 2), GUILayout.Height(areaRect.height - 80));
            });
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                if (GUILayout.Button(Locale.Get("CANCEL"), GUILayout.Width(areaRect.width / 3)))
                {
                    m_currentState = State.List;
                }
                if (GUILayout.Button(Locale.Get("DEBUG_CLEAR"), GUILayout.Width(areaRect.width / 3)))
                {
                    onClearFunc();
                }
                if (GUILayout.Button("OK", GUILayout.Width(areaRect.width / 3)))
                {
                    GetBackToList(item);
                }
            });
        }

        private string GetCurrentParamString()
        {
            switch (m_currentState)
            {
                case State.GettingImage:
                    return $"{(m_isLocal ? "image" : "assetImage")}://{(m_selectedFolder is null ? "" : (m_selectedFolder == "" ? "<ROOT>" : m_selectedFolder) + "/")}{m_selectedValue}";
                case State.GettingFolder:
                    return $"{(m_isLocal ? "folder" : "assetFolder")}://{(m_selectedFolder == "" ? "<ROOT>" : m_selectedFolder)}";
                case State.GettingText:
                    return m_isVariable ? $"var://{m_selectedValue}" : m_searchText;
            }
            return null;
        }
        private void GoToPicker(int key, TextType targetContentType, TextParameterWrapper paramVal, OnNetInstanceCacheContainerXml item)
        {
            switch (targetContentType)
            {
                case TextType.ParameterizedGameSpriteIndexed:
                    m_isLocal = paramVal.IsLocal;
                    m_selectedFolder = paramVal.AtlasName.TrimToNull();
                    m_selectedValue = paramVal.TextOrSpriteValue;
                    m_currentState = State.GettingImage;
                    break;
                case TextType.ParameterizedGameSprite:
                    m_isLocal = paramVal.IsLocal;
                    m_selectedFolder = paramVal.AtlasName.TrimToNull();
                    m_currentState = State.GettingFolder;
                    break;
                case TextType.ParameterizedText:
                    m_isVariable = paramVal.ParamType == TextParameterWrapper.ParameterType.VARIABLE;
                    m_currentState = State.GettingText;
                    if (m_isVariable)
                    {
                        m_searchText = "";
                        m_selectedValue = paramVal.GetOriginalVariableParam();
                    }
                    else
                    {
                        m_searchText = paramVal.TextOrSpriteValue;
                        m_selectedValue = null;
                    }
                    break;
            }
            m_searchText = "";
            m_searchPropName = item.Descriptor?.PropName;
            m_searchResult.Value = new string[0];
            RestartFilterCoroutine();
            m_currentEditingParam = key;
        }
        private void GetBackToList(OnNetInstanceCacheContainerXml item)
        {
            if (m_currentState == State.GettingText && m_isVariable)
            {
                var cl = CommandLevel.OnFilterParamByText(GetCurrentParamString(), out _);
                if (m_searchResult.Value.Contains(m_searchText))
                {
                    m_selectedValue = CommandLevel.FromParameterPath(CommandLevel.GetParameterPath(m_selectedValue ?? "").Take(cl.level).Concat(new[] { m_searchText }));
                }
                if (m_varListHover > 0 && m_varListHover < m_searchResult.Value.Length)
                {
                    m_selectedValue = CommandLevel.FromParameterPath(CommandLevel.GetParameterPath(m_selectedValue ?? "").Take(cl.level).Concat(new[] { m_searchResult.Value[m_varListHover] }));
                }
            }
            item.SetTextParameter(m_currentEditingParam, GetCurrentParamString());
            m_currentState = State.List;
        }

        private void RestartFilterCoroutine()
        {
            if (m_searchCoroutine != null)
            {
                WriteTheSignsMod.Controller.StopCoroutine(m_searchCoroutine);
            }
            m_searchCoroutine = WriteTheSignsMod.Controller.StartCoroutine(OnFilterParam());
        }

        private IEnumerator OnFilterParam()
        {
            yield return 0;
            if (m_currentState == State.GettingImage || m_currentState == State.GettingFolder)
            {
                yield return m_searchResult.Value = OnFilterParamImagesAndFolders();
            }
            else if (m_currentState == State.GettingText)
            {
                yield return m_searchResult.Value = new[] { "<color=#FFFF00><<</color>" }.Concat(OnFilterParamVariable()?.Select(x => x.IsNullOrWhiteSpace() ? v_emptyPlaceholderOption : x) ?? new string[0]).ToArray();
            }
        }
        #endregion

        #region Image
        private void DrawTopImage(ref Rect areaRect)
        {
            bool dirtyType = false;
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_imageProtocolSelect);
                var modelType = GUILayout.SelectionGrid(m_isLocal ? 1 : 0, v_protocolsImg, v_protocolsImg.Length);
                dirtyType = m_isLocal != (modelType == 1);
                if (dirtyType)
                {
                    m_isLocal = modelType == 1;
                    m_selectedFolder = null;
                }
            }, GUILayout.Width(areaRect.width));

            bool dirtyInput = false;
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_imageSearchText);
                var newInput = GUILayout.TextField(m_searchText);
                dirtyInput = newInput != m_searchText;
                if (dirtyInput)
                {
                    m_searchText = newInput;
                }
            }, GUILayout.Width(areaRect.width));

            if (dirtyInput || dirtyType)
            {
                RestartFilterCoroutine();
            }
        }
        private void DrawLeftPanelImage(ref Rect areaRect)
        {
            var selectLayout = GUILayout.SelectionGrid(Array.IndexOf(m_searchResult.Value, m_selectedValue), m_searchResult.Value, 1, new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            }, GUILayout.Width((areaRect.width / 2) - 25));
            if (selectLayout >= 0)
            {
                OnSelectItemImage(selectLayout);
            }

        }
        private void DrawRightPanelImage() => GUILayout.Label(m_currentFolderAtlas?.sprites.Where(x => x.name == m_selectedValue).FirstOrDefault()?.texture, new GUIStyle() { }, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
        private void OnClearImage()
        {
            m_selectedFolder = null;
            m_selectedValue = null;
            m_searchResult.Value = new string[0];
            RestartFilterCoroutine();
        }
        private void OnSelectItemImage(int selectLayout)
        {
            if (m_selectedFolder is null)
            {
                m_selectedFolder = (m_searchResult.Value[selectLayout] == "<ROOT>" ? "" : m_searchResult.Value[selectLayout]);
                m_searchResult.Value = new string[0];
                RestartFilterCoroutine();
            }
            else
            {
                m_selectedValue = m_searchResult.Value[selectLayout];
            }
        }

        private string[] OnFilterParamImagesAndFolders()
        {
            switch (m_currentState)
            {
                case State.GettingImage:
                    if (m_selectedFolder is null && m_isLocal)
                    {
                        goto case State.GettingFolder;
                    }
                    return m_isLocal
                        ? WriteTheSignsMod.Controller.AtlasesLibrary.FindByInLocalSimple(m_selectedFolder == "<ROOT>" ? null : m_selectedFolder, m_searchText, out m_currentFolderAtlas)
                        : WriteTheSignsMod.Controller.AtlasesLibrary.FindByInAssetSimple(ulong.TryParse(m_searchPropName.Split('.')[0] ?? "", out ulong wId) ? wId : 0u, m_searchText, out m_currentFolderAtlas);
                case State.GettingFolder:
                    return m_isLocal
                        ? WriteTheSignsMod.Controller.AtlasesLibrary.FindByInLocalFolders(m_searchText)
                        : WriteTheSignsMod.Controller.AtlasesLibrary.HasAtlas(ulong.TryParse(m_searchPropName?.Split('.')[0] ?? "", out ulong wId2) ? wId2 : 0) ? new string[] { "<ROOT>" } : new string[0];
            }
            return null;
        }
        #endregion

        #region Folder

        private void DrawTopFolder(ref Rect areaRect)
        {
            bool dirtyType = false;
            if (ulong.TryParse(m_searchPropName?.Split('.')[0] ?? "", out ulong wId2))
            {
                GUIKlyteCommons.DoInHorizontal(() =>
                {
                    GUI.SetNextControlName(f_imageProtocolSelect);
                    var modelType = GUILayout.SelectionGrid(m_isLocal ? 1 : 0, v_protocolsFld, v_protocolsFld.Length);
                    dirtyType = m_isLocal != (modelType == 1);
                    if (dirtyType)
                    {
                        m_isLocal = modelType == 1;
                        m_selectedFolder = null;
                    }
                }, GUILayout.Width(areaRect.width));
            }
            else
            {
                dirtyType = !m_isLocal;
                m_isLocal = true;
            }

            bool dirtyInput = false;
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_imageSearchText);
                var newInput = GUILayout.TextField(m_searchText);
                dirtyInput = newInput != m_searchText;
                if (dirtyInput)
                {
                    m_searchText = newInput;
                }
            }, GUILayout.Width(areaRect.width));

            if (dirtyInput || dirtyType)
            {
                RestartFilterCoroutine();
            }
        }

        private void DrawLeftPanelFolder(ref Rect areaRect)
        {
            var selectLayout = GUILayout.SelectionGrid(Array.IndexOf(m_searchResult.Value, m_selectedValue), m_searchResult.Value, 1, new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            }, GUILayout.Width((areaRect.width / 2) - 25));
            if (selectLayout >= 0)
            {
                OnSelectItemFolder(selectLayout);
            }

        }
        private void DrawRightPanelFolder()
        {
            if (m_currentFolderAtlas != null)
            {
                GUILayout.Label(string.Join("\n", m_currentFolderAtlas.spriteNames.OrderBy(x => x).Select(x => $"\u2022 {x}").ToArray()), new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                });
            }
        }

        private void OnClearFolder()
        {
            m_selectedFolder = null;
            m_selectedValue = null;
            m_searchResult.Value = new string[0];
            RestartFilterCoroutine();
        }
        private void OnSelectItemFolder(int selectLayout)
        {
            m_selectedFolder = (m_searchResult.Value[selectLayout] == "<ROOT>" ? "" : m_searchResult.Value[selectLayout]);
            if (m_isLocal)
            {
                WriteTheSignsMod.Controller.AtlasesLibrary.GetAtlas(m_selectedFolder, out m_currentFolderAtlas);
            }
            else if (ulong.TryParse(m_searchPropName?.Split('.')[0] ?? "", out ulong wId))
            {
                m_selectedFolder = "";
                WriteTheSignsMod.Controller.AtlasesLibrary.GetAtlas(wId.ToString(), out m_currentFolderAtlas);
            }
        }

        #endregion

        #region Variable
        private void DrawTopVariable(ref Rect areaRect)
        {
            bool dirtyType = false;
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_imageProtocolSelect);
                var varType = GUILayout.SelectionGrid(m_isVariable ? 1 : 0, v_protocolsTxt, v_protocolsTxt.Length);
                dirtyType = m_isVariable != (varType == 1);
                if (dirtyType)
                {
                    m_isVariable = varType == 1;
                    m_selectedValue = null;
                }
            }, GUILayout.Width(areaRect.width));


            bool dirtyInput = false;
            GUIKlyteCommons.DoInHorizontal(() =>
            {
                GUI.SetNextControlName(f_imageSearchText);
                var newInput = GUILayout.TextField(m_searchText);
                dirtyInput = newInput != m_searchText;
                if (dirtyInput)
                {
                    m_searchText = newInput;
                }
            }, GUILayout.Width(areaRect.width));

            if (m_isVariable && dirtyInput)
            {
                RestartFilterCoroutine();
            }
        }

        private void DrawLeftPanelVariable(ref Rect areaRect)
        {
            if (m_isVariable)
            {
                var selectOpt = GUILayout.SelectionGrid(-1, m_searchResult.Value, 1, new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft
                }, GUILayout.Width((areaRect.width / 2) - 25));
                if (selectOpt >= 0)
                {
                    OnSelectItemVariable(selectOpt);
                }
            }

        }

        private void DrawRightPanelVariable() => GUILayout.Label(m_variableDescription, new GUIStyle(GUI.skin.label) { richText = true });

        private void OnClearVariable()
        {
            m_selectedFolder = null;
            m_selectedValue = null;
            m_searchResult.Value = new string[0];
            m_searchText = "";
            if (m_isVariable)
            {
                RestartFilterCoroutine();
            }
        }

        private void OnSelectItemVariable(int selectOpt)
        {
            var cl = CommandLevel.OnFilterParamByText(GetCurrentParamString(), out _);
            if (selectOpt > 0)
            {
                if (cl.defaultValue is null || m_varListHover == selectOpt)
                {
                    var value = m_searchResult.Value[selectOpt];
                    var paramPath = CommandLevel.GetParameterPath(m_selectedValue ?? "");
                    m_selectedValue = CommandLevel.FromParameterPath(paramPath.Take(cl.level).Concat(new[] { value == v_emptyPlaceholderOption ? "" : value }));
                    m_searchResult.Value = new string[0];
                    m_searchText = "";
                    m_varListHover = -1;
                    RestartFilterCoroutine();
                }
                else
                {
                    m_varListHover = selectOpt;
                    var str = m_searchResult.Value[m_varListHover];
                    var key = cl.nextLevelOptions.Where(z => z.Key.ToString() == str).FirstOrDefault().Key;
                    m_variableDescription = (key is null ? "" : $"<color=#00FF00>{key}</color>\n\n" + Locale.Get("K45_WTS_PARAMVARS_DESC", CommandLevel.ToLocaleVar(key)));
                }
            }
            else if (selectOpt == 0 && cl.level > 0)
            {
                var paramPath = CommandLevel.GetParameterPath(m_selectedValue ?? "");
                m_selectedValue = CommandLevel.FromParameterPath(paramPath.Take(cl.level - 1));
                m_searchResult.Value = new string[0];
                m_searchText = cl.defaultValue is null ? paramPath[cl.level] : "";
                m_varListHover = -1;
                RestartFilterCoroutine();
            }
        }

        private string[] OnFilterParamVariable()
        {
            var cmdResult = CommandLevel.OnFilterParamByText(GetCurrentParamString(), out string currentDescription);
            if (cmdResult is null)
            {
                return null;
            }
            else
            {
                m_variableDescription =
                    (cmdResult.regexValidValues.IsNullOrWhiteSpace() ? "" : $"Regex: <color=#FFFF00>{cmdResult.regexValidValues}</color>\n")
                    + Locale.Get("K45_WTS_PARAMVARS_DESC", currentDescription);
                if (cmdResult.regexValidValues != null)
                {
                    return Regex.IsMatch(m_searchText, cmdResult.regexValidValues) ? (new[] { Regex.Replace(m_searchText, @"([^\\])/|^/", "$1\\/") }) : (new string[0]);
                }
                else
                {
                    return cmdResult.nextLevelOptions?.Select(x => x.Key.ToString()).Where(x => x.ToLower().Contains(m_searchText)).OrderBy(x => x).ToArray();
                }
            }
        }
        #endregion

        #region Param editor fields
        private readonly Wrapper<string[]> m_searchResult = new Wrapper<string[]>();
        private string m_searchText;
        private string m_selectedFolder;
        private string m_searchPropName;
        private string m_selectedValue;
        private string m_variableDescription;
        private int m_varListHover;
        private UITextureAtlas m_currentFolderAtlas;
        private Coroutine m_searchCoroutine;
        private bool m_isLocal = false;
        private bool m_isVariable = false;
        #endregion


        #region Private definitions
        private delegate void DrawInAreaFunc(ref Rect area);

        private enum State
        {
            List,
            GettingImage,
            GettingFolder,
            GettingText
        }
        #endregion

    }
}
