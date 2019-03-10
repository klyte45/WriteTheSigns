using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Utils
{
    internal class DTBUtils : KlyteUtils
    {
        #region Logging
        public static void doLog(string format, params object[] args)
        {
            try
            {
                if (DynamicTextBoardsMod.debugMode)
                {
                    Console.WriteLine("DTBv" + DynamicTextBoardsMod.version + " " + format, args);
                }
            }
            catch
            {
                Debug.LogErrorFormat("DTBv" + DynamicTextBoardsMod.version + " Erro ao fazer log: {0} (args = {1})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }
        }
        public static void doErrorLog(string format, params object[] args)
        {
            try
            {
                Console.WriteLine("[ERR] DTBv" + DynamicTextBoardsMod.version + " " + format, args);
            }
            catch
            {
                Debug.LogErrorFormat("DTBv" + DynamicTextBoardsMod.version + " Erro ao logar ERRO!!!: {0} (args = [{1}])", format, args == null ? "" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }

        }
        #endregion


    }
}

