using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using System.Collections.Generic;

namespace Klyte.WriteTheCity.Utils
{
    public class PrefabUtils : SingletonLite<PrefabUtils>
    {
        private Dictionary<string, string> m_authorList;

        public Dictionary<string, string> AuthorList
        {
            get {
                if (m_authorList == null)
                {
                    m_authorList = LoadAuthors();
                }
                return m_authorList;
            }
        }

        private Dictionary<string, string> LoadAuthors()
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

