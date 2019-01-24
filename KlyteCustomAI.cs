using ICities;
using Klyte.Commons.Extensors;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.0.0.0")]
namespace Klyte.CustomAI
{
    public class KlyteCustomAI : IUserMod, ILoadingExtension
    {

        public string Name => "Klyte Custom AI " + KlyteCustomAI.version;
        public string Description => "Mod with new customized AI's for buildings";

        public void OnCreated(ILoading loading) { }

        public void OnLevelLoaded(LoadMode mode)
        {
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame && mode != LoadMode.NewGameFromScenario)
            {
                return;
            }


            GameObject gameObject = new GameObject
            {
                name = "KlyteCustomAI"
            };

            var typeTarg = typeof(Redirector<>);
            var instances = from t in Assembly.GetAssembly(typeof(KlyteCustomAI)).GetTypes()
                            let y = t.BaseType
                            where t.IsClass && !t.IsAbstract && y != null && y.IsGenericType && y.GetGenericTypeDefinition() == typeTarg
                            select t;

            foreach (Type t in instances)
            {
                gameObject.AddComponent(t);
            }

        }

        public void OnLevelUnloading()
        {
        }

        public void OnReleased()
        {
        }

        public static string minorVersion
        {
            get {
                return majorVersion + "." + typeof(KlyteCustomAI).Assembly.GetName().Version.Build;
            }
        }
        public static string majorVersion
        {
            get {
                return typeof(KlyteCustomAI).Assembly.GetName().Version.Major + "." + typeof(KlyteCustomAI).Assembly.GetName().Version.Minor;
            }
        }
        public static string fullVersion
        {
            get {
                return minorVersion + " r" + typeof(KlyteCustomAI).Assembly.GetName().Version.Revision;
            }
        }
        public static string version
        {
            get {
                if (typeof(KlyteCustomAI).Assembly.GetName().Version.Minor == 0 && typeof(KlyteCustomAI).Assembly.GetName().Version.Build == 0)
                {
                    return typeof(KlyteCustomAI).Assembly.GetName().Version.Major.ToString();
                }
                if (typeof(KlyteCustomAI).Assembly.GetName().Version.Build > 0)
                {
                    return minorVersion;
                }
                else
                {
                    return majorVersion;
                }
            }
        }
    }
}
