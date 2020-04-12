using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Overrides;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextProps.Utils
{
    internal class DTPUtils
    {

        public static void ReloadFontsOf<BG>(UIDropDown target) where BG : BoardGeneratorParent<BG>
        {
            LogUtils.DoLog($"Instance = {BoardGeneratorParent<BG>.Instance?.ToString() ?? "<NULL>"}");
            List<string> items = Font.GetOSInstalledFontNames().ToList();
            items.Insert(0, Locale.Get("K45_DTP_DEFAULT_FONT_LABEL"));
            target.items = items.ToArray();
            //string filename = BoardGeneratorParent<BG>.Instance.DrawFont.baseFont.fontNames[0];
            //if (items.Contains(filename))
            //{
            //    target.selectedIndex = items.IndexOf(filename);
            //}
            //else
            //{
                target.selectedIndex = 0;
                BoardGeneratorParent<BG>.Instance.ChangeFont(null);
            //}
        }
    }
}

