extern alias ADR;
using ADR::Klyte.Addresses.ModShared;
using UnityEngine;

namespace Klyte.WriteTheSigns.ModShared
{
    internal class BridgeADR : IBridgeADR
    {
        public void Start()
        {
            AdrFacade.Instance.EventZeroMarkerBuildingChange += WTSController.OnZeroMarkChanged;
            AdrFacade.Instance.EventRoadNamingChange += WTSController.OnZeroMarkChanged;
            AdrFacade.Instance.EventDistrictChanged += WTSController.OnDistrictChanged;
            AdrFacade.Instance.EventBuildingNameStrategyChanged += () => WTSController.OnBuildingNameChanged(null);
            AdrFacade.Instance.EventPostalCodeChanged += WTSController.OnPostalCodeChanged;
        }

        public override bool GetAddressStreetAndNumber(Vector3 sidewalk, Vector3 midPosBuilding, out int number, out string streetName) => AdrFacade.GetStreetAndNumber(sidewalk, midPosBuilding, out streetName, out number);
        public override Color GetDistrictColor(ushort districtId) => AdrFacade.GetDistrictColor(districtId);
        public override Vector2 GetStartPoint() => AdrFacade.GetStartPoint();
        public override string GetStreetPostalCode(Vector3 position, ushort idx) => AdrFacade.GetPostalCode(position);
        public override string GetStreetQualifier(ushort idx) => AdrFacade.GetStreetQualifier(idx);
        public override string GetStreetSuffix(ushort idx) => AdrFacade.GetStreetSuffix(idx);
    }
}

