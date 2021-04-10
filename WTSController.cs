extern alias TLM;

using ColossalFramework.Globalization;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.ModShared;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Sprites;
using Klyte.WriteTheSigns.Tools;
using Klyte.WriteTheSigns.Utils;
using SpriteFontPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static ColossalFramework.UI.UITextureAtlas;

namespace Klyte.WriteTheSigns
{

    public class WTSController : BaseController<WriteTheSignsMod, WTSController>
    {
        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();
        public BuildingEditorTool BuildingEditorToolInstance => FindObjectOfType<BuildingEditorTool>();

        internal WTSAtlasesLibrary AtlasesLibrary { get; private set; }
        internal WTSBuildingPropsSingleton BuildingPropsSingleton { get; private set; }
        internal WTSVehicleTextsSingleton VehicleTextsSingleton { get; private set; }
        internal WTSOnNetPropsSingleton OnNetPropsSingleton { get; private set; }
        internal IBridgeTLM ConnectorTLM { get; private set; }
        internal IBridgeADR ConnectorADR { get; private set; }

        public WTSRoadPropsSingleton RoadPropsSingleton { get; private set; }
        public Dictionary<string, Dictionary<string, string>> AbbreviationFiles { get; private set; }
        public FontServer FontServer => FontServer.instance;

        public static int DefaultTextureSizeFont => 512 << WriteTheSignsMod.StartTextureSizeFont;

        public event Action EventFontsReloadedFromFolder;
        public event Action EventOnDistrictChanged;
        public event Action EventOnParkChanged;
        public event Action EventOnBuildingNameChanged;
        public event Action EventOnZeroMarkerChanged;
        public event Action EventOnPostalCodeChanged;

        public static void OnDistrictChanged() => WriteTheSignsMod.Controller?.EventOnDistrictChanged?.Invoke();
        public static void OnParkChanged() => WriteTheSignsMod.Controller?.EventOnParkChanged?.Invoke();
        public static void OnBuildingNameChanged() => WriteTheSignsMod.Controller?.EventOnBuildingNameChanged?.Invoke();
        public static void OnCityNameChanged() => RenderUtils.ClearCacheCityName();
        public static void OnZeroMarkChanged() => WriteTheSignsMod.Controller?.EventOnZeroMarkerChanged?.Invoke();
        public static void OnPostalCodeChanged() => WriteTheSignsMod.Controller?.EventOnPostalCodeChanged?.Invoke();

        public void Awake()
        {
            if (RoadSegmentToolInstance == null)
            {
                ToolsModifierControl.toolController.gameObject.AddComponent<RoadSegmentTool>();
            }

            if (BuildingEditorToolInstance == null)
            {
                ToolsModifierControl.toolController.gameObject.AddComponent<BuildingEditorTool>();
            }
            ReloadAbbreviationFiles();

            FontServer.Ensure();
            AtlasesLibrary = gameObject.AddComponent<WTSAtlasesLibrary>();
            BuildingPropsSingleton = gameObject.AddComponent<WTSBuildingPropsSingleton>();
            RoadPropsSingleton = gameObject.AddComponent<WTSRoadPropsSingleton>();
            VehicleTextsSingleton = gameObject.AddComponent<WTSVehicleTextsSingleton>();
            OnNetPropsSingleton = gameObject.AddComponent<WTSOnNetPropsSingleton>();
            ConnectorTLM = PluginUtils.GetImplementationTypeForMod<BridgeTLM, BridgeTLMFallback, IBridgeTLM>(gameObject, "TransportLinesManager", "13.4.0.1");
            ConnectorADR = PluginUtils.GetImplementationTypeForMod<BridgeADR, BridgeADRFallback, IBridgeADR>(gameObject, "KlyteAddresses", "2.0.4.1");
        }



        protected override void StartActions()
        {
            ReloadFontsFromPath();
            BuildingManager.instance.EventBuildingReleased += WTSBuildingDataCaches.PurgeBuildingCache;
            BuildingManager.instance.EventBuildingRelocated += WTSBuildingDataCaches.PurgeBuildingCache;

            NetManagerOverrides.EventSegmentNameChanged += OnNameSeedChanged;
            BuildingManager.instance.EventBuildingRelocated += RenderUtils.ClearCacheBuildingName;
            BuildingManager.instance.EventBuildingReleased += RenderUtils.ClearCacheBuildingName;
            BuildingManager.instance.EventBuildingCreated += RenderUtils.ClearCacheBuildingName;
            EventOnDistrictChanged += RenderUtils.ClearCacheDistrictName;
            EventOnParkChanged += RenderUtils.ClearCacheParkName;
            EventOnBuildingNameChanged += RenderUtils.ClearCacheBuildingName;
            EventOnPostalCodeChanged += RenderUtils.ClearCachePostalCode;
            EventOnZeroMarkerChanged += OnNameSeedChanged;

        }

        private void OnNameSeedChanged(ushort segmentId) => OnNameSeedChanged();
        private void OnNameSeedChanged()
        {
            RenderUtils.ClearCacheFullStreetName();
            RenderUtils.ClearCacheStreetName();
            RenderUtils.ClearCacheStreetQualifier();
            RenderUtils.ClearCachePostalCode();
            RenderUtils.ClearCacheBuildingName();
        }

        public static void ReloadFontsFromPath()
        {
            FontServer.instance.ResetCollection();
            FontServer.instance.RegisterFont(DEFAULT_FONT_KEY, KlyteResourceLoader.LoadResourceData("UI.DefaultFont.SourceSansPro-Regular.ttf"), WTSController.DefaultTextureSizeFont);

            foreach (string fontFile in Directory.GetFiles(FontFilesPath, "*.ttf"))
            {
                FontServer.instance.RegisterFont(Path.GetFileNameWithoutExtension(fontFile), File.ReadAllBytes(fontFile), WTSController.DefaultTextureSizeFont);
            }
            WriteTheSignsMod.Controller?.EventFontsReloadedFromFolder?.Invoke();
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
        public const string m_defaultFileNameBuildingsXml = "WTS_DefaultBuildingsConfig";
        public const string m_defaultFileNameVehiclesXml = "WTS_DefaultVehiclesConfig";
        public const string m_defaultFileNamePropsXml = "WTS_DefaultPropsConfig";
        public const string DEFAULT_GAME_PROP_LAYOUT_FOLDER = "PropsDefaultLayouts";
        public const string DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER = "BuildingsDefaultPlacing";
        public const string DEFAULT_GAME_VEHICLES_CONFIG_FOLDER = "VehiclesDefaultPlacing";
        public const string ABBREVIATION_FILES_FOLDER = "AbbreviationFiles";
        public const string FONTS_FILES_FOLDER = "Fonts";
        public const string EXTRA_SPRITES_FILES_FOLDER = "Sprites";
        public const string DEFAULT_FONT_KEY = "/DEFAULT/";
        public const string EXTRA_SPRITES_FILES_FOLDER_ASSETS = "K45WTS_Sprites";

        public static string DefaultPropsLayoutConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_PROP_LAYOUT_FOLDER;
        public static string DefaultBuildingsConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER;
        public static string DefaultVehiclesConfigurationFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + DEFAULT_GAME_VEHICLES_CONFIG_FOLDER;
        public static string ExtraSpritesFolder { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + EXTRA_SPRITES_FILES_FOLDER;
        public static string AbbreviationFilesPath { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + ABBREVIATION_FILES_FOLDER;
        public static string FontFilesPath { get; } = FOLDER_NAME + Path.DirectorySeparatorChar + FONTS_FILES_FOLDER;

        public static Shader DEFAULT_SHADER_TEXT = Shader.Find("Custom/Props/Prop/Default") ?? DistrictManager.instance.m_properties.m_areaNameShader;
        internal bool? m_tlmExistsAndActive = null;
        internal bool? m_addressesExistsAndActive = null;

    }

}
