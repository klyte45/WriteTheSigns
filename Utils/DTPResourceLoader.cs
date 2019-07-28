using Klyte.Commons.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextProps.Utils
{
    public sealed class DTPResourceLoader : KlyteResourceLoader<DTPResourceLoader>
    {
        public override string Prefix { get; } = "Klyte.DynamicTextProps.";
        public override Shader GetLoadedShader(string shaderName)
        {
            DTPShaderLibrary.GetShaders().TryGetValue(shaderName, out Shader result);
            return result;
        }


        public Dictionary<string, Shader> LoadAllShaders(string assetBundleName)
        {
            AssetBundle bundle = LoadBundle(assetBundleName);
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
            string[] files = bundle.GetAllAssetNames();
            foreach (string filename in files)
            {
                if (filename.EndsWith(".shader"))
                {

                    Shader shader = bundle.LoadAsset<Shader>(filename);
                    string effectiveName = filename.Split('.')[0].Split('/').Last();
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
