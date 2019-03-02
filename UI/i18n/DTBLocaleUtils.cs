using ColossalFramework.Globalization;
using Klyte.Commons.i18n;
using Klyte.DynamicTextBoards.Utils;
using System;

namespace Klyte.DynamicTextBoards.i18n
{
    public class DTBLocaleUtils : KlyteLocaleUtils<DTBLocaleUtils, DTBResourceLoader>
    {
        public override string prefix => "KCAI_";

        protected override string packagePrefix => "Klyte.CustomAI";
    }
}
