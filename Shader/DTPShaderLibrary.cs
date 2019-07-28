using Klyte.Commons.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Klyte.DynamicTextProps.Utils
{
    internal static class DTPShaderLibrary
    {
        internal static Dictionary<string, Shader> m_loadedShaders = null;

        public static Dictionary<string, Shader> GetShaders()
        {
            if (m_loadedShaders == null)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    m_loadedShaders = DTPResourceLoader.instance.LoadAllShaders("Shader.ShaderTest.unity3d");
                }
                else if (Application.platform == RuntimePlatform.LinuxPlayer)
                {
                    m_loadedShaders = DTPResourceLoader.instance.LoadAllShaders("Shader.ShaderTest-linux.unity3d");
                }
                else if (Application.platform == RuntimePlatform.OSXPlayer)
                {

                    m_loadedShaders = DTPResourceLoader.instance.LoadAllShaders("Shader.ShaderTest-macosx.unity3d");
                }
            }
            return m_loadedShaders;
        }

        private static AssetBundle m_memoryLoaded;
        public static void ReloadFromDisk()
        {
            LogUtils.DoErrorLog("LOADING ");
            m_memoryLoaded?.Unload(true);
            m_memoryLoaded = AssetBundle.LoadFromFile("Q:/SkylineMods/TesteLinha/TransportLinesManager/TextProp/TestProj/New Resource.unity3d");
            if (m_memoryLoaded != null)
            {
                LogUtils.DoErrorLog("FOUND");
                DTPResourceLoader.instance.ReadShaders(m_memoryLoaded, out m_loadedShaders);
            }
            else
            {
                LogUtils.DoErrorLog("NOT FOUND");
            }
        }


    }
}
