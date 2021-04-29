using Klyte.WriteTheSigns;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        internal static readonly string[] AssetExtraDirectoryNames = new string[] {
            WTSController.EXTRA_SPRITES_FILES_FOLDER_ASSETS
        };
        internal static readonly string[] AssetExtraFileNames = new string[] {
            WTSController.m_defaultFileNameBuildingsXml+".xml",
            WTSController.m_defaultFileNamePropsXml+".xml",
            WTSController.m_defaultFileNameVehiclesXml+".xml"
        };

        public static bool DebugMode => WriteTheSignsMod.DebugMode;
        public static string Version => WriteTheSignsMod.Version;
        public static string ModName => WriteTheSignsMod.Instance.SimpleName;
        public static string Acronym { get; } = "WTS";
        public static string ModRootFolder { get; } = WTSController.FOLDER_NAME;
        public static string ModDllRootFolder { get; } = WriteTheSignsMod.RootFolder;
        public static string ModIcon => WriteTheSignsMod.Instance.IconName;
    }
}