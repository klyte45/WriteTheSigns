using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using System.Collections.Generic;

namespace Klyte.Commons.Utils
{
    internal class UITemplateUtils
    {
        public static Dictionary<string, UIComponent> GetTemplateDict() => (Dictionary<string, UIComponent>) typeof(UITemplateManager).GetField("m_Templates", RedirectorUtils.allFlags).GetValue(UITemplateManager.instance);
    }

}

