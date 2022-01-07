using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;

namespace Klyte.WriteTheSigns.ModShared
{
    internal abstract class IBridgeADR : MonoBehaviour
    {
        public abstract bool AddressesAvailable { get; }
        public abstract Color GetDistrictColor(ushort districtId);
        public abstract Vector2 GetStartPoint();
        public virtual string GetStreetFullName(ushort idx) => NetManager.instance.GetSegmentName(idx);
        public abstract string GetStreetSuffix(ushort idx);
        public abstract string GetStreetQualifier(ushort idx);
        public abstract string GetStreetPostalCode(Vector3 position, ushort idx);
        public abstract bool GetAddressStreetAndNumber(Vector3 sidewalk, Vector3 midPosBuilding, out int number, out string streetName);
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
        public virtual string GetStreetQualifierCustom(ushort idx)
        {
            string result = GetStreetFullName(idx);
            if (result.Contains(" "))
            {
                switch (WTSRoadNodesData.Instance.RoadQualifierExtraction)
                {
                    case RoadQualifierExtractionMode.START:
                        result = result.Substring(0, result.IndexOf(' '));
                        break;
                    case RoadQualifierExtractionMode.END:
                        result = result.Substring(result.LastIndexOf(' ') + 1);
                        break;
                }
                return result;
            }
            return "";
        }

        public abstract AdrHighwayParameters GetHighwayData(ushort seedId);
        public abstract AdrHighwayParameters GetHighwayTypeData(string typeName);
        public abstract string[] ListAllAvailableHighwayTypes(string filterText);

        internal class AdrHighwayParameters
        {
            public string layoutName;
            public string detachedStr;
            public string hwIdentifier;
            public string shortCode;
            public string longCode;
            public Color hwColor;
            public int mileageOffset;
            public bool invertMileage;
        }
    }
}