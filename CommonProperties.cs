using Klyte.DynamicTextBoards;
using Klyte.DynamicTextBoards.Utils;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        public static bool DebugMode => DynamicTextBoardsMod.DebugMode;
        public static string Version => DynamicTextBoardsMod.Version;
        public static string ModName => DynamicTextBoardsMod.Instance.SimpleName;
        public static string ResourceBasePath => DTBResourceLoader.instance.Prefix;
        public static string Acronym => "DTB";
    }
}