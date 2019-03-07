using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Utils
{
    public static class DTBShaderLibrary
    {
        private static Dictionary<string, Shader> m_loadedShaders = null;

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

    }
}
