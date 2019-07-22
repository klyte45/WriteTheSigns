using Klyte.DynamicTextBoards.Libraries;
using Klyte.DynamicTextBoards.Tools;
using UnityEngine;

namespace Klyte.DynamicTextBoards
{

    public class DTBController : MonoBehaviour
    {
        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();

        public DTBLibPropGroup GroupInstance => DTBLibPropGroup.Instance;

        public void Awake()
        {
            FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
            DTBLibPropGroup.Reload();
            DTBLibPropSingle.Reload();
            DTBLibTextMeshHighwaySigns.Reload();
        }

    }

}
