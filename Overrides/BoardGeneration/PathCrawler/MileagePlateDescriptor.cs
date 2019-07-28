using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwayMileage
    {
        public struct MileagePlateDescriptor
        {
            public ushort segmentId;
            public int kilometer;
            public Vector3 position;
            public float rotation;
            public byte cardinalDirection8;
        }

    }
}
