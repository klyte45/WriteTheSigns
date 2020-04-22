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

            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            DistrictManagerOverrides.EventOnDistrictChanged += OnDistrictChanged;

            var adrEventsType = Type.GetType("Klyte.Addresses.ModShared.AdrEvents, KlyteAddresses");
            if (adrEventsType != null)
            {
                static void RegisterEvent(string eventName, Type adrEventsType, Action action) => adrEventsType.GetEvent(eventName)?.AddEventHandler(null, action);
                RegisterEvent("EventRoadNamingChange", adrEventsType, new Action(OnNameSeedChanged));
                RegisterEvent("EventDistrictColorChanged", adrEventsType, new Action(OnDistrictChanged));
            }
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
        #endregion


    }


}
