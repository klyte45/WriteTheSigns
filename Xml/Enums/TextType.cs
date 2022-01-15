namespace Klyte.WriteTheSigns.Xml
{
    public enum TextType
    {
        None,
        OwnName,
        Fixed,
        StreetPrefix,
        StreetSuffix,
        StreetNameComplete,
        Mileage,
        District,
        Park,
        DistrictOrPark,
        ParkOrDistrict,
        LinesSymbols,
        Direction,
        DistanceFromReference,
        LastStopLine,
        HwShield,
        NextStopLine,
        PrevStopLine,
        PlatformNumber,
        PostalCode,
        GameSprite,
        LineIdentifier,
        ParameterizedText,
        TimeTemperature,
        ParameterizedGameSprite,
        ParameterizedGameSpriteIndexed,
        LineFullName,
        CityName,
        HwCodeShort,
        HwCodeLong,
        HwDettachedPrefix,
        HwIdentifierSuffix        
    }
    public enum TextContent
    {
        None,
        ParameterizedText,
        ParameterizedSpriteFolder,
        ParameterizedSpriteSingle,
        LinesSymbols,
        LinesNameList,
        HwShield,
        TimeTemperature
    }


}
