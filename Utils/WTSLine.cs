using Klyte.WriteTheSigns.Xml;

namespace Klyte.WriteTheSigns.Utils
{
    internal struct WTSLine
    {
        public int lineId;
        public bool regional;

        public WTSLine(int lineId, bool regional)
        {
            this.lineId = lineId;
            this.regional = regional;
        }

        public WTSLine(StopInformation stop) : this(stop.m_lineId, stop.m_regionalLine) { }

        public bool ZeroLine => lineId == 0 && !regional;

        internal static WTSLine FromRefID(int refId) => refId < 256 ? new WTSLine(refId, false) : new WTSLine(refId - 256, true);
        internal int ToRefId() => regional ? 256 + lineId : lineId;
        internal uint GetUniqueStopId(ushort stopId) => (uint)(ToRefId() << 16) | stopId;

        public override string ToString() => $"{lineId}@{(regional ? "REG" : "CITY")}";
    }
}

