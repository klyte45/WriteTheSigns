using ColossalFramework;
using ColossalFramework.DataBinding;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using Klyte.Commons.Extensors;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Klyte.DynamicTextBoards;

namespace Klyte.Commons
{
    public static class CommonProperties 
    {
        public static bool DebugMode => DynamicTextBoardsMod.DebugMode;
        public static string Version => DynamicTextBoardsMod.Version;
        public static string ModName => DynamicTextBoardsMod.Instance.SimpleName;
        public static object Acronym => "DTB";
    }
}