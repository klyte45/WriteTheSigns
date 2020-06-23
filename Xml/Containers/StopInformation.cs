using Klyte.WriteTheSigns.Utils;

namespace Klyte.WriteTheSigns.Xml
{

    public struct StopInformation
    {
        public ushort m_stopId;
        public ushort m_lineId;
        public ushort m_nextStopId;
        public ushort m_previousStopId;
        public ushort m_destinationId;
        public string m_destinationString;
        public DestinationType destinationType;

        public ushort NextStopBuildingId => WTSBuildingDataCaches.GetStopBuilding(m_nextStopId, m_lineId);
        public ushort PrevStopBuildingId => WTSBuildingDataCaches.GetStopBuilding(m_previousStopId, m_lineId);
        public ushort DestinationBuildingId => WTSBuildingDataCaches.GetStopBuilding(m_destinationId, m_lineId);

        public override string ToString() => $"StopInformation(S={m_stopId},L={m_lineId},N={m_nextStopId},P={m_previousStopId},D={m_destinationId})";
    }

    public enum DestinationType
    {
        Stop,
        Building,
        District,
        Park
    }
}