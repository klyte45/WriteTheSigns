using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using System.Collections.Generic;

namespace Klyte.DynamicTextProps.Utils
{
    internal static class PrefabUtils<T> where T : PrefabInfo
    {
        private static List<T> m_assetsLoaded;

        public static List<T> AssetsLoaded
        {
            get {
                if (m_assetsLoaded == null)
                {
                    m_assetsLoaded = GetInfos();
                }
                return m_assetsLoaded;
            }
        }



        private static List<T> GetInfos()
        {
            var list = new List<T>();
            uint num = 0u;
            while (num < (ulong)PrefabCollection<T>.LoadedCount())
            {
                T prefabInfo = PrefabCollection<T>.GetLoaded(num);
                if (prefabInfo != null)
                {
                    list.Add(prefabInfo);
                }
                num += 1u;
            }
            return list;
        }

    }

    public static class PrefabUtils
    {
        private static Dictionary<string, string> m_authorList;

        public static Dictionary<string, string> AuthorList
        {
            get {
                if (m_authorList == null)
                {
                    m_authorList = LoadAuthors();
                }
                return m_authorList;
            }
        }

        private static Dictionary<string, string> LoadAuthors()
        {
            var authors = new Dictionary<string, string>();
            foreach (Package.Asset current in PackageManager.FilterAssets(new Package.AssetType[] { UserAssetType.CustomAssetMetaData }))
            {
                PublishedFileId id = current.package.GetPublishedFileID();
                string publishedFileId = string.Concat(id.AsUInt64);
                if (!authors.ContainsKey(publishedFileId) && !current.package.packageAuthor.IsNullOrWhiteSpace() && current.isEnabled)
                {
                    if (ulong.TryParse(current.package.packageAuthor.Substring("steamid:".Length), out ulong authorID))
                    {
                        string author = new Friend(new UserID(authorID)).personaName;
                        authors.Add(publishedFileId, author);
                    }
                }
            }
            return authors;
        }
    }
}

