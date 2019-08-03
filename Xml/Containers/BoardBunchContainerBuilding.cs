namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorBuildings
    {
        public class BoardBunchContainerBuilding : IBoardBunchContainer<CacheControl, BasicRenderInformation>
        {
            public ushort[][] m_platformToLine;
            public ushort[] m_ordenedLines;
            public uint m_linesUpdateFrame;

        }
    }
}