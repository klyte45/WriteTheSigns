using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;
using Klyte.DynamicTextBoards.TextureAtlas;
using Klyte.DynamicTextBoards.UI;
using Klyte.DynamicTextBoards.Utils;
using System.IO;
using System.Reflection;
using UnityEngine;
using static Klyte.DynamicTextBoards.TextureAtlas.DTBCommonTextureAtlas;

[assembly: AssemblyVersion("0.0.0.0")]
namespace Klyte.DynamicTextBoards
{
    public class DynamicTextBoardsMod : BasicIUserMod<DynamicTextBoardsMod, DTBResourceLoader, DTBController, DTBCommonTextureAtlas, DTBPanel, SpriteNames>
    {
        public DynamicTextBoardsMod() => Construct();

        public override string SimpleName => "Klyte's Dynamic Text Boards";

        public override string Description => "This mod allows creating dynamic text boards in the city";

        public override void DoErrorLog(string fmt, params object[] args) => LogUtils.DoErrorLog(fmt, args);

        public override void DoLog(string fmt, params object[] args) => LogUtils.DoLog(fmt, args);

        public override void LoadSettings()
        {
        }

        public override void OnReleased() => base.OnReleased();

        public override void TopSettingsUI(UIHelperExtension helper)
        {
            UIHelperExtension group8 = helper.AddGroupExtended(Locale.Get("K45_DTB_GENERAL_INFO"));
            AddFolderButton(DefaultBuildingsConfigurationFolder, group8, "K45_DTB_DEFAULT_BUILDINGS_CONFIG_PATH_TITLE");
            UIButton namesFilesButton = ((UIButton) helper.AddButton(Locale.Get("K45_DTB_GENERATE_DEFAULT_BUILDINGS_FILE"), BoardGeneratorBuildings.GenerateDefaultBuildingsConfiguration));
        }

        private static void AddFolderButton(string filePath, UIHelperExtension helper, string localeId)
        {
            FileInfo fileInfo = FileUtils.EnsureFolderCreation(filePath);
            helper.AddLabel(Locale.Get(localeId) + ":");
            UIButton namesFilesButton = ((UIButton) helper.AddButton("/", () => ColossalFramework.Utils.OpenInFileBrowser(fileInfo.FullName)));
            namesFilesButton.textColor = Color.yellow;
            KlyteMonoUtils.LimitWidth(namesFilesButton, 710);
            namesFilesButton.text = fileInfo.FullName + Path.DirectorySeparatorChar;
        }


        public static readonly string FOLDER_NAME = FileUtils.BASE_FOLDER_PATH + "DynamicTextBoards";
        internal static readonly string m_defaultFileNameXml = "DefaultBuildingsConfig";
        public const string DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER = "BuildingsDefaultPlacing";

        public static string DefaultBuildingsConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER;

    }
}
