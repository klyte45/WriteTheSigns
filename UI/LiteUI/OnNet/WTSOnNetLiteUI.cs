using ColossalFramework.Globalization;
using Klyte.Commons.LiteUI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Tools;
using Klyte.WriteTheSigns.Xml;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSOnNetLiteUI : GUIRootWindowBase
    {
        public static WTSOnNetLiteUI Instance { get; private set; }
        private GUIColorPicker colorPicker;


        public WTSOnNetLiteUI()
           : base("On Net Editor", new Rect(128, 128, 680, 420), resizable: true, minSize: new Vector2(340, 260))
        {
            Instance = this;
            xmlLibList.DeleteI18n = "K45_WTS_SEGMENT_CLEARDATA_AYS";
        }
        public void Start() => colorPicker = KlyteMonoUtils.CreateElement<GUIColorPicker>(gameObject.transform);
        public void Update()
        {
            if (Visible && Event.current.button == 1)
            {
                ToolsModifierControl.SetTool<SegmentEditorPickerTool>();
            }
        }
        internal override GUIColorPicker ColorPicker => colorPicker;

        private int tabSel = 0;
        private int listSel = -1;
        public int ListSel
        {
            get => listSel; set
            {
                listSel = value;
                xmlLibItem.ResetStatus();
                xmlLibList.ResetStatus();
                basicTab.Reset(ListSel >= 0 ? CurrentEditingInstance.BoardsData[ListSel] : null);
                targetTab.Reset();
                paramsTab.Reset();
                tabSel = 0;
            }
        }

        private Vector2 scrollPosition;

        private Texture[] TabsImages = new[] {
            GUIKlyteCommons.GetByNameFromDefaultAtlas("K45_Settings"),
            GUIKlyteCommons.GetByNameFromDefaultAtlas("InfoIconEscapeRoutes"),
            GUIKlyteCommons.GetByNameFromDefaultAtlas("K45_FontIcon")
        };

        private ushort currentSegmentId;
        private readonly WTSOnNetBasicTab basicTab = new WTSOnNetBasicTab();
        private readonly WTSOnNetTargetsTab targetTab = new WTSOnNetTargetsTab();
        private readonly WTSOnNetParamsTab paramsTab = new WTSOnNetParamsTab();
        private readonly GUIXmlLib<WTSLibOnNetPropLayout, BoardInstanceOnNetXml, OnNetInstanceCacheContainerXml> xmlLibItem = new GUIXmlLib<WTSLibOnNetPropLayout, BoardInstanceOnNetXml, OnNetInstanceCacheContainerXml>();
        private readonly GUIXmlLib<WTSLibOnNetPropLayoutList, ExportableBoardInstanceOnNetListXml> xmlLibList = new GUIXmlLib<WTSLibOnNetPropLayoutList, ExportableBoardInstanceOnNetListXml>();

        public static bool LockSelection { get; internal set; } = true;
        private OnNetGroupDescriptorXml CurrentEditingInstance { get; set; }
        public ushort CurrentSegmentId
        {
            get => currentSegmentId; set
            {
                currentSegmentId = value;
                Visible = value != 0;
                if (value != 0)
                {
                    ReloadSegment();
                }
            }
        }
        private void ReloadSegment()
        {
            WriteTheSignsMod.Controller.ConnectorADR.GetAddressStreetAndNumber(NetManager.instance.m_segments.m_buffer[CurrentSegmentId].m_middlePosition, NetManager.instance.m_segments.m_buffer[CurrentSegmentId].m_middlePosition, out int num, out string streetName);
            Title = $"{Locale.Get("K45_WTS_SEGMENTPLACING_TITLE")}: {streetName}, ~{num}m";
            if (WTSOnNetData.Instance.m_boardsContainers[CurrentSegmentId] == null)
            {
                WTSOnNetData.Instance.m_boardsContainers[CurrentSegmentId] = new OnNetGroupDescriptorXml();
            }
            CurrentEditingInstance = WTSOnNetData.Instance.m_boardsContainers[CurrentSegmentId];
            ListSel = -1;
        }
        private GUIStyle m_greenButton;
        private GUIStyle m_redButton;
        private GUIStyle GreenButton
        {
            get
            {
                if (m_greenButton is null)
                {
                    m_greenButton = new GUIStyle(Skin.button)
                    {
                        normal = new GUIStyleState()
                        {
                            background = GUIKlyteCommons.darkGreenTexture,
                            textColor = Color.white
                        },
                        hover = new GUIStyleState()
                        {
                            background = GUIKlyteCommons.greenTexture,
                            textColor = Color.black
                        },
                    };
                }
                return m_greenButton;
            }
        }
        private GUIStyle RedButton
        {
            get
            {
                if (m_redButton is null)
                {
                    m_redButton = new GUIStyle(Skin.button)
                    {
                        normal = new GUIStyleState()
                        {
                            background = GUIKlyteCommons.darkRedTexture,
                            textColor = Color.white
                        },
                        hover = new GUIStyleState()
                        {
                            background = GUIKlyteCommons.redTexture,
                            textColor = Color.white
                        },
                    };
                }
                return m_redButton;
            }
        }



        protected override void DrawWindow()
        {
            if (currentSegmentId == 0)
            {
                Visible = false;
                return;
            }
            if (xmlLibList.Status != FooterBarStatus.AskingToImport)
            {
                RegularDraw();
            }
            else
            {
                xmlLibList.DrawImportView(new Rect(5, 20, WindowRect.width - 10, WindowRect.height - 25), OnSelectBoardList);
            }
        }


        private void RegularDraw()
        {
            if (CurrentEditingInstance is null)
            {
                return;
            }
            var headerArea = new Rect(5, 20, WindowRect.width - 10, 20);
            if (xmlLibList.Status == FooterBarStatus.Normal)
            {
                GUIKlyteCommons.DoInArea(headerArea, (x) =>
                GUIKlyteCommons.DoInHorizontal(() =>
                    {
                        LockSelection = GUILayout.Toggle(LockSelection, Locale.Get("K45_WTS_SEGMENTEDITOR_BUTTONROWACTION_LOCKCAMERASELECTION"));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button(Locale.Get("K45_WTS_SEGMENT_IMPORTDATA")))
                        {
                            xmlLibList.GoToImport();
                        }
                        if (GUILayout.Button(Locale.Get("K45_WTS_SEGMENT_EXPORTDATA")))
                        {
                            xmlLibList.GoToExport();
                        }
                        if (GUILayout.Button(Locale.Get("K45_WTS_SEGMENT_CLEARDATA")))
                        {
                            xmlLibList.GoToRemove();
                        }

                    })
                );

            }
            else
            {
                xmlLibList.Draw(headerArea, RedButton, OnDeleteList, OnGetCurrentList);
            }

            GUIKlyteCommons.DoInArea(new Rect(0, 40, 120, WindowRect.height - 40),
                (x) => GUIKlyteCommons.DoInScroll(ref scrollPosition,
                () =>
                {
                    var newListSel = GUILayout.SelectionGrid(ListSel, CurrentEditingInstance.BoardsData.Select((y, i) => y.SaveName).ToArray(), 1, new GUIStyle(GUI.skin.button) { wordWrap = true });
                    if (ListSel != newListSel && newListSel >= 0 && newListSel < CurrentEditingInstance.BoardsData.Length)
                    {
                        ListSel = newListSel;
                    }
                    if (GUILayout.Button(Locale.Get("K45_WTS_SEGMENT_ADDITEMLIST"), GreenButton, GUILayout.ExpandWidth(true)))
                    {
                        CurrentEditingInstance.BoardsData = CurrentEditingInstance.BoardsData.Concat(new[] { new OnNetInstanceCacheContainerXml() { SaveName = "NEW" } }).ToArray();
                        ListSel = CurrentEditingInstance.BoardsData.Length - 1;
                    }
                })
                );

            if (ListSel >= 0 && ListSel < CurrentEditingInstance.BoardsData.Length)
            {
                var usedHeight = 0f;
                var extraHeight = 0f;
                if (xmlLibItem.Status != FooterBarStatus.AskingToImport && basicTab.ShowTabsOnTop() && paramsTab.ShowTabsOnTop())
                {
                    GUIKlyteCommons.DoInArea(new Rect(125, 40, WindowRect.width - 135, usedHeight += 40),
                        (x) =>
                         tabSel = GUILayout.SelectionGrid(tabSel, TabsImages, TabsImages.Length, new GUIStyle(Skin.button)
                         {
                             fixedWidth = 32,
                             fixedHeight = 32,
                         })
                    );
                }
                else
                {
                    if (!basicTab.ShowTabsOnTop())
                    {
                        tabSel = 0;
                        extraHeight = 30;
                    }
                    else if (!paramsTab.ShowTabsOnTop())
                    {
                        tabSel = 2;
                        extraHeight = 30;
                    }
                }
                var tabAreaRect = new Rect(125, usedHeight + 40, WindowRect.width - 130, WindowRect.height - usedHeight - 70 + extraHeight);
                GUIKlyteCommons.DoInArea(tabAreaRect, (x) =>
                {
                    if (xmlLibItem.Status == FooterBarStatus.AskingToImport)
                    {
                        xmlLibItem.DrawImportView(tabAreaRect, OnImportSingle);
                    }
                    else
                    {
                        switch (tabSel)
                        {
                            case 0:
                                basicTab.DrawArea(CurrentEditingInstance.BoardsData[ListSel], x);
                                break;
                            case 1:
                                targetTab.DrawArea(CurrentEditingInstance.BoardsData[ListSel], x);
                                break;
                            case 2:
                                paramsTab.DrawArea(CurrentEditingInstance.BoardsData[ListSel], x);
                                break;
                        }
                    }
                });
                if ((!basicTab.CanHaveParameter() || paramsTab.ShowTabsOnTop()) && basicTab.ShowFooter())
                {
                    xmlLibItem.Draw(new Rect(120, WindowRect.height - 30, WindowRect.width - 125, 25), RedButton, OnDelete, GetCurrent, xmlLibItem.FooterDraw);
                }
            }
        }

        private void OnSelectBoardList(ExportableBoardInstanceOnNetListXml obj)
        {
            CurrentEditingInstance.BoardsData = CurrentEditingInstance.BoardsData.Concat(obj.Instances.Select(x => XmlUtils.TransformViaXml<BoardInstanceOnNetXml, OnNetInstanceCacheContainerXml>(x)).Where(x => !(x?.SaveName is null))).ToArray();
            foreach (var x in obj.Layouts)
            {
                if (WTSPropLayoutData.Instance.Get(x.Key) is null)
                {
                    var value = XmlUtils.CloneViaXml(x.Value);
                    WTSPropLayoutData.Instance.Add(x.Key, value);
                }
            };
            ListSel = -1;
        }
        private ExportableBoardInstanceOnNetListXml OnGetCurrentList() => new ExportableBoardInstanceOnNetListXml
        {
            Instances = CurrentEditingInstance.BoardsData.Select((x) => XmlUtils.DefaultXmlDeserialize<BoardInstanceOnNetXml>(XmlUtils.DefaultXmlSerialize(x))).ToArray(),
            Layouts = CurrentEditingInstance.GetLocalLayouts()
        };

        private void OnDeleteList()
        {
            CurrentEditingInstance.BoardsData = new OnNetInstanceCacheContainerXml[0];
            ListSel = -1;
        }

        private void OnDelete()
        {
            CurrentEditingInstance.BoardsData = CurrentEditingInstance.BoardsData.Where((k, i) => i != ListSel).ToArray();
            ListSel = -1;
        }
        private OnNetInstanceCacheContainerXml GetCurrent() => CurrentEditingInstance.BoardsData[ListSel];

        private void OnImportSingle(OnNetInstanceCacheContainerXml data)
        {
            data.SaveName = CurrentEditingInstance.BoardsData[ListSel].SaveName;
            CurrentEditingInstance.BoardsData[ListSel] = data;
        }
    }
}
