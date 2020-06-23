extern alias TLM;

using ColossalFramework.Globalization;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Connectors;
using Klyte.WriteTheSigns.Overrides;
using Klyte.WriteTheSigns.Rendering;
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
using static ColossalFramework.UI.UITextureAtlas;

namespace Klyte.WriteTheSigns
{

    public class WTSController : BaseController<WriteTheSignsMod, WTSController>
    {
        public RoadSegmentTool RoadSegmentToolInstance => FindObjectOfType<RoadSegmentTool>();
        public BuildingEditorTool BuildingEditorToolInstance => FindObjectOfType<BuildingEditorTool>();

        internal WTSSpritesRenderingRules SpriteRenderingRules { get; private set; }
        internal WTSBuildingPropsSingleton BuildingPropsSingleton { get; private set; }
        internal WTSVehicleTextsSingleton VehicleTextsSingleton { get; private set; }
        internal WTSDestinationSingleton DestinationSingleton { get; private set; }
        internal IConnectorTLM ConnectorTLM { get; private set; }
        internal IConnectorADR ConnectorADR { get; private set; }

        public WTSRoadPropsSingleton RoadPropsSingleton { get; private set; }
        public Dictionary<string, Dictionary<string, string>> AbbreviationFiles { get; private set; }
        public FontServer FontServer => FontServer.instance;

        public static int DefaultTextureSizeFont => 512 << WriteTheSignsMod.Instance.StartTextureSizeFont;

        public event Action EventFontsReloadedFromFolder;
        public event Action EventOnDistrictChanged;
        public event Action EventOnParkChanged;
        public event Action EventOnBuildingNameChanged;
        public event Action EventOnZeroMarkerChanged;
        public event Action EventOnPostalCodeChanged;

        public static void OnDistrictChanged() => WriteTheSignsMod.Controller?.EventOnDistrictChanged?.Invoke();
        public static void OnParkChanged() => WriteTheSignsMod.Controller?.EventOnParkChanged?.Invoke();
        public static void OnBuildingNameChanged() => WriteTheSignsMod.Controller?.EventOnBuildingNameChanged?.Invoke();
        public static void OnZeroMarkChanged() => WriteTheSignsMod.Controller?.EventOnZeroMarkerChanged?.Invoke();

        public static void OnPostalCodeChanged() => WriteTheSignsMod.Controller?.EventOnPostalCodeChanged?.Invoke();

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
            SpriteRenderingRules = gameObject.AddComponent<WTSSpritesRenderingRules>();
            BuildingPropsSingleton = gameObject.AddComponent<WTSBuildingPropsSingleton>();
            RoadPropsSingleton = gameObject.AddComponent<WTSRoadPropsSingleton>();
            DestinationSingleton = gameObject.AddComponent<WTSDestinationSingleton>();
            VehicleTextsSingleton = gameObject.AddComponent<WTSVehicleTextsSingleton>();
            ConnectorTLM = PluginUtils.GetImplementationTypeForMod<ConnectorTLM, ConnectorTLMFallback, IConnectorTLM>(gameObject, "TransportLinesManager", "13.3.6");
            ConnectorADR = PluginUtils.GetImplementationTypeForMod<ConnectorADR, ConnectorADRFallback, IConnectorADR>(gameObject, "KlyteAddresses", "2.0.4");

            var spritesToAdd = new List<SpriteInfo>();
            var errors = new List<string>();
            foreach (var imgFile in Directory.GetFiles(WTSController.ExtraSpritesFolder, "*.png"))
            {
                var fileData = File.ReadAllBytes(imgFile);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(fileData))
                {
                    if (tex.width <= 256 && tex.width <= 256)
                    {
                        var imgName = $"K45_WKS_{Path.GetFileNameWithoutExtension(imgFile)}";
                        spritesToAdd.Add(new SpriteInfo
                        {
                            border = new RectOffset(),
                            name = imgName,
                            texture = tex
                        });
                    }
                    else
                    {
                        errors.Add($"{Path.GetFileName(imgFile)}: {Locale.Get("K45_WTS_CUSTOMSPRITE_IMAGETOOLARGE")} (max: 256x256)");
                    }
                }
                else
                {
                    errors.Add($"{Path.GetFileName(imgFile)}: {Locale.Get("K45_WTS_CUSTOMSPRITE_FAILEDREADIMAGE")}");
                }
            }
            if (spritesToAdd.Count > 0)
            {
                TextureAtlasUtils.RegenerateDefaultTextureAtlas(spritesToAdd);
            }
            if (errors.Count > 0)
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    message = $"{Locale.Get("K45_WTS_CUSTOMSPRITE_ERRORHEADER")}:\n\t{string.Join("\n\t", errors.ToArray())}"
                }, (x) => true);
            }
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
        public const string DEFAULT_GAME_BUILDINGS_CONFIG_FOLDER = "BuildingsDefaultPlacing";
        public const string DEFAULT_GAME_VEHICLES_CONFIG_FOLDER = "VehiclesDefaultPlacing";
        public const string ABBREVIATION_FILES_FOLDER = "AbbreviationFiles";
        public const string FONTS_FILES_FOLDER = "Fonts";
        public const string EXTRA_SPRITES_FILES_FOLDER = "Sprites";

        public const string DEFAULT_FONT_KEY = "/DEFAULT/";

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
