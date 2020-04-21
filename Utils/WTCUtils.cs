using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.WriteTheCity.Data;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheCity.Utils
{
    internal class WTCUtils
    {

        public static void ReloadFontsOf(UIDropDown target, bool hasDefaultOption = false)
        {
            var items = FontServer.instance.GetAllFonts().ToList();
            items.Sort();
            items.Remove(WTCController.DEFAULT_FONT_KEY);
            items.Insert(0, Locale.Get("K45_WTC_DEFAULT_FONT_LABEL"));
            if (hasDefaultOption)
            {
                items.Insert(0, Locale.Get("K45_WTC_USE_GROUP_SETTING_FONT"));
            }
            target.items = items.ToArray();
            string filename = target.selectedValue;
            if (items.Contains(filename))
            {
                target.selectedIndex = items.IndexOf(filename);
            }
            else
            {
                target.selectedIndex = 0;
            }
        }

        public static string ApplyAbbreviations(string name)
        {
            if (WriteTheCityMod.Controller.AbbreviationFiles.TryGetValue(WTCRoadNodesData.Instance.AbbreviationFile ?? "", out Dictionary<string, string> translations))
            {
                foreach (string key in translations.Keys.Where(x => x.Contains(" ")))
                {
                    name = TextUtils.ReplaceCaseInsensitive(name, key, translations[key], StringComparison.OrdinalIgnoreCase);

                }
                string[] parts = name.Split(' ');
                for (int i = 0; i < parts.Length; i++)
                {
                    if ((i == 0 && translations.TryGetValue($"^{parts[i]}", out string replacement))
                        || (i == parts.Length - 1 && translations.TryGetValue($"{parts[i]}$", out replacement))
                        || (i > 0 && i < parts.Length - 1 && translations.TryGetValue($"={parts[i]}=", out replacement))
                        || translations.TryGetValue(parts[i], out replacement))
                    {
                        parts[i] = replacement;
                    }
                }
                return string.Join(" ", parts.Where(x => !x.IsNullOrWhiteSpace()).ToArray());

            }
            else
            {
                return name;
            }
        }
    }
}

