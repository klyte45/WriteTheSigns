using ColossalFramework;
using ColossalFramework.UI;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static Klyte.DynamicTextProps.TextureAtlas.DTPCommonTextureAtlas;

namespace Klyte.DynamicTextProps.TextureAtlas
{
    public class DTPCommonTextureAtlas : TextureAtlasDescriptor<DTPCommonTextureAtlas, DTPResourceLoader, SpriteNames>
    {
        protected override string ResourceName => "UI.Images.sprites.png";
        protected override string CommonName => "KlyteDTPSprites";
        public enum SpriteNames
        {
            KDTPIcon, FontIcon, AutoNameIcon, AutoColorIcon, RemoveIcon, Load, _24hLineIcon, PerHourIcon, Reload, Save, Copy, Paste
        };
    }
}
