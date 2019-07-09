using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Utils
{
    internal class DTBUtils 
    {

        public static void ReloadFontsOf<BG>(UIDropDown target) where BG : BoardGeneratorParent<BG>
        {
            LogUtils.DoLog($"{BoardGeneratorParent<BG>.Instance}");
            List<string> items = Font.GetOSInstalledFontNames().ToList();
            items.Insert(0, Locale.Get("DTB_DEFAULT_FONT_LABEL"));
            target.items = items.ToArray();
            string filename = BoardGeneratorParent<BG>.Instance.DrawFont.baseFont.fontNames[0];
            if (items.Contains(filename))
            {
                target.selectedIndex = items.IndexOf(filename);
            }
            else
            {
                target.selectedIndex = 0;
                BoardGeneratorParent<BG>.Instance.ChangeFont(null);
            }
        }
    }
}

