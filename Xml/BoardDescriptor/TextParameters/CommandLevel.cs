using ColossalFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Klyte.WriteTheSigns.Xml
{
    internal class CommandLevel
    {

        public Enum defaultValue;
        public Dictionary<Enum, CommandLevel> nextLevelOptions;
        public string regexValidValues;
        public CommandLevel nextLevelByRegex;
        public string descriptionKey;

        public int level;

        public void ParseFormatting(string[] relativeParams, ref string numberFormat, ref string stringFormat, ref string prefix, ref string suffix)
        {
            if (this == m_numberFormatFloat || this == m_numberFormatInt)
            {
                if (relativeParams.Length >= 1)
                {
                    numberFormat = relativeParams[0];
                    if (relativeParams.Length >= 2)
                    {
                        prefix = relativeParams[1];
                        if (relativeParams.Length >= 3)
                        {
                            suffix = relativeParams[2];
                        }
                    }
                }
            }
            if (this == m_stringFormat)
            {
                if (relativeParams.Length >= 1)
                {
                    stringFormat = relativeParams[0];
                    if (relativeParams.Length >= 2)
                    {
                        prefix = relativeParams[1];
                        if (relativeParams.Length >= 3)
                        {
                            suffix = relativeParams[2];
                        }
                    }
                }
            }
            if (this == m_appendPrefix)
            {
                if (relativeParams.Length >= 1)
                {
                    prefix = relativeParams[0];
                    if (relativeParams.Length >= 2)
                    {
                        suffix = relativeParams[1];
                    }
                }
            }
        }

        public static string[] GetParameterPath(string input) => Regex.Split(input, @"(?<!\\)/").Select(x => x.Replace("\\/", "/")).ToArray();
        public static string FromParameterPath(IEnumerable<string> path) => string.Join("/", path.Select(x => Regex.Replace(x, @"([^\\])/|^/", "$1\\/")).ToArray()) + "/";
        public static string ToLocaleVar(Enum e) => $"{e.GetType().Name}.{e.ToString()}";

        public const string PROTOCOL_VARIABLE = "var://";

        public static readonly CommandLevel m_appendSuffix = new CommandLevel
        {
            descriptionKey = "COMMON_SUFFIX",
            regexValidValues = ".*",
            nextLevelByRegex = m_endLevel
        };
        public static readonly CommandLevel m_appendPrefix = new CommandLevel
        {
            descriptionKey = "COMMON_PREFIX",
            regexValidValues = ".*",
            nextLevelByRegex = m_appendSuffix
        };
        public static readonly CommandLevel m_numberFormatFloat = new CommandLevel
        {
            descriptionKey = "COMMON_NUMBERFORMAT_FLOAT",
            regexValidValues = ".*",
            nextLevelByRegex = m_appendPrefix
        };
        public static readonly CommandLevel m_numberFormatInt = new CommandLevel
        {
            descriptionKey = "COMMON_NUMBERFORMAT_INT",
            regexValidValues = ".*",
            nextLevelByRegex = m_appendPrefix
        };
        public static readonly CommandLevel m_stringFormat = new CommandLevel
        {
            descriptionKey = "COMMON_STRINGFORMAT",
            regexValidValues = "[ULA]{0,2}",
            nextLevelByRegex = m_appendPrefix
        };
        public static readonly CommandLevel m_endLevel = new CommandLevel
        {
        };
        private static readonly Dictionary<Enum, CommandLevel> commandTree = ReadCommandTree();

        private static Dictionary<Enum, CommandLevel> ReadCommandTree()
        {
            Dictionary<Enum, CommandLevel> result = new Dictionary<Enum, CommandLevel>();
            foreach (var value in Enum.GetValues(typeof(VariableType)).Cast<VariableType>())
            {
                if (value == 0)
                {
                    continue;
                }

                result[value] = value.GetCommandTree();
            }
            return result;
        }

        internal static CommandLevel OnFilterParamByText(string inputText, out string currentLocaleDesc)
        {
            if ((inputText?.Length ?? 0) >= 4 && inputText.StartsWith(PROTOCOL_VARIABLE))
            {
                var parameterPath = GetParameterPath(inputText.Substring(PROTOCOL_VARIABLE.Length));
                return IterateInCommandTree(out currentLocaleDesc, parameterPath, null, new CommandLevel
                {
                    descriptionKey = "_VarLevelRoot",
                    nextLevelOptions = commandTree,
                    defaultValue = VariableType.Invalid
                }, 0);
            }
            else
            {
                currentLocaleDesc = null;
                return null;
            }
        }

        private static CommandLevel IterateInCommandTree(out string currentLocaleDesc, string[] parameterPath, Enum levelKey, CommandLevel currentLevel, int level)
        {
            if (currentLevel is null)
            {
                currentLocaleDesc = null;
                return null;
            }
            if (level < parameterPath.Length - 1)
            {
                if (currentLevel.defaultValue != null)
                {
                    Enum varType = currentLevel.defaultValue;
                    try
                    {
                        varType = (Enum)Enum.Parse(varType.GetType(), parameterPath[level]);
                    }
                    catch
                    {

                    }
                    if (varType != currentLevel.defaultValue && currentLevel.nextLevelOptions.ContainsKey(varType))
                    {
                        return IterateInCommandTree(out currentLocaleDesc, parameterPath, varType, currentLevel.nextLevelOptions[varType], level + 1);
                    }
                }
                else
                {
                    if (!currentLevel.regexValidValues.IsNullOrWhiteSpace())
                    {
                        if (Regex.IsMatch(parameterPath[level], $"^{currentLevel.regexValidValues}$"))
                        {
                            if (currentLevel.nextLevelByRegex != null)
                            {
                                return IterateInCommandTree(out currentLocaleDesc, parameterPath, null, currentLevel.nextLevelByRegex, level + 1);
                            }
                        }
                    }
                }
            }
            currentLocaleDesc = !(currentLevel.descriptionKey is null)
                ? currentLevel.descriptionKey
                : !(levelKey is null)
                    ? CommandLevel.ToLocaleVar(levelKey)
                    : !(currentLevel.defaultValue is null)
                        ? CommandLevel.ToLocaleVar(currentLevel.defaultValue)
                        : null;
            currentLevel.level = level;
            return currentLevel;
        }

    }
}
