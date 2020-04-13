using Klyte.DynamicTextProps;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        public static bool DebugMode => DynamicTextPropsMod.DebugMode;
        public static string Version => DynamicTextPropsMod.Version;
        public static string ModName => DynamicTextPropsMod.Instance.SimpleName;
        public static string Acronym { get; } = "DTP";
        public static string ModRootFolder { get; } = DTPController.FOLDER_NAME;
        public static string ModIcon => DynamicTextPropsMod.Instance.IconName;
    }
}