using Klyte.WriteTheSigns;
using UnityEngine;

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
        public static string GitHubRepoPath { get; } = "klyte45/WriteTheSigns";

        public static float UIScale { get; } = 1f;
        public static Color ModColor { get; } = new Color32(1, 79, 113, 255);
        public static MonoBehaviour Controller => WriteTheSignsMod.Controller;
    }
}