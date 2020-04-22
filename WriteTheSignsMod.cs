using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.0.0.*")]
namespace Klyte.WriteTheSigns
{
    public class WriteTheSignsMod : BasicIUserMod<WriteTheSignsMod, WTSController, WTSPanel>
    {
        public override string SimpleName => "Write The Signs";

        public override string Description => "Texts, texts everywhere...";
        public override string IconName => "K45_KWTSIcon";
        
        public override void OnReleased() => base.OnReleased();

        public override void TopSettingsUI(UIHelperExtension helper)
        {

            UIHelperExtension group8 = helper.AddGroupExtended(Locale.Get("K45_WTS_GENERAL_INFO"));
            AddFolderButton(WTSController.DefaultBuildingsConfigurationFolder, group8, "K45_WTS_DEFAULT_BUILDINGS_CONFIG_PATH_TITLE");
            AddFolderButton(WTSController.AbbreviationFilesPath, group8, "K45_WTS_ABBREVIATION_FILES_PATH_TITLE");
            AddFolderButton(WTSController.FontFilesPath, group8, "K45_WTS_FONT_FILES_PATH_TITLE");
            group8.AddLabel(Locale.Get("K45_WTS_GET_FILES_GITHUB"));
            group8.AddButton(Locale.Get("K45_WTS_GO_TO_GITHUB"), () => Application.OpenURL("https://github.com/klyte45/WriteTheSignsFiles"));
        }
  
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
