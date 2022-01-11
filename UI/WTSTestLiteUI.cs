using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.LiteUI;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Xml;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSTestLiteUI : GUIRootWindowBase
    {
        private GUIColorPicker colorPicker;

        public void Start() => colorPicker = KlyteMonoUtils.CreateElement<GUIColorPicker>(gameObject.transform);
        internal override GUIColorPicker ColorPicker => colorPicker;



        private int tabSel = 0;
        private int listSel = -1;

        private Vector2 scrollPosition;

        private static Texture GetByName(string name) => UIView.GetAView().defaultAtlas.sprites.Where(x => x.name == name).FirstOrDefault().texture;
        private Texture[] TabsImages = new[] { GetByName("K45_Settings"), GetByName("K45_MoveCross"), GetByName("InfoIconEscapeRoutes"), GetByName("K45_FontIcon") };
        private ushort currentSegmentId;

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
            listSel = -1;
        }


        public WTSTestLiteUI()
           : base("TEST WTS", new Rect(128, 128, 680, 420), resizable: true, minSize: new Vector2(340, 260))
        {
        }
        protected override void DrawWindow()
        {
            if (currentSegmentId == 0)
            {
                Visible = false;
                return;
            }

            GUILayout.BeginArea(new Rect(0, 20, WindowRect.width, 20));
            GUILayout.BeginHorizontal();

            LockSelection = GUILayout.Toggle(LockSelection, Locale.Get("K45_WTS_SEGMENTEDITOR_BUTTONROWACTION_LOCKCAMERASELECTION"));

            GUILayout.EndHorizontal();
            GUILayout.EndArea();


            GUILayout.BeginArea(new Rect(0, 40, 100, WindowRect.height - 40));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            if (CurrentEditingInstance != null)
            {
                var newListSel = GUILayout.SelectionGrid(listSel, CurrentEditingInstance.BoardsData.Select((x, i) => x.SaveName).ToArray(), 1);
                if (listSel != newListSel)
                {
                    listSel = newListSel;
                    tabSel = 0;
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
                if (listSel >= 0 && listSel < CurrentEditingInstance.BoardsData.Length)
                {
                    GUILayout.BeginArea(new Rect(110, 40, WindowRect.width - 110, 40));
                    tabSel = GUILayout.SelectionGrid(tabSel, TabsImages, TabsImages.Length, new GUIStyle(Skin.button)
                    {
                        fixedWidth = 32,
                        fixedHeight = 32,
                    });
                    GUILayout.EndArea();
                    GUILayout.BeginArea(new Rect(110, 80, WindowRect.width - 110, WindowRect.height - 80));
                    switch (tabSel)
                    {
                        case 0:
                            DrawAreaSettings(CurrentEditingInstance.BoardsData[listSel]);
                            break;
                    }
                    GUILayout.EndArea();
                }
            }
        }

        private const string f_base = "K45_WTS_OnNetInstanceCacheContainerXml_";
        private const string f_SaveName = f_base + "SaveName";

        private void DrawAreaSettings(OnNetInstanceCacheContainerXml item)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(Locale.Get("K45_WTS_ONNETEDITOR_NAME"));
            
            var newName = GUITextField.TextField(f_base, item.SaveName);
            if (!newName.IsNullOrWhiteSpace() && newName != item.SaveName)
            {
                item.SaveName = newName;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label($"Last sel IDX: {listSel}=>{tabSel}");
        }

    }
}
