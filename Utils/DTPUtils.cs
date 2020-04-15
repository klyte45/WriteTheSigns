using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Overrides;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.DynamicTextProps.Utils
{
    internal class DTPUtils
    {

        public static void ReloadFontsOf<BG>(UIDropDown target) where BG : BoardGeneratorParent<BG>
        {
            LogUtils.DoLog($"Instance = {BoardGeneratorParent<BG>.Instance?.ToString() ?? "<NULL>"}");
            DTPController.ReloadFontsFromPath();
            var items = FontServer.instance.GetAllFonts().ToList();
            items.Sort();
            items.Remove(DTPController.DEFAULT_FONT_KEY);
            items.Insert(0, Locale.Get("K45_DTP_DEFAULT_FONT_LABEL"));
            target.items = items.ToArray();
            string filename = BoardGeneratorParent<BG>.Instance.FontName;
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

        public static string ApplyAbbreviations(string name)
        {
            if (DynamicTextPropsMod.Controller.AbbreviationFiles.TryGetValue(DTPNetNodesData.Instance.CurrentDescriptor.AbbreviationFile ?? "", out Dictionary<string, string> translations))
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

