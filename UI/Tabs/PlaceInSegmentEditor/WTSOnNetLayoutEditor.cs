using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensions;
using Klyte.Commons.UI.SpriteNames;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Xml;
using System;
using UnityEngine;
using static Klyte.Commons.UI.DefaultEditorUILib;

namespace Klyte.WriteTheSigns.UI
{
    internal class WTSOnNetLayoutEditor : UICustomControl
    {
        private static WTSOnNetLayoutEditor m_instance;
        public static WTSOnNetLayoutEditor Instance
        {
            get
            {
                if (m_instance == null)
                {
                    m_instance = FindObjectOfType<WTSOnNetLayoutEditor>();
                }
                return m_instance;
            }
        }

        private OnNetGroupDescriptorXml CurrentEditingInstance { get; set; }



        internal event Action<OnNetGroupDescriptorXml> EventOnSegmentSelectionChanged;




        public UIPanel MainContainer { get; protected set; }

        private UIButton m_buttonTool;
        private UIPanel m_secondaryContainer;
        private UILabel m_labelSelectionDescription;
        private UIPanel m_containerSelectionDescription;
        private UIButton m_btnLock;

        public WTSOnNetLayoutEditorPropList LayoutList { get; private set; }
        public ushort CurrentSegmentId { get; private set; }
        public bool LockSelection { get; internal set; } = true;

        public void Awake()
        {

            MainContainer = GetComponent<UIPanel>();
            MainContainer.autoLayout = true;
            MainContainer.autoLayoutDirection = LayoutDirection.Vertical;
            MainContainer.autoLayoutPadding = new RectOffset(5, 5, 5, 5);

            var m_uiHelperHS = new UIHelperExtension(MainContainer);

            m_buttonTool = (UIButton)m_uiHelperHS.AddButton(Locale.Get("K45_WTS_PICK_A_SEGMENT"), EnablePickTool);
            KlyteMonoUtils.LimitWidth(m_buttonTool, (m_uiHelperHS.Self.width - 20), true);


            AddLabel("", m_uiHelperHS, out m_labelSelectionDescription, out m_containerSelectionDescription);
            KlyteMonoUtils.LimitWidthAndBox(m_labelSelectionDescription, (m_uiHelperHS.Self.width / 2), out UIPanel containerBoxDescription, true);
            m_labelSelectionDescription.prefix = Locale.Get("K45_WTS_CURRENTSELECTION") + ": ";
            m_labelSelectionDescription.minimumSize = new Vector3(0, 30);
            m_btnLock = AddButtonInEditorRow(containerBoxDescription, CommonsSpriteNames.K45_Lock, OnLockSelection, "K45_WTS_SEGMENTEDITOR_BUTTONROWACTION_LOCKCAMERASELECTION", false, 30);
            m_btnLock.color = LockSelection ? Color.red : Color.white;
            m_btnLock.focusedColor = LockSelection ? Color.red : Color.white;
            m_btnLock.pressedColor = LockSelection ? Color.red : Color.white;

            KlyteMonoUtils.CreateUIElement(out m_secondaryContainer, MainContainer.transform, "SecContainer", new Vector4(0, 0, MainContainer.width, 655));
            m_secondaryContainer.autoLayout = true;
            m_secondaryContainer.autoLayoutDirection = LayoutDirection.Horizontal;
            m_secondaryContainer.autoLayoutPadding = new RectOffset(0, 10, 0, 0);

            KlyteMonoUtils.CreateUIElement(out UIPanel tertiaryContainer, m_secondaryContainer.transform, "TrcContainer", new Vector4(0, 0, m_secondaryContainer.width * 0.25f, m_secondaryContainer.height));
            LayoutList = tertiaryContainer.gameObject.AddComponent<WTSOnNetLayoutEditorPropList>();

            KlyteMonoUtils.CreateUIElement(out UIPanel editorPanel, m_secondaryContainer.transform, "EditPanel", new Vector4(0, 0, m_secondaryContainer.width * 0.75f - 35, m_secondaryContainer.height));
            editorPanel.gameObject.AddComponent<WTSOnNetLayoutEditorPropDetail>();

            OnSegmentSet(0);
        }

        private void OnLockSelection()
        {
            LockSelection = !LockSelection;
            m_btnLock.normalFgSprite = KlyteResourceLoader.GetDefaultSpriteNameFor(LockSelection ? CommonsSpriteNames.K45_Lock : CommonsSpriteNames.K45_Unlock);
            m_btnLock.color = LockSelection ? Color.red : Color.white;
            m_btnLock.focusedColor = LockSelection ? Color.red : Color.white;
            m_btnLock.pressedColor = LockSelection ? Color.red : Color.white;
        }

        private void EnablePickTool()
        {
            OnSegmentSet(0);
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.OnSelectSegment += OnSegmentSet;
            WriteTheSignsMod.Controller.RoadSegmentToolInstance.enabled = true;
        }

        private void OnSegmentSet(ushort id)
        {
            CurrentSegmentId = id;
            ReloadSegment();
        }


        private void ReloadSegment()
        {
            m_secondaryContainer.isVisible = CurrentSegmentId != 0;
            m_containerSelectionDescription.isVisible = CurrentSegmentId != 0;
            SegmentUtils.GetAddressStreetAndNumber(NetManager.instance.m_segments.m_buffer[CurrentSegmentId].m_middlePosition, NetManager.instance.m_segments.m_buffer[CurrentSegmentId].m_middlePosition, out int num, out string streetName);
            m_labelSelectionDescription.text = $"{streetName}, ~{num}m";
            if (WTSOnNetData.Instance.m_boardsContainers[CurrentSegmentId] == null)
            {
                WTSOnNetData.Instance.m_boardsContainers[CurrentSegmentId] = new OnNetGroupDescriptorXml();
            }
            CurrentEditingInstance = WTSOnNetData.Instance.m_boardsContainers[CurrentSegmentId];
            EventOnSegmentSelectionChanged?.Invoke(CurrentEditingInstance);
        }

    }

}
