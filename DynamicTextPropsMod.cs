using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.UI;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("3.0.0.*")]
namespace Klyte.DynamicTextProps
{
    public class DynamicTextPropsMod : BasicIUserMod<DynamicTextPropsMod, DTPController, DTPPanel>
    {
        public override string SimpleName => "Dynamic Text Props";

        public override string Description => "This mod allows creating dynamic text props in the city";
        public override string IconName => "K45_KDTPIcon";

        public override void DoErrorLog(string fmt, params object[] args) => LogUtils.DoErrorLog(fmt, args);

        public override void DoLog(string fmt, params object[] args) => LogUtils.DoLog(fmt, args);

        public override void OnReleased() => base.OnReleased();

        public override void TopSettingsUI(UIHelperExtension helper)
        {

            UIHelperExtension group8 = helper.AddGroupExtended(Locale.Get("K45_DTP_GENERAL_INFO"));
            AddFolderButton(DefaultBuildingsConfigurationFolder, group8, "K45_DTP_DEFAULT_BUILDINGS_CONFIG_PATH_TITLE");
            AddFolderButton(AbbreviationFilesPath, group8, "K45_DTP_ABBREVIATION_FILES_PATH_TITLE");
            group8.AddLabel(Locale.Get("K45_DTP_GET_FILES_GITHUB"));
            group8.AddButton(Locale.Get("K45_DTP_GO_TO_GITHUB"), () => Application.OpenURL("https://github.com/klyte45/DynamicTextPropsFiles"));
        }
        protected void BuildSurfaceFont(out UIDynamicFont font, string fontName)
        {
            font = ScriptableObject.CreateInstance<UIDynamicFont>();

            var fontList = new List<string> { fontName };
            font.baseFont = Font.CreateDynamicFontFromOSFont(fontList.ToArray(), 64);
            font.lineHeight = 70;
            font.baseline = 66;
            font.size = 64;
        }

        private static void AddFolderButton(string filePath, UIHelperExtension helper, string localeId)
        {
            FileInfo fileInfo = FileUtils.EnsureFolderCreation(filePath);
            helper.AddLabel(Locale.Get(localeId) + ":");
            var namesFilesButton = ((UIButton) helper.AddButton("/", () => ColossalFramework.Utils.OpenInFileBrowser(fileInfo.FullName)));
            namesFilesButton.textColor = Color.yellow;
            KlyteMonoUtils.LimitWidth(namesFilesButton, 710);
            namesFilesButton.text = fileInfo.FullName + Path.DirectorySeparatorChar;
        }

        public static readonly string FOLDER_NAME = FileUtils.BASE_FOLDER_PATH + "DynamicTextProps";
        internal static readonly string m_defaultFileNameXml = "DefaultBuildingsConfig";
        private const string DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER = "BuildingsDefaultPlacing";
        private const string DEFAULT_GAME_VEHICLES_CONFIG_FOLDER = "VehiclesDefaultPlacing";
        private const string ABBREVIATION_FILES_FOLDER = "AbbreviationFiles";

        public static string DefaultBuildingsConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER;
        public static string DefaultVehiclesConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_VEHICLES_CONFIG_FOLDER;

        public static string AbbreviationFilesPath { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + ABBREVIATION_FILES_FOLDER;

    }
}
