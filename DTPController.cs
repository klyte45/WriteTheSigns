using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using Klyte.DynamicTextProps.Tools;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Klyte.DynamicTextProps
{

    public class DTPController : BaseController<DynamicTextPropsMod, DTPController>
    {

        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();
        public BuildingEditorTool BuildingEditorToolInstance => FindObjectOfType<BuildingEditorTool>();
        public DTPLibPropGroupHigwaySigns GroupInstance => DTPLibPropGroupHigwaySigns.Instance;
        public Dictionary<string, Dictionary<string, string>> AbbreviationFiles { get; private set; }

        public BoardGeneratorRoadNodes BoardGeneratorRoadNodesInstance => BoardGeneratorRoadNodes.Instance;

        public void Awake()
        {
            if (RoadSegmentToolInstance == null)
            {
                FindObjectOfType<ToolController>().gameObject.AddComponent<RoadSegmentTool>();
            }

            if (BuildingEditorToolInstance == null)
            {
                FindObjectOfType<ToolController>().gameObject.AddComponent<BuildingEditorTool>();
            }
            ReloadAbbreviationFiles();

            FontServer.Ensure();
            ReloadFontsFromPath();
        }

        public static void ReloadFontsFromPath()
        {
            FontServer.instance.ResetCollection();
            FontServer.instance.RegisterFont(DEFAULT_FONT_KEY, KlyteResourceLoader.LoadResourceData("UI.DefaultFont.SourceSansPro-Regular.ttf"));

            foreach (string fontFile in Directory.GetFiles(FontFilesPath, "*.ttf"))
            {
                FontServer.instance.RegisterFont(Path.GetFileNameWithoutExtension(fontFile), File.ReadAllBytes(fontFile));
            }
        }

        public void ReloadAbbreviationFiles()
        {
            AbbreviationFiles = LoadAbbreviationFiles(AbbreviationFilesPath);
            BoardGeneratorRoadNodes.Instance.ClearCacheStreetName();
            BoardGeneratorRoadNodes.Instance.ClearCacheStreetQualifier();
        }

        private static Dictionary<string, Dictionary<string, string>> LoadAbbreviationFiles(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            foreach (string filename in Directory.GetFiles(path, "*.txt").Select(x => x.Split(Path.DirectorySeparatorChar).Last()))
            {
                string fileContents = File.ReadAllText(path + Path.DirectorySeparatorChar + filename, Encoding.UTF8);
                result[filename] = new Dictionary<string, string>();
                foreach (string[] entry in fileContents.Split(Environment.NewLine.ToCharArray()).Select(x => x?.Trim()?.Split('=')).Where(x => x != null && x.Length == 2))
                {
                    result[filename][entry[0]] = entry[1];
                }
                LogUtils.DoLog($"LOADED Files at {path} ({filename}) QTT: {result[filename].Count}");
            }
            return result;
        }


        public static readonly string FOLDER_NAME = FileUtils.BASE_FOLDER_PATH + "DynamicTextProps";
        public const string m_defaultFileNameXml = "DefaultBuildingsConfig";
        public const string DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER = "BuildingsDefaultPlacing";
        public const string DEFAULT_GAME_VEHICLES_CONFIG_FOLDER = "VehiclesDefaultPlacing";
        public const string ABBREVIATION_FILES_FOLDER = "AbbreviationFiles";
        public const string FONTS_FILES_FOLDER = "Fonts";

        public const string DEFAULT_FONT_KEY = "/DEFAULT/";

        public static string DefaultBuildingsConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER;
        public static string DefaultVehiclesConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_VEHICLES_CONFIG_FOLDER;
        public static string AbbreviationFilesPath { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + ABBREVIATION_FILES_FOLDER;
        public static string FontFilesPath { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + FONTS_FILES_FOLDER;
    }

}
