extern alias ADR;
using UnityEngine;
using ADR::Klyte.Addresses.ModShared;

namespace Klyte.WriteTheSigns.Connectors
{
    internal class ConnectorADR : IConnectorADR
    {
        public void Awake()
        {
           AdrShared.EventZeroMarkerBuildingChange += WTSController.OnZeroMarkChanged;
           AdrShared.EventRoadNamingChange += WTSController.OnZeroMarkChanged;
           AdrShared.EventDistrictColorChanged += WTSController.OnDistrictChanged;
           AdrShared.EventBuildingNameStrategyChanged += WTSController.OnBuildingNameChanged;
        }

        public override Color GetDistrictColor(ushort districtId) =>AdrShared.GetDistrictColor(districtId);
        public override Vector2 GetStartPoint() =>AdrShared.GetStartPoint();
        public override string GetStreetFullName(ushort idx) =>AdrShared.GetStreetFull(idx);
        public override string GetStreetPostalCode(Vector3 position, ushort idx) => AdrShared.GetPostalCode(position);
        public override string GetStreetQualifier(ushort idx) =>AdrShared.GetStreetQualifier(idx);
        public override string GetStreetSuffix(ushort idx) =>AdrShared.GetStreetSuffix(idx);
        public override string GetStreetSuffixCustom(ushort idx) =>AdrShared.GetStreetFull(idx);
    }
}

