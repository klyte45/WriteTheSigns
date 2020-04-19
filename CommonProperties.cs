using Klyte.WriteTheCity;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        public static bool DebugMode => WriteTheCityMod.DebugMode;
        public static string Version => WriteTheCityMod.Version;
        public static string ModName => WriteTheCityMod.Instance.SimpleName;
        public static string Acronym { get; } = "WTC";
        public static string ModRootFolder { get; } = WTCController.FOLDER_NAME;
        public static string ModDllRootFolder { get; } = WriteTheCityMod.RootFolder;
        public static string ModIcon => WriteTheCityMod.Instance.IconName;
    }
}