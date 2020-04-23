namespace Klyte.WriteTheSigns.Xml
{

    public partial class BoardGeneratorBuildings
    {
        public class BoardBunchContainerBuilding : IBoardBunchContainer
        {
            public StopInformation[][] m_platformToLine;
            public uint m_linesUpdateFrame;

            public bool HasAnyBoard() => false;
        }

        public struct StopInformation
        {
            public ushort m_stopId;
            public ushort m_lineId;
            public ushort m_nextStopId;
            public ushort m_previousStopId;
            public ushort m_destinationId;
        }
    }
}