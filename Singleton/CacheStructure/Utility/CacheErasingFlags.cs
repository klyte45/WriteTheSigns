namespace Klyte.WriteTheSigns.Rendering
{
    public enum CacheErasingFlags
    {
        SegmentNameParam = 1,
        PostalCodeParam = 1 << 1,
        ParkName = 1 << 2,
        DistrictName = 1 << 3,
        SegmentSize = 1 << 4,
        BuildingName = 1 << 5,
        LineName = 1 << 6,
        VehicleParameters = 1 << 7,
        ParkArea = 1 << 8,
        DistrictArea = 1 << 9,
        BuildingPosition = 1 << 10,
        NodeParameter = 1 << 11,
        LineId = 1 << 12,
        OutsideConnections = 1 << 13,
    }

    public static class CacheErasingFlagsExtensions
    {
        public static bool Has(this CacheErasingFlags src, CacheErasingFlags test) => (src & test) != 0;
    }
}
