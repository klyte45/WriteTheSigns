using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Singleton;
using System.Reflection;
using static Klyte.Commons.Extensors.RedirectorUtils;
namespace Klyte.WriteTheSigns.Overrides
{
    public class TransportLineOverrides : Redirector, IRedirectable
    {
        public void Awake()
        {
            #region Automation Hooks
            MethodInfo stopChanged = typeof(TransportLineOverrides).GetMethod("DoAutomation", allFlags);

            LogUtils.DoLog("Loading AutoColor & AutoName Hook");
            AddRedirect(typeof(TransportLine).GetMethod("AddStop", allFlags), null, stopChanged);
            #endregion
        }

        public static void DoAutomation() => WTSBuildingPropsSingleton.instance.MarkLinesDirty();

    }
}
