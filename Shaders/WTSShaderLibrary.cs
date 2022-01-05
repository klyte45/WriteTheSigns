using ColossalFramework;
using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.WriteTheSigns
{
    public class WTSShaderLibrary : SingletonLite<WTSShaderLibrary>
    {
        private Dictionary<string, Shader> m_loadedShaders = null;

        public WTSShaderLibrary()
        {
            GetShaders();
        }

        public Dictionary<string, Shader> GetShaders()
        {
            if (m_loadedShaders is null)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    m_loadedShaders = LoadAllShaders("Shaders.ShaderTest.unity3d");
                    LogUtils.DoLog($"Shaders loaded for {Application.platform}!");
                }
                else if (Application.platform == RuntimePlatform.LinuxPlayer)
                {
                    m_loadedShaders = LoadAllShaders("Shaders.ShaderTest-linux.unity3d");
                    LogUtils.DoLog($"Shaders loaded for {Application.platform}!");
                }
                else if (Application.platform == RuntimePlatform.OSXPlayer)
                {

                    m_loadedShaders = LoadAllShaders("Shaders.ShaderTest-macosx.unity3d");
                    LogUtils.DoLog($"Shaders loaded for {Application.platform}!");
                }
                else
                {
                    m_loadedShaders = new Dictionary<string, Shader>();
                    LogUtils.DoErrorLog($"WARNING: Shaders not found for {Application.platform}!");
                }
            }
            return m_loadedShaders;
        }

        private AssetBundle m_memoryLoaded;
        public bool ReloadFromDisk()
        {
            LogUtils.DoWarnLog("LOADING Shaders");
            m_memoryLoaded?.Unload(true);
            m_memoryLoaded = AssetBundle.LoadFromFile("Q:/GameModding/Cities Skylines/CodedMods/Current/projects/WriteTheCity/Shaders/ShaderTest.unity3d");
            if (m_memoryLoaded != null)
            {
                LogUtils.DoWarnLog("FOUND Shaders");
                ReadShaders(m_memoryLoaded, out m_loadedShaders);
                return true;
            }
            else
            {
                LogUtils.DoErrorLog("NOT FOUND Shaders");
                return false;
            }
        }

        public Shader GetLoadedShader(string shaderName)
        {
            GetShaders().TryGetValue(shaderName, out Shader result);
            return result;
        }
        private Dictionary<string, Shader> LoadAllShaders(string assetBundleName)
        {
            AssetBundle bundle = KlyteResourceLoader.LoadBundle(assetBundleName);
            if (bundle != null)
            {

                ReadShaders(bundle, out Dictionary<string, Shader> m_loadedShaders);
                bundle.Unload(false);
                return m_loadedShaders;
            }
            return null;
        }

        private void ReadShaders(AssetBundle bundle, out Dictionary<string, Shader> m_loadedShaders)
        {
            m_loadedShaders = new Dictionary<string, Shader>();
            string[] files = bundle.GetAllAssetNames();
            foreach (string filename in files)
            {
                if (filename.EndsWith(".shader"))
                {

                    Shader shader = bundle.LoadAsset<Shader>(filename);
                    string effectiveName = filename.Split('.')[0].Split('/').Last();
                    if (effectiveName.StartsWith("klyte"))
                    {
                        shader.name = $"Klyte/WTS/{effectiveName}";
                        m_loadedShaders[shader.name] = (shader);
                    }
                    else
                    {
                        GameObject.Destroy(shader);
                    }
                }
            }
        }
    }
}
