using System.Collections.Generic;
using static Klyte.Commons.Utils.SegmentUtils;

namespace Klyte.WriteTheSigns.Overrides
{
    public struct RoadIdentifier
    {
        public RoadIdentifier(ComparableRoad start, ComparableRoad end, ushort[] segments)
        {
            this.start = start;
            this.end = end;
            this.segments = segments;
        }

        public ComparableRoad start;
        public ComparableRoad end;
        public ushort[] segments;

        public static bool operator ==(RoadIdentifier id, RoadIdentifier other) => (other.start.ToString() == id.start.ToString() && other.end.ToString() == id.end.ToString()) || (other.end.ToString() == id.start.ToString() && other.start.ToString() == id.end.ToString());
        public static bool operator !=(RoadIdentifier id, RoadIdentifier other) => !(id == other);

        public override bool Equals(object obj)
        {
            if (!(obj is RoadIdentifier))
            {
                return false;
            }

            var identifier = (RoadIdentifier)obj;
            return EqualityComparer<ComparableRoad>.Default.Equals(start, identifier.start) &&
                   EqualityComparer<ComparableRoad>.Default.Equals(end, identifier.end);
        }

        public override int GetHashCode()
        {
            int hashCode = 1075529825;
            hashCode = (hashCode * -1521134295) + base.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<ComparableRoad>.Default.GetHashCode(start);
            hashCode = (hashCode * -1521134295) + EqualityComparer<ComparableRoad>.Default.GetHashCode(end);
            return hashCode;
        }
    }


}
