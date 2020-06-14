using SpriteFontPlus.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{

    public partial class BoardGeneratorBuildings
    {
        public class BoardBunchContainerBuilding : IBoardBunchContainer
        {
            public uint m_linesUpdateFrame;
            public PropInfo m_cachedProp;

            public Vector3? m_cachedPosition;
            public Vector3? m_cachedRotation;

            public bool HasAnyBoard() => true;

        }
    }
}