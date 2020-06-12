using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using System;

namespace Klyte.WriteTheSigns.Overrides
{

    public class RenderEventsRedirector : Redirector, IRedirectable
    {
        public void Awake()
        {


            var adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventRoadNamingChange", adrEventsType, new Action(OnNameSeedChanged));
                RegisterEvent("EventDistrictColorChanged", adrEventsType, new Action(OnDistrictChanged));
            }
        }

        public void Start()
        {
            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            WTSController.EventOnDistrictChanged += OnDistrictChanged;
            WTSController.EventOnParkChanged += OnParkChanged;
            BuildingManager.instance.EventBuildingRelocated += OnBuildingChange;
            BuildingManager.instance.EventBuildingReleased += OnBuildingChange;
            BuildingManager.instance.EventBuildingCreated += OnBuildingChange;
            WTSController.EventOnBuildingNameChanged += OnBuildingChange;
        }
        private void OnNameSeedChanged()
        {
            RenderUtils.ClearCacheFullStreetName();
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
        }

        #region events
        private void OnNameSeedChanged(ushort segmentId)
        {
            RenderUtils.ClearCacheFullStreetName();
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
        }
        private void OnDistrictChanged()
        {
            LogUtils.DoLog("onDistrictChanged");
            RenderUtils.ClearCacheDistrictName();
        }
        private void OnParkChanged()
        {
            LogUtils.DoLog("onDistrictChanged");
            RenderUtils.ClearCacheParkName();
        }
        private void OnBuildingChange(ushort buildingID)
        {
            LogUtils.DoLog("onBuildingChanged");
            RenderUtils.ClearCacheBuildingName(buildingID);
        }
        #endregion


    }


}
