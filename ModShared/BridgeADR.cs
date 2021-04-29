extern alias ADR;
using ADR::Klyte.Addresses.ModShared;
using UnityEngine;

namespace Klyte.WriteTheSigns.ModShared
{
    internal class BridgeADR : IBridgeADR
    {
        public override bool AddressesAvailable { get; } = true;
        public void Start()
        {
            AdrFacade.Instance.EventZeroMarkerBuildingChange += WTSController.OnZeroMarkChanged;
            AdrFacade.Instance.EventRoadNamingChange += WTSController.OnZeroMarkChanged;
            AdrFacade.Instance.EventDistrictChanged += WTSController.OnDistrictChanged;
            AdrFacade.Instance.EventBuildingNameStrategyChanged += () => WTSController.OnBuildingNameChanged(null);
            AdrFacade.Instance.EventPostalCodeChanged += WTSController.OnPostalCodeChanged;
            AdrFacade.Instance.EventHighwaySeedChanged += (x) => WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields();
            AdrFacade.Instance.EventHighwaysChanged += WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.PurgeShields;
        }

        public override bool GetAddressStreetAndNumber(Vector3 sidewalk, Vector3 midPosBuilding, out int number, out string streetName) => AdrFacade.GetStreetAndNumber(sidewalk, midPosBuilding, out streetName, out number);
        public override Color GetDistrictColor(ushort districtId) => AdrFacade.GetDistrictColor(districtId);
        public override Vector2 GetStartPoint() => AdrFacade.GetStartPoint();
        public override string GetStreetPostalCode(Vector3 position, ushort idx) => AdrFacade.GetPostalCode(position);
        public override string GetStreetQualifier(ushort idx) => AdrFacade.GetStreetQualifier(idx);
        public override string GetStreetSuffix(ushort idx) => AdrFacade.GetStreetSuffix(idx);

        public override AdrHighwayParameters GetHighwayData(ushort seedId)
        {
            var result = new AdrHighwayParameters();
            return AdrFacade.GetSeedHighwayParameters(seedId, out result.layoutName, out result.detachedStr, out result.hwIdentifier, out result.shortCode, out result.longCode, out result.hwColor)
                ? result
                : null;
        }

        public override string[] ListAllAvailableHighwayTypes(string filterText) => AdrFacade.ListAllHighwayTypes(filterText);
        public override AdrHighwayParameters GetHighwayTypeData(string layoutName)
        {
            var result = new AdrHighwayParameters
            {
                hwIdentifier = "XXX"
            };
            return AdrFacade.GetHighwayTypeParameters(layoutName, out result.detachedStr, out result.shortCode, out result.longCode)
                ? result
                : null;
        }
    }
}

