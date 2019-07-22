namespace Klyte.DynamicTextBoards.Overrides
{
    public abstract class CacheControlSerializer<CCS, CC> where CC : CacheControl where CCS : CacheControlSerializer<CCS, CC>, new()
    {
        protected CC cc;


        public virtual void Deserialize(string input)
        {
        }
        public virtual string Serialize() => null;

        public static CCS New(CC sign)
        {
            return new CCS
            {
                cc = sign
            };
        }

    }


}
