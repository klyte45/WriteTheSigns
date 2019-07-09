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
        protected override string Prefix => "Klyte.DynamicTextBoards.";
        public override Shader GetLoadedShader(string shaderName)
        {
            DTBShaderLibrary.GetShaders().TryGetValue(shaderName, out Shader result);
            return result;
        }


        public Dictionary<string, Shader> loadAllShaders(string assetBundleName)
        {
            var bundle = LoadBundle(assetBundleName);
            if (bundle != null)
            {

                ReadShaders(bundle, out Dictionary<string, Shader> m_loadedShaders);
                bundle.Unload(false);
                return m_loadedShaders;
            }
            return null;
        }

        public void ReadShaders(AssetBundle bundle, out Dictionary<string, Shader> m_loadedShaders)
        {
            m_loadedShaders = new Dictionary<string, Shader>();
            var files = bundle.GetAllAssetNames();
            foreach (var filename in files)
            {
                if (filename.EndsWith(".shader"))
                {

                    var shader = bundle.LoadAsset<Shader>(filename);
                    var effectiveName = filename.Split('.')[0].Split('/').Last();
                    if (effectiveName.StartsWith("klyte"))
                    {
                        shader.name = $"{Prefix.Replace(".", "/")}{effectiveName}";
                        m_loadedShaders[shader.name] = (shader);
                        
                    }
                    else
                    {
                        Destroy(shader);
                    }
                }
            }
        }

    }
}
