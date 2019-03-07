using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.UI;
using Klyte.DynamicTextBoards.i18n;
using Klyte.DynamicTextBoards.TextureAtlas;
using Klyte.DynamicTextBoards.UI;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion("0.0.0.*")]
namespace Klyte.DynamicTextBoards
{
    public class DynamicTextBoardsMod : BasicIUserMod<DynamicTextBoardsMod, DTBLocaleUtils, DTBResourceLoader, MonoBehaviour, DTBCommonTextureAtlas, DTBPanel>
    {
        public DynamicTextBoardsMod()
        {
            Construct();
        }
        protected override ModTab? Tab => ModTab.DynamicTextBoards;

        public override string SimpleName => "Klyte's Dynamic Text Boards";

        public override string Description => "This mod allows creating dynamic text boards in the city";

        public override void doErrorLog(string fmt, params object[] args)
        {
            DTBUtils.doErrorLog(fmt, args);
        }

        public override void doLog(string fmt, params object[] args)
        {
            DTBUtils.doLog(fmt, args);
        }

        public override void LoadSettings()
        {
        }



        public override void TopSettingsUI(UIHelperExtension ext)
        {
        }

        public override void OnReleased()
        {
            base.OnReleased();
        }
    }
}
