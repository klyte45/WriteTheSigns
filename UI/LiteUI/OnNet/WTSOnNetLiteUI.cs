using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.LiteUI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Libraries;
using Klyte.WriteTheSigns.Xml;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSOnNetLiteUI : GUIRootWindowBase
    {
        private GUIColorPicker colorPicker;


        public void Start() => colorPicker = KlyteMonoUtils.CreateElement<GUIColorPicker>(gameObject.transform);
        internal override GUIColorPicker ColorPicker => colorPicker;

        private int tabSel = 0;
        private int listSel = -1;
        public int ListSel
        {
            get => listSel; set
            {
                listSel = value;
                xmlLib.ResetStatus();
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
        private readonly GUIXmlLib<WTSLibOnNetPropLayout, BoardInstanceOnNetXml, OnNetInstanceCacheContainerXml> xmlLib = new GUIXmlLib<WTSLibOnNetPropLayout, BoardInstanceOnNetXml, OnNetInstanceCacheContainerXml>();

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
            Title = $"{streetName}, ~{num}m";
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


        public WTSOnNetLiteUI()
           : base("On Net Editor", new Rect(128, 128, 680, 420), resizable: true, minSize: new Vector2(340, 260)) { }
        protected override void DrawWindow()
        {
            if (currentSegmentId == 0)
            {
                Visible = false;
                return;
            }

            GUIKlyteCommons.DoInArea(new Rect(5, 20, WindowRect.width - 10, 20), (x) => GUIKlyteCommons.DoInHorizontal(() => LockSelection = GUILayout.Toggle(LockSelection, Locale.Get("K45_WTS_SEGMENTEDITOR_BUTTONROWACTION_LOCKCAMERASELECTION"))));


            if (CurrentEditingInstance != null)
            {
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
                    if (xmlLib.Status != FooterBarStatus.AskingToImport && basicTab.ShowTabsOnTop() && paramsTab.ShowTabsOnTop())
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
                        if (xmlLib.Status == FooterBarStatus.AskingToImport)
                        {
                            xmlLib.DrawImportView(tabAreaRect, OnImport);
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
                    if (paramsTab.ShowTabsOnTop() && basicTab.ShowTabsOnTop())
                    {
                        xmlLib.DrawFooter(new Rect(120, WindowRect.height - 30, WindowRect.width - 125, 25), RedButton, OnDelete, GetCurrent);
                    }
                }
            }
        }

        private void OnDelete()
        {
            CurrentEditingInstance.BoardsData = CurrentEditingInstance.BoardsData.Where((k, i) => i != ListSel).ToArray();
            ListSel = -1;
        }
        private OnNetInstanceCacheContainerXml GetCurrent() => CurrentEditingInstance.BoardsData[ListSel];

        private void OnImport(OnNetInstanceCacheContainerXml data)
        {
            data.SaveName = CurrentEditingInstance.BoardsData[ListSel].SaveName;
            CurrentEditingInstance.BoardsData[ListSel] = data;
        }
    }
}
