

using Klyte.Commons.Utils;
using System;
using UnityEngine;

namespace Klyte.DynamicTextBoards.ModShared
{
    public static class AdrEvents
    {
        public static event Action eventZeroMarkerBuildingChange;

        public static void TriggerZeroMarkerBuildingChange()
        {
            eventZeroMarkerBuildingChange?.Invoke();
        }
    }
}