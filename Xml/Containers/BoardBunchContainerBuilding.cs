namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorBuildings
    {
        public class BoardBunchContainerBuilding : IBoardBunchContainer<CacheControl, BasicRenderInformation>
        {
            public StopInformation[][] m_platformToLine;
            public uint m_linesUpdateFrame;
        }

        public struct StopInformation
        {
            public ushort m_stopId;
            public ushort m_lineId;
            public ushort m_nextStopBuilding;
            public ushort m_previousStopBuilding;
            public ushort m_destinationBuilding;
        }
    }
}