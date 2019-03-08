using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Utils
{
    internal static class DTBShaderLibrary
    {
        internal static Dictionary<string, Shader> m_loadedShaders = null;

        public static Dictionary<string, Shader> GetShaders()
        {
            if (m_loadedShaders == null)
            {
                if (Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    m_loadedShaders = DTBResourceLoader.instance.loadAllShaders("Shader.ShaderTest.unity3d");
                }
                else if (Application.platform == RuntimePlatform.LinuxPlayer)
                {
                    m_loadedShaders = DTBResourceLoader.instance.loadAllShaders("Shader.ShaderTest-linux.unity3d");
                }
                else if (Application.platform == RuntimePlatform.OSXPlayer)
                {

                    m_loadedShaders = DTBResourceLoader.instance.loadAllShaders("Shader.ShaderTest-macosx.unity3d");
                }
            }
            return m_loadedShaders;
        }
        static AssetBundle memoryLoaded;
        public static void ReloadFromDisk()
        {
            DTBUtils.doErrorLog("LOADING ");
            memoryLoaded?.Unload(true);
            memoryLoaded = AssetBundle.LoadFromFile("Q:/SkylineMods/TesteLinha/TransportLinesManager/TextProp/TestProj/New Resource.unity3d");
            if (memoryLoaded != null)
            {
                DTBUtils.doErrorLog("FOUND");
                DTBResourceLoader.instance.ReadShaders(memoryLoaded, out m_loadedShaders);
            }
            else
            {
                DTBUtils.doErrorLog("NOT FOUND");
            }
        }


    }
}
