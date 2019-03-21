using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons;
using System;
using System.Diagnostics;
using UnityEngine;
using Klyte.DynamicTextBoards.Tools;
using UnityEngine.SceneManagement;
using Klyte.DynamicTextBoards.Utils;
using Klyte.DynamicTextBoards.Libraries;

namespace Klyte.DynamicTextBoards
{

    public class DTBController : MonoBehaviour
    {
        public RoadSegmentTool RoadSegmentToolInstance { get; private set; }

        public DTBLibPropGroup GroupInstance => DTBLibPropGroup.Instance;

        public void Awake()
        {
            RoadSegmentToolInstance = FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
            DTBLibPropGroup.Reload();
            DTBLibPropSingle.Reload();
            DTBLibTextMesh.Reload();
        }

    }

}
