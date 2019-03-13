using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.UI;
using Klyte.DynamicTextBoards.i18n;
using Klyte.DynamicTextBoards.Overrides;
using Klyte.DynamicTextBoards.TextureAtlas;
using Klyte.DynamicTextBoards.UI;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.0.0.*")]
namespace Klyte.DynamicTextBoards
{
    public class DynamicTextBoardsMod : BasicIUserMod<DynamicTextBoardsMod, DTBLocaleUtils, DTBResourceLoader, DTBController, DTBCommonTextureAtlas, DTBPanel>
    {
        public DynamicTextBoardsMod()
        {
            Construct();
        }
        protected override ModTab? Tab => ModTab.DynamicTextBoards;

        public override string SimpleName => "Klyte's Dynamic Text Boards";

        public override string Description => "This mod allows creating dynamic text boards in the city";

        public override void doErrorLog(string fmt, params object[] args)
        {
            DTBUtils.doErrorLog(fmt, args);
        }

        public override void doLog(string fmt, params object[] args)
        {
            DTBUtils.doLog(fmt, args);
        }

        public override void LoadSettings()
        {
        }
        
        public override void OnReleased()
        {
            base.OnReleased();
        }

        public override void TopSettingsUI(UIHelperExtension helper)
        {
            UIHelperExtension group8 = helper.AddGroupExtended(Locale.Get("DTB_GENERAL_INFO"));
            addFolderButton(defaultBuildingsConfigurationFolder, group8, "DTB_DEFAULT_BUILDINGS_CONFIG_PATH_TITLE");
            var namesFilesButton = ((UIButton)helper.AddButton(Locale.Get("DTB_GENERATE_DEFAULT_BUILDINGS_FILE"), BoardGeneratorBuildings.GenerateDefaultBuildingsConfiguration));
        }

        private static void addFolderButton(string filePath, UIHelperExtension helper, string localeId)
        {
            var fileInfo = DTBUtils.EnsureFolderCreation(filePath);
            helper.AddLabel(Locale.Get(localeId) + ":");
            var namesFilesButton = ((UIButton)helper.AddButton("/", () => { ColossalFramework.Utils.OpenInFileBrowser(fileInfo.FullName); }));
            namesFilesButton.textColor = Color.yellow;
            DTBUtils.LimitWidth(namesFilesButton, 710);
            namesFilesButton.text = fileInfo.FullName + Path.DirectorySeparatorChar;
        }


        public static readonly string FOLDER_NAME = DTBUtils.BASE_FOLDER_PATH + "DynamicTextBoards";
        internal static readonly string defaultFileNameXml = "DefaultBuildingsConfig";
        public const string DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER = "BuildingsDefaultPlacing";

        public static string defaultBuildingsConfigurationFolder => FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER;

    }
}
