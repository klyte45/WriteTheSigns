using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Tools;
using UnityEngine;

namespace Klyte.DynamicTextProps
{

    public class DTPController : MonoBehaviour
    {
        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();

        public DTPLibPropGroup GroupInstance => DTPLibPropGroup.Instance;

        public void Awake()
        {
            FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
            DTPLibPropGroup.Reload();
            DTPLibPropSingle.Reload();
            DTPLibTextMeshHighwaySigns.Reload();
        }

    }

}
