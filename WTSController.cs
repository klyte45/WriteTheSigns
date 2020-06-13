using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Tools;
using Klyte.WriteTheSigns.Utils;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Klyte.WriteTheSigns
{

    public class WTSController : BaseController<WriteTheSignsMod, WTSController>
    {
        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();
        public BuildingEditorTool BuildingEditorToolInstance => FindObjectOfType<BuildingEditorTool>();

        public WTSRoadPropsSingleton BgRoadNodes => WTSRoadPropsSingleton.instance;
        public Dictionary<string, Dictionary<string, string>> AbbreviationFiles { get; private set; }
        public FontServer FontServer => FontServer.instance;

        public static int DefaultTextureSizeFont => 512 << WriteTheSignsMod.Instance.StartTextureSizeFont;

        public static event Action EventFontsReloadedFromFolder;

        public static event Action EventOnDistrictChanged;

        public static event Action EventOnParkChanged;

        public static event Action EventOnBuildingNameChanged;

        public static void OnDistrictChanged() => EventOnDistrictChanged?.Invoke();
        public static void OnParkChanged() => EventOnParkChanged?.Invoke();
        public static void OnBuildingNameChanged() => EventOnBuildingNameChanged?.Invoke();

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
        }
        public void OnDestroy()
        {
            EventFontsReloadedFromFolder = null;
            EventOnDistrictChanged = null;
        }
        protected override void StartActions()
        {
            ReloadFontsFromPath();
            BuildingManager.instance.EventBuildingReleased += WTSBuildingDataCaches.PurgeBuildingCache;
            BuildingManager.instance.EventBuildingRelocated += WTSBuildingDataCaches.PurgeBuildingCache;
        }

        public static void ReloadFontsFromPath()
        {
            FontServer.instance.ResetCollection();
            FontServer.instance.RegisterFont(DEFAULT_FONT_KEY, KlyteResourceLoader.LoadResourceData("UI.DefaultFont.SourceSansPro-Regular.ttf"), WTSController.DefaultTextureSizeFont);

            foreach (string fontFile in Directory.GetFiles(FontFilesPath, "*.ttf"))
            {
                FontServer.instance.RegisterFont(Path.GetFileNameWithoutExtension(fontFile), File.ReadAllBytes(fontFile), WTSController.DefaultTextureSizeFont);
            }
            EventFontsReloadedFromFolder?.Invoke();
        }

        public void ReloadAbbreviationFiles()
        {
            AbbreviationFiles = LoadAbbreviationFiles(AbbreviationFilesPath);
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
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


        public static readonly string FOLDER_NAME = FileUtils.BASE_FOLDER_PATH + "WriteTheSigns";
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
        public static Shader DEFAULT_SHADER_TEXT = Shader.Find("Custom/Buildings/Building/NoBase") ?? DistrictManager.instance.m_properties.m_areaNameShader;
        public static Shader DISALLOWED_SHADER_PROP = Shader.Find("Custom/Buildings/Building/Default");
    }

}
