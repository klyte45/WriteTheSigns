using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;

namespace Klyte.WriteTheSigns.Connectors
{
    internal abstract class IConnectorADR : MonoBehaviour
    {
        public abstract Color GetDistrictColor(ushort districtId);
        public abstract Vector2 GetStartPoint();
        public abstract string GetStreetFullName(ushort idx);
        public abstract string GetStreetSuffix(ushort idx);
        public abstract string GetStreetQualifier(ushort idx);
        public abstract string GetStreetPostalCode(Vector3 position, ushort idx);
        public virtual string GetStreetSuffixCustom(ushort idx)
        {
            string result = GetStreetFullName(idx);
            if (result.Contains(" "))
            {
                switch (WTSRoadNodesData.Instance.RoadQualifierExtraction)
                {
                    case RoadQualifierExtractionMode.START:
                        result = result.Substring(result.IndexOf(' ') + 1);
                        break;
                    case RoadQualifierExtractionMode.END:
                        result = result.Substring(0, result.LastIndexOf(' '));
                        break;
                }
            }
            return result;
        }
    }
}