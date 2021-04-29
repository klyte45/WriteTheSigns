﻿using ColossalFramework.Globalization;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static ColossalFramework.UI.UITextureAtlas;

namespace Klyte.WriteTheSigns.Utils
{
    public static class WTSAtlasLoadingUtils
    {
        public const int MAX_SIZE_IMAGE_IMPORT = 400;
        public static void LoadAllImagesFromFolder(string folder, out List<SpriteInfo> spritesToAdd, out List<string> errors, bool addPrefix = true)
        {
            spritesToAdd = new List<SpriteInfo>();
            errors = new List<string>();
            LoadAllImagesFromFolderRef(folder, ref spritesToAdd, ref errors, addPrefix);
        }
        public static void LoadAllImagesFromFolderRef(string folder, ref List<SpriteInfo> spritesToAdd, ref List<string> errors, bool addPrefix)
        {
            foreach (var imgFile in Directory.GetFiles(folder, "*.png"))
            {
                var fileData = File.ReadAllBytes(imgFile);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(fileData))
                {
                    if (tex.width <= MAX_SIZE_IMAGE_IMPORT && tex.width <= MAX_SIZE_IMAGE_IMPORT)
                    {
                        var imgName = addPrefix ? $"K45_WTS_{Path.GetFileNameWithoutExtension(imgFile)}" : Path.GetFileNameWithoutExtension(imgFile);
                        spritesToAdd.Add(new SpriteInfo
                        {
                            border = new RectOffset(),
                            name = imgName,
                            texture = tex
                        });
                    }
                    else
                    {
                        errors.Add($"{Path.GetFileName(imgFile)}: {Locale.Get("K45_WTS_CUSTOMSPRITE_IMAGETOOLARGE")} (max: 400x400)");
                    }
                }
                else
                {
                    errors.Add($"{Path.GetFileName(imgFile)}: {Locale.Get("K45_WTS_CUSTOMSPRITE_FAILEDREADIMAGE")}");
                }
            }
        }
    }
}

