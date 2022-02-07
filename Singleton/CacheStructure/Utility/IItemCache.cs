using Klyte.Commons.Interfaces;

namespace Klyte.WriteTheSigns.Rendering
{
    public interface IItemCache : IIdentifiable
    {
        void PurgeCache(CacheErasingFlags cacheToPurge, InstanceID refId);
    }

}
