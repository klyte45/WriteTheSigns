namespace Klyte.WriteTheSigns.Rendering
{

    public class IntercityTransportLineItemCache : ITransportLineItemCache
    {
        public ushort nodeId;
        public long? Id { get => nodeId; set => nodeId = (ushort)(value ?? 0); }

        public FormatableString Name
        {
            get
            {
                if (name is null)
                {
                    identifier = WriteTheSignsMod.Controller.ConnectorTLM.GetLineName(new Utils.WTSLine() { lineId = nodeId, regional = true });
                }
                return name;
            }
        }

        public string Identifier
        {
            get
            {
                if (identifier is null)
                {
                    identifier = WriteTheSignsMod.Controller.ConnectorTLM.GetLineIdString(new Utils.WTSLine() { lineId = nodeId, regional = true });
                }
                return identifier;
            }
        }

        private FormatableString name;
        private string identifier;
        public void PurgeCache(CacheErasingFlags cacheToPurge, InstanceID refID)
        {
            if (cacheToPurge.Has(CacheErasingFlags.NodeParameter))
            {
                name = null;
                identifier = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.LineName))
            {
                name = null;
            }
            if (cacheToPurge.Has(CacheErasingFlags.LineId))
            {
                identifier = null;
            }
        }
    }

}
