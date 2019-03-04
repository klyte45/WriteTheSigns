using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Overrides;

namespace Klyte.DynamicTextBoards.Utils
{
    public sealed class DTBResourceLoader : KlyteResourceLoader<DTBResourceLoader>
    {
        protected override string prefix => "Klyte.DynamicTextBoards.";
        public BoardGeneratorBuildings BoardGeneratorInstance => BoardGeneratorBuildings.instance;
        public BoardGeneratorRoadNodes BoardGeneratorRoadNodesInstance => BoardGeneratorRoadNodes.instance;
    }
}
