using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;

namespace Klyte.DynamicTextBoards.Utils
{
    public sealed class DTBResourceLoader : KlyteResourceLoader<DTBResourceLoader>
    {
        protected override string prefix => "Klyte.CustomAI.";
        public BoardGeneratorBuildings BoardGeneratorInstance => BoardGeneratorBuildings.instance;
    }
}
