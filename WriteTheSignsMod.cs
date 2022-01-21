using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using FontStashSharp;
using Klyte.Commons.Extensions;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.UI;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.4.0.0")]
namespace Klyte.WriteTheSigns
{
    public class WriteTheSignsMod : BasicIUserMod<WriteTheSignsMod, WTSController, WTSPanel>
    {
        public override string SimpleName => "Write The Signs";

        public override string Description => "Texts, texts everywhere...";
        public override string IconName => "K45_KWTSIcon";

        public static SavedInt StartTextureSizeFont = new SavedInt("K45_WTS_startTextureSizeFont", Settings.gameSettingsFile, 0);
        public static SavedInt FontQuality = new SavedInt("K45_WTS_fontQuality", Settings.gameSettingsFile, 2);
        public static SavedFloat ClockPrecision = new SavedFloat("K45_WTS_clockPrecision", Settings.gameSettingsFile, 15);
        public static SavedBool ClockShowLeadingZero = new SavedBool("K45_WTS_clockShowLeadingZero", Settings.gameSettingsFile, true);
        public static SavedBool Clock12hFormat = new SavedBool("K45_WTS_clock12hFormat", Settings.gameSettingsFile, false);
        public override void OnReleased() => base.OnReleased();

        private UIButton reloadImages;

        protected override void OnLevelLoadingInternal()
        {
            base.OnLevelLoadingInternal();
            WTSShaderLibrary.instance.GetShaders();
        }

        protected override Dictionary<ulong, string> IncompatibleModList => new Dictionary<ulong, string>();
        protected override List<string> IncompatibleDllModList => new List<string> { "KlyteDynamicTextProps" };

        public override void TopSettingsUI(UIHelperExtension helper)
        {

            UIHelperExtension group8 = helper.AddGroupExtended(Locale.Get("K45_WTS_GENERAL_INFO"));
            AddFolderButton(WTSController.DefaultBuildingsConfigurationFolder, group8, "K45_WTS_DEFAULT_BUILDINGS_CONFIG_PATH_TITLE");
            AddFolderButton(WTSController.DefaultVehiclesConfigurationFolder, group8, "K45_WTS_DEFAULT_VEHICLES_CONFIG_PATH_TITLE");
            AddFolderButton(WTSController.DefaultPropsLayoutConfigurationFolder, group8, "K45_WTS_DEFAULT_PROP_LAYOUTS_PATH_TITLE");
            AddFolderButton(WTSController.DefaultHwShieldsConfigurationFolder, group8, "K45_WTS_DEFAULT_HWSHIELDS_CONFIG_PATH_TITLE");
            AddFolderButton(WTSController.AbbreviationFilesPath, group8, "K45_WTS_ABBREVIATION_FILES_PATH_TITLE");
            AddFolderButton(WTSController.FontFilesPath, group8, "K45_WTS_FONT_FILES_PATH_TITLE");
            AddFolderButton(WTSController.ExtraSpritesFolder, group8, "K45_WTS_EXTRA_SPRITES_PATH_TITLE");
            group8.AddLabel(Locale.Get("K45_WTS_GET_FILES_GITHUB"));
            group8.AddButton(Locale.Get("K45_WTS_GO_TO_GITHUB"), () => Application.OpenURL("https://github.com/klyte45/WriteTheSignsFiles"));
            group8.AddButton(Locale.Get("K45_WTS_GO_TO_WIKI"), () => Application.OpenURL("https://github.com/klyte45/WriteTheSigns/wiki"));
            group8.AddButton(Locale.Get("K45_WTS_GO_TO_WTSWORKSHOP"), () => Application.OpenURL("https://github.com/klyte45/WriteTheSigns/wiki"));
            reloadImages = (UIButton)group8.AddButton(Locale.Get("K45_WTS_REFRESH_IMAGES_FOLDER"), () => Controller?.AtlasesLibrary.LoadImagesFromLocalFolders());
            reloadImages.eventVisibilityChanged += (k, x) =>
            {
                if (x)
                {
                    if (Controller is null)
                    {
                        k.Disable();
                    }
                    else
                    {
                        k.Enable();
                    }
                }
            };

            UIHelperExtension group4 = helper.AddGroupExtended(Locale.Get("K45_WTS_GENERATED_TEXT_OPTIONS"));
            (group4.AddDropdownLocalized("K45_WTS_INITIAL_TEXTURE_SIZE_FONT", new string[] { "512", "1024", "2048", "4096 (!)", "8192 (!!!)", "16384 (WTF??)" }, StartTextureSizeFont, (x) => StartTextureSizeFont.value = x).parent as UIPanel).autoFitChildrenVertically = true;
            (group4.AddDropdownLocalized("K45_WTS_FONT_QUALITY", new string[] { "50%", "75%", "100%", "125%", "150% (!)", "200% (!!!)", "400% (BEWARE!)", "800% (You don't need this!)" }, FontQuality, (x) =>
            {
                FontQuality.value = x;
                FontServer.instance.SetQualityMultiplier(m_qualityArray[x]);
                WTSController.ReloadFontsFromPath();
            }).parent as UIPanel).autoFitChildrenVertically = true;
            FontServer.instance.SetQualityMultiplier(m_qualityArray[FontQuality]);
            (group4.AddDropdownLocalized("K45_WTS_MAX_PARALLEL_WORD_PROCESSES", new string[] { "1", "2", "4", "8", "16", "32","64","128 (!)", "256 (!!)", "512 (Your game may freeze)", "1024 (Your game WILL freeze)" }, Convert.ToString(FontSystem.MaxCoroutines, 2).Length - 1, (x) => FontSystem.MaxCoroutines.value = 1 << x).parent as UIPanel).autoFitChildrenVertically = true;



            UIHelperExtension group5 = helper.AddGroupExtended(Locale.Get("K45_WTS_GENERATED_CLOCK_OPTIONS"));
            (group5.AddDropdownLocalized("K45_WTS_CLOCK_MINUTES_PRECISION", new string[] { "30", "20", "15 (DEFAULT)", "12", "10", "7.5", "6", "5", "4", "3 (!)", "2 (!!)", "1 (!!!!)" }, Array.IndexOf(m_clockPrecision, ClockPrecision), (x) =>
             {
                 ClockPrecision.value = m_clockPrecision[x];
             }).parent as UIPanel).autoFitChildrenVertically = true;
            group5.AddCheckboxLocale("K45_WTS_CLOCK_SHOW_LEADING_ZERO", ClockShowLeadingZero, (x) => ClockShowLeadingZero.value = x);
            group5.AddCheckboxLocale("K45_WTS_CLOCK_12H_CLOCK", Clock12hFormat, (x) => Clock12hFormat.value = x);
            UIHelperExtension group6 = helper.AddGroupExtended(Locale.Get("K45_WTS_CACHE_INFO"));
            UIButton calcMeshSize = null;
            var calcMeshLbl = "Calculate text meshes cache size";
            calcMeshSize = (UIButton)group6.AddButton(calcMeshLbl, () => calcMeshSize.text = $"{calcMeshLbl}: {FontServer.instance?.GetAllFontsCacheSize().ToString("#,##0") ?? "N/A " }bytes");

        }

        private readonly float[] m_qualityArray = new float[]
        {
            .5f,
            .75f,
            1f,
            1.25f,
            1.5f,
            2f,
            4f,
            8f
        };

        private readonly float[] m_clockPrecision = new float[]
        {
           30, 20, 15, 12, 10, 7.5f,6, 5, 4,3, 2,1
        };


        private static void AddFolderButton(string filePath, UIHelperExtension helper, string localeId)
        {
            FileInfo fileInfo = FileUtils.EnsureFolderCreation(filePath);
            helper.AddLabel(Locale.Get(localeId) + ":");
            var namesFilesButton = ((UIButton)helper.AddButton("/", () => ColossalFramework.Utils.OpenInFileBrowser(fileInfo.FullName)));
            namesFilesButton.textColor = Color.yellow;
            KlyteMonoUtils.LimitWidthAndBox(namesFilesButton, 710);
            namesFilesButton.text = fileInfo.FullName + Path.DirectorySeparatorChar;
        }

        protected override Tuple<string, string> GetButtonLink() => Tuple.New("Request & Read future features of/for WTS", "https://github.com/klyte45/WriteTheSigns/issues");

    }
}