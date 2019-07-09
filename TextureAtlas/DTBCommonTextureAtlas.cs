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
using static Klyte.DynamicTextBoards.TextureAtlas.DTBCommonTextureAtlas;

namespace Klyte.DynamicTextBoards.TextureAtlas
{
    public class DTBCommonTextureAtlas : TextureAtlasDescriptor<DTBCommonTextureAtlas, DTBResourceLoader, SpriteNames>
    {
        protected override string ResourceName => "UI.Images.sprites.png";
        protected override string CommonName => "KlyteDTBSprites";
        public enum SpriteNames
        {
            KDTBIcon, FontIcon, AutoNameIcon, AutoColorIcon, RemoveIcon, Load, _24hLineIcon, PerHourIcon, Reload, Save, Copy, Paste
        };
    }
}
