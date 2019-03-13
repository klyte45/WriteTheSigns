using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.DynamicTextBoards.Utils;
using Klyte.Commons.Extensors;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Klyte.DynamicTextBoards.Overrides;
using Klyte.DynamicTextBoards.Tools;
using static Klyte.DynamicTextBoards.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextBoards.UI
{

    internal class DTBHighwaySignTab : UICustomControl
    {
        public UIComponent mainContainer { get; private set; }

        private UIHelperExtension m_uiHelperHS;

        private UIButton m_buttonTool;

        private UIHelperExtension m_subPanelSelection;
        private UILabel m_selectionAddress;
        private UIDropDown m_propsDropdown;
        private UISlider m_segmentPosition;
        private UICheckBox m_invertOrientation;
        private UITextField[] m_posVectorEditor;
        private UITextField[] m_rotVectorEditor;
        private UITextField[] m_scaleVectorEditor;

        public ushort m_currentSelectedSegment;

        private bool m_isLoading = false;

        #region Awake
        private void Awake()
        {
            mainContainer = GetComponent<UIComponent>();

            m_uiHelperHS = new UIHelperExtension(mainContainer);

            m_buttonTool = (UIButton)m_uiHelperHS.AddButton(Locale.Get("DTB_PICK_A_SEGMENT"), EnablePickTool);

            m_subPanelSelection = m_uiHelperHS.AddGroupExtended(Locale.Get("DTB_PICKED_SEGMENT_DATA"));
            m_selectionAddress = m_subPanelSelection.AddLabel("");
            m_selectionAddress.prefix = Locale.Get("DTB_ADDRESS_LABEL_PREFIX") + " ";
            DTBUtils.LimitWidth(m_selectionAddress, m_subPanelSelection.self.width, true);

            m_propsDropdown = (UIDropDown)m_subPanelSelection.AddDropdown(Locale.Get("DTB_PROP_MODEL_SELECT"), new string[0], 0, SetPropModel);
            m_propsDropdown.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            m_propsDropdown.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            DTBUtils.LimitWidth(m_propsDropdown.parent.GetComponentInChildren<UILabel>(), m_subPanelSelection.self.width / 2, true);
            m_segmentPosition = (UISlider)m_subPanelSelection.AddSlider(Locale.Get("DTB_SEGMENT_RELATIVE_POS"), 0, 1, 0.01f, 0.5f, SetPropSegPosition);
            m_segmentPosition.GetComponentInParent<UIPanel>().autoLayoutDirection = LayoutDirection.Horizontal;
            m_segmentPosition.GetComponentInParent<UIPanel>().autoFitChildrenVertically = true;
            DTBUtils.LimitWidth(m_segmentPosition.parent.GetComponentInChildren<UILabel>(), m_subPanelSelection.self.width / 2, true);
            m_invertOrientation = (UICheckBox)m_subPanelSelection.AddCheckbox(Locale.Get("DTB_INVERT_SIGN_SIDE"), false, SetInvertSignSide);

            m_posVectorEditor = m_subPanelSelection.AddVector3Field(Locale.Get("DTB_RELATIVE_POS"), Vector3.zero, SetPropRelPosition);
            DTBUtils.LimitWidth(m_posVectorEditor[0].parent.GetComponentInChildren<UILabel>(), m_subPanelSelection.self.width / 2, true);
            m_rotVectorEditor = m_subPanelSelection.AddVector3Field(Locale.Get("DTB_RELATIVE_ROT"), Vector3.zero, SetPropRelRotation);
            DTBUtils.LimitWidth(m_rotVectorEditor[0].parent.GetComponentInChildren<UILabel>(), m_subPanelSelection.self.width / 2, true);
            m_scaleVectorEditor = m_subPanelSelection.AddVector3Field(Locale.Get("DTB_RELATIVE_SCALE"), Vector3.up, SetPropRelScale);
            DTBUtils.LimitWidth(m_scaleVectorEditor[0].parent.GetComponentInChildren<UILabel>(), m_subPanelSelection.self.width / 2, true);

            OnSegmentSet(0);
        }

        private void Start()
        {
            m_propsDropdown.items = BoardGeneratorHighwaySigns.instance.LoadedProps.Select(x => Locale.Get("PROPS_TITLE", x)).ToArray();
        }

        private void SetPropModel(int idx)
        {
            if (idx > 0) SafeActionInBoard(descriptor => descriptor.m_propName = BoardGeneratorHighwaySigns.instance.LoadedProps[idx]);
        }

        private void SetPropSegPosition(float value)
        {
            SafeActionInBoard(descriptor => descriptor.m_segmentPosition = value);
        }
        private void SetPropRelPosition(Vector3 value)
        {
            SafeActionInBoard(descriptor => descriptor.m_propPosition = value);
        }
        private void SetPropRelRotation(Vector3 value)
        {
            SafeActionInBoard(descriptor => descriptor.m_propRotation = value);
        }
        private void SetPropRelScale(Vector3 value)
        {
            SafeActionInBoard(descriptor => descriptor.PropScale = value);
        }
        private void SetInvertSignSide(bool value)
        {
            SafeActionInBoard(descriptor => descriptor.m_invertSign = value);
        }
        private void SafeActionInBoard(Action<BoardDescriptorHigwaySign> toDo)
        {
            if (m_currentSelectedSegment != 0)
            {
                EnsureBoardsArraySize(0);
                BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].cached = false;
                var descriptor = BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData[0].descriptor;
                if (!m_isLoading) toDo(descriptor);
            }
        }

        private void EnsureBoardsArraySize(int idx)
        {
            if (BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment] == null)
            {
                BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment] = new BoardGeneratorHighwaySigns.BoardBunchContainerHighwaySign();
            }
            if (BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData == null || BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData.Length < 1)
            {
                BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData = new BoardGeneratorHighwaySigns.CacheControlHighwaySign[1];
            }
            if (BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx] == null)
            {
                BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx] = new BoardGeneratorHighwaySigns.CacheControlHighwaySign();
            }
            if (BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor == null)
            {
                BoardGeneratorHighwaySigns.instance.m_boardsContainers[m_currentSelectedSegment].m_boardsData[idx].descriptor = new BoardGeneratorHighwaySigns.BoardDescriptorHigwaySign();
            }
        }

        private void EnablePickTool()
        {
            OnSegmentSet(0);
            DynamicTextBoardsMod.instance.controller.RoadSegmentToolInstance.OnSelectSegment += OnSegmentSet;
            DynamicTextBoardsMod.instance.controller.RoadSegmentToolInstance.enabled = true;
        }

        private void OnSegmentSet(ushort segmentId)
        {
            m_propsDropdown.selectedIndex = -1;
            m_subPanelSelection.self.isVisible = segmentId > 0;
            if (segmentId > 0)
            {
                m_isLoading = true;
                var endNodeNum = DTBUtils.GetNumberAt(segmentId, false);
                var startNodeNum = DTBUtils.GetNumberAt(segmentId, true);
                m_selectionAddress.text = $"{NetManager.instance.GetSegmentName(segmentId)}, {Mathf.Min(startNodeNum, endNodeNum)} - {Mathf.Max(startNodeNum, endNodeNum)}";

                m_propsDropdown.selectedIndex = BoardGeneratorHighwaySigns.instance.LoadedProps.IndexOf(BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.m_propName);
                m_segmentPosition.value = BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.m_segmentPosition ?? 0.5f;
                m_invertOrientation.isChecked = BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.m_invertSign ?? false;
                m_posVectorEditor[0].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.PropPositionX ?? 0).ToString();
                m_posVectorEditor[1].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.PropPositionY ?? 0).ToString();
                m_posVectorEditor[2].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.PropPositionZ ?? 0).ToString();
                m_rotVectorEditor[0].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.PropRotationX ?? 0).ToString();
                m_rotVectorEditor[1].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.PropRotationY ?? 0).ToString();
                m_rotVectorEditor[2].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.PropRotationZ ?? 0).ToString();
                m_scaleVectorEditor[0].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.ScaleX ?? 1).ToString();
                m_scaleVectorEditor[1].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.ScaleY ?? 1).ToString();
                m_scaleVectorEditor[2].text = (BoardGeneratorHighwaySigns.instance.m_boardsContainers[segmentId]?.m_boardsData?.ElementAtOrDefault(0)?.descriptor?.ScaleZ ?? 1).ToString();

                m_isLoading = false;
            }
            m_currentSelectedSegment = segmentId;
        }

        private void CreateGroupFileSelect(string i18n, OnDropdownSelectionChanged onChanged, out UIDropDown dropDown)
        {
            dropDown = m_uiHelperHS.AddDropdownLocalized(i18n, new String[0], -1, onChanged);
            dropDown.width = 370;
            m_uiHelperHS.AddSpace(20);
        }

        #endregion

    }


}
