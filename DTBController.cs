using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons;
using System;
using System.Diagnostics;
using UnityEngine;
using Klyte.DynamicTextBoards.Tools;
using UnityEngine.SceneManagement;

namespace Klyte.DynamicTextBoards
{

    public class DTBController : MonoBehaviour
    {
        public RoadSegmentTool RoadSegmentToolInstance { get; private set; }

        public void Awake()
        {
            RoadSegmentToolInstance = FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
        }

    }

}
