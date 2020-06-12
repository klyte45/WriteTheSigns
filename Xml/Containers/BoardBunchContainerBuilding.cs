using Klyte.WriteTheSigns.Utils;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{

    public partial class BoardGeneratorBuildings
    {
        public class BoardBunchContainerBuilding : IBoardBunchContainer
        {
            public StopInformation[][] m_platformToLine;
            public uint m_linesUpdateFrame;
            public PropInfo m_cachedProp;

            public Vector3? m_cachedPosition;
            public Vector3? m_cachedRotation;

            public bool HasAnyBoard() => true;
        }

        public struct StopInformation
        {
            public ushort m_stopId;
            public ushort m_lineId;
            public ushort m_nextStopId;
            public ushort m_previousStopId;
            public ushort m_destinationId;

            public ushort NextStopBuildingId => WTSBuildingDataCaches.GetStopBuilding(m_nextStopId, m_lineId);
            public ushort PrevStopBuildingId => WTSBuildingDataCaches.GetStopBuilding(m_previousStopId, m_lineId);
            public ushort DestinationBuildingId => WTSBuildingDataCaches.GetStopBuilding(m_destinationId, m_lineId);
        }
    }
}