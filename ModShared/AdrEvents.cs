

using Klyte.Commons.Utils;
using System;
using UnityEngine;

namespace Klyte.DynamicTextProps.ModShared
{
    public static class AdrEvents
    {
        public static event Action EventZeroMarkerBuildingChange;

        public static void TriggerZeroMarkerBuildingChange() => EventZeroMarkerBuildingChange?.Invoke();
    }
}