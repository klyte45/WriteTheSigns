extern alias ADR;
using ADR::Klyte.Addresses.ModShared;
using UnityEngine;

namespace Klyte.WriteTheSigns.Connectors
{
    internal class ConnectorADR : IConnectorADR
    {
        public void Start()
        {
            AdrShared.Instance.EventZeroMarkerBuildingChange += WTSController.OnZeroMarkChanged;
            AdrShared.Instance.EventRoadNamingChange += WTSController.OnZeroMarkChanged;
            AdrShared.Instance.EventDistrictChanged += WTSController.OnDistrictChanged;
            AdrShared.Instance.EventBuildingNameStrategyChanged += WTSController.OnBuildingNameChanged;
            AdrShared.Instance.EventPostalCodeChanged += WTSController.OnPostalCodeChanged;
        }

        public override Color GetDistrictColor(ushort districtId) => AdrShared.GetDistrictColor(districtId);
        public override Vector2 GetStartPoint() => AdrShared.GetStartPoint();
        public override string GetStreetPostalCode(Vector3 position, ushort idx) => AdrShared.GetPostalCode(position);
        public override string GetStreetQualifier(ushort idx) => AdrShared.GetStreetQualifier(idx);
        public override string GetStreetSuffix(ushort idx) => AdrShared.GetStreetSuffix(idx);
    }
}

