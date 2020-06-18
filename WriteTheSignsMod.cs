using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.UI;
using SpriteFontPlus;
using System.IO;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.0.1.*")]
namespace Klyte.WriteTheSigns
{
    public class WriteTheSignsMod : BasicIUserMod<WriteTheSignsMod, WTSController, WTSPanel>
    {
        public override string SimpleName => "Write The Signs";

        public override string Description => "Texts, texts everywhere...";
        public override string IconName => "K45_KWTSIcon";

        public SavedInt StartTextureSizeFont = new SavedInt("K45_WTS_startTextureSizeFont", Settings.gameSettingsFile, 0);
        public SavedInt FontQuality = new SavedInt("K45_WTS_fontQuality", Settings.gameSettingsFile, 2);
        public override void OnReleased() => base.OnReleased();

        protected override void OnLevelLoadingInternal() => base.OnLevelLoadingInternal();

        public override void TopSettingsUI(UIHelperExtension helper)
        {

            UIHelperExtension group8 = helper.AddGroupExtended(Locale.Get("K45_WTS_GENERAL_INFO"));
            AddFolderButton(WTSController.DefaultBuildingsConfigurationFolder, group8, "K45_WTS_DEFAULT_BUILDINGS_CONFIG_PATH_TITLE");
            AddFolderButton(WTSController.AbbreviationFilesPath, group8, "K45_WTS_ABBREVIATION_FILES_PATH_TITLE");
            AddFolderButton(WTSController.FontFilesPath, group8, "K45_WTS_FONT_FILES_PATH_TITLE");
            group8.AddLabel(Locale.Get("K45_WTS_GET_FILES_GITHUB"));
            group8.AddButton(Locale.Get("K45_WTS_GO_TO_GITHUB"), () => Application.OpenURL("https://github.com/klyte45/WriteTheSignsFiles"));

            UIHelperExtension group4 = helper.AddGroupExtended(Locale.Get("K45_WTS_GENERATED_TEXT_OPTIONS"));
            group4.AddDropdownLocalized("K45_WTS_INITIAL_TEXTURE_SIZE_FONT", new string[] { "512", "1024", "2048", "4096 (!)", "8192 (!!!)", "16384 (WTF??)" }, StartTextureSizeFont, (x) => StartTextureSizeFont.value = x);
            group4.AddDropdownLocalized("K45_WTS_FONT_QUALITY", new string[] { "50%", "75%", "100%", "125%", "150% (!)", "200% (!!!)", "400% (BEWARE!)", "800% (You don't need this!)" }, FontQuality, (x) =>
            {
                FontQuality.value = x;
                FontServer.instance.SetQualityMultiplier(m_qualityArray[x]);
                WTSController.ReloadFontsFromPath();
            });
            FontServer.instance.SetQualityMultiplier(m_qualityArray[FontQuality]);
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


        private static void AddFolderButton(string filePath, UIHelperExtension helper, string localeId)
        {
            FileInfo fileInfo = FileUtils.EnsureFolderCreation(filePath);
            helper.AddLabel(Locale.Get(localeId) + ":");
            var namesFilesButton = ((UIButton)helper.AddButton("/", () => ColossalFramework.Utils.OpenInFileBrowser(fileInfo.FullName)));
            namesFilesButton.textColor = Color.yellow;
            KlyteMonoUtils.LimitWidth(namesFilesButton, 710);
            namesFilesButton.text = fileInfo.FullName + Path.DirectorySeparatorChar;
        }


    }
}
