using Klyte.DynamicTextProps;
using Klyte.DynamicTextProps.Utils;

namespace Klyte.Commons
{
    public static class CommonProperties
    {
        public static bool DebugMode => DynamicTextPropsMod.DebugMode;
        public static string Version => DynamicTextPropsMod.Version;
        public static string ModName => DynamicTextPropsMod.Instance.SimpleName;
        public static string ResourceBasePath => DTPResourceLoader.instance.Prefix;
        public static string Acronym => "DTP";
    }
}