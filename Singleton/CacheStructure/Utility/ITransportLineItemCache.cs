namespace Klyte.WriteTheSigns.Rendering
{
    public interface ITransportLineItemCache : IItemCache
    {
        string Identifier { get; }
        FormatableString Name { get; }
    }

}
