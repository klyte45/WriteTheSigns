using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Tools;
using UnityEngine;

namespace Klyte.DynamicTextProps
{

    public class DTPController : MonoBehaviour
    {
        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();
        public BuildingEditorTool BuildingEditorToolInstance => FindObjectOfType<BuildingEditorTool>();

        public DTPLibPropGroupHigwaySigns GroupInstance => DTPLibPropGroupHigwaySigns.Instance;

        public void Awake()
        {
            if (RoadSegmentToolInstance == null)
            {
                FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
            }

            if (BuildingEditorToolInstance == null)
            {
                FindObjectOfType<ToolController>().gameObject.AddComponent<BuildingEditorTool>();
            }
        }

    }

}
