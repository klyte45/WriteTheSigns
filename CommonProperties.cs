using Klyte.WriteTheSigns;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        public static bool DebugMode => WriteTheSignsMod.DebugMode;
        public static string Version => WriteTheSignsMod.Version;
        public static string ModName => WriteTheSignsMod.Instance.SimpleName;
        public static string Acronym { get; } = "WTS";
        public static string ModRootFolder     { get; } = WTSController.FOLDER_NAME;
        public static string ModDllRootFolder { get; } = WriteTheSignsMod.RootFolder;
        public static string ModIcon => WriteTheSignsMod.Instance.IconName;
    }
}