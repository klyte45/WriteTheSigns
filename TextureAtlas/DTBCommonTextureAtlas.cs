using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextBoards.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Klyte.DynamicTextBoards.TextureAtlas
{
    public class DTBCommonTextureAtlas : TextureAtlasDescriptor<DTBCommonTextureAtlas, DTBResourceLoader>
    {
        protected override string ResourceName => "UI.Images.sprites.png";
        protected override string CommonName => "KlyteCAISprites";
        public override string[] SpriteNames => new string[] {
                    "ToolbarIconGroup6Hovered",    "ToolbarIconGroup6Focused",   "ToolbarIconGroup6Pressed",    "KlyteMenuIcon"
                };
    }
}
