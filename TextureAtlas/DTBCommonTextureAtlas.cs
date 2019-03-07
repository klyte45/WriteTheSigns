﻿using ColossalFramework;
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
        protected override string CommonName => "KlyteDTBSprites";
        public override string[] SpriteNames => new string[] {
                "KDTBIcon","FontIcon","AutoNameIcon","AutoColorIcon","RemoveUnwantedIcon","ConfigIcon","24hLineIcon", "PerHourIcon","AbsoluteMode","RelativeMode","Copy","Paste"
                };
    }
}
