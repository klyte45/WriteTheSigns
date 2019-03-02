using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Klyte.DynamicTextBoards.Managers.BoardManager;

namespace Klyte.DynamicTextBoards.Utils
{
    internal class KCAIUtils : KlyteUtils
    {
        #region Logging
        public static void doLog(string format, params object[] args)
        {
            try
            {
                if (DynamicTextBoardsMod.debugMode)
                {
                    Debug.LogWarningFormat("KCAIv" + DynamicTextBoardsMod.version + " " + format, args);
                }
            }
            catch
            {
                Debug.LogErrorFormat("KCAIv" + DynamicTextBoardsMod.version + " Erro ao fazer log: {0} (args = {1})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }
        }
        public static void doErrorLog(string format, params object[] args)
        {
            try
            {
                Debug.LogWarningFormat("KCAIv" + DynamicTextBoardsMod.version + " " + format, args);
            }
            catch
            {
                Debug.LogErrorFormat("KCAIv" + DynamicTextBoardsMod.version + " Erro ao logar ERRO!!!: {0} (args = [{1}])", format, args == null ? "" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }

        }
        #endregion


    }
}

