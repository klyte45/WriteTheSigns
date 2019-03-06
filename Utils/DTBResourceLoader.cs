using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Utils
{
    public sealed class DTBResourceLoader : KlyteResourceLoader<DTBResourceLoader>
    {
        protected override string prefix => "Klyte.DynamicTextBoards.";
        public BoardGeneratorBuildings BoardGeneratorInstance => BoardGeneratorBuildings.instance;
        public BoardGeneratorRoadNodes BoardGeneratorRoadNodesInstance => BoardGeneratorRoadNodes.instance;
        public BoardGeneratorHighwayMileage BoardGeneratorHighwayMileageInstance => BoardGeneratorHighwayMileage.instance;
        static AssetBundle memoryLoaded;
        public override Dictionary<string, Shader> LoadedShaders
        {
            get {
                if (m_loadedShaders == null)
                {
                    memoryLoaded?.Unload(true);
                    memoryLoaded = loadAllShaders("Shader.ShaderTest.unity3d");
                }
                return m_loadedShaders;
            }

        }
        private static Dictionary<string, Shader> m_loadedShaders = null;

        public void ReloadFromDisk()
        {
            memoryLoaded?.Unload(true);
            memoryLoaded = AssetBundle.LoadFromMemory(File.ReadAllBytes("Q:/SkylineMods/TesteLinha/TransportLinesManager/TransportLinesManager/DynamicTextBoards/Shader/ShaderTest.unity3d"));
            if (memoryLoaded != null)
            {
                ReadShaders(memoryLoaded);
            }
        }

        public AssetBundle loadAllShaders(string assetBundleName)
        {
            var bundle = loadBundle(assetBundleName);
            if (bundle != null)
            {
                m_loadedShaders = new Dictionary<string, Shader>();
                ReadShaders(bundle);
            }
            return bundle;
        }

        private void ReadShaders(AssetBundle bundle)
        {
            var files = bundle.GetAllAssetNames();
            foreach (var filename in files)
            {
                if (filename.EndsWith(".shader"))
                {
                    var shader = bundle.LoadAsset<Shader>(filename);
                    shader.name = $"{prefix.Replace(".", "/")}{filename.Split('.')[0].Split('/').Last()}";
                    m_loadedShaders[shader.name] = (shader);
                }
            }
        }

        public void OnDestroy()
        {
            memoryLoaded?.Unload(true);
        }
    }
}
