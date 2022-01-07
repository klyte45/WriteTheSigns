namespace Klyte.WriteTheSigns.Rendering
{
    public class CityTransportLineItemCache : ITransportLineItemCache
    {
        public ushort transportLineId;
        public long? Id { get => transportLineId; set => transportLineId = (ushort)(value ?? 0); }
        public FormatableString Name
        {
            get
            {
                if (name is null)
                {
                    identifier = WriteTheSignsMod.Controller.ConnectorTLM.GetLineName(new Utils.WTSLine() { lineId = transportLineId, regional = false });
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
                    identifier = WriteTheSignsMod.Controller.ConnectorTLM.GetLineIdString(new Utils.WTSLine() { lineId = transportLineId, regional = false });
                }
                return identifier;
            }
        }

        private FormatableString name;
        private string identifier;
        public void PurgeCache(CacheErasingFlags cacheToPurge, InstanceID refID)
        {
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
