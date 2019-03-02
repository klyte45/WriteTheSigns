using Klyte.Commons.Utils;

namespace Klyte.DynamicTextBoards.Utils
{
    public sealed class DTBResourceLoader : KlyteResourceLoader<DTBResourceLoader>
    {
        protected override string prefix => "Klyte.CustomAI.";
    }
}
