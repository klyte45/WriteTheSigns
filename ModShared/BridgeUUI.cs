extern alias UUI;

using ColossalFramework.Globalization;
using Klyte.Commons.Interfaces;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Tools;
using UUI::UnifiedUI.Helpers;

namespace Klyte.WriteTheSigns.ModShared
{
    internal class BridgeUUI : Commons.ModShared.BridgeUUI
    {
        public override void RegisterMod<U, C, T>(BasicIUserMod<U, C, T> modInstance)
        {
            base.RegisterMod(modInstance);
            RegisterTools();
        }

        public static void RegisterTools() => UUIHelpers.RegisterToolButton(
                    name: "K45_WTS_PICK_A_SEGMENT",
                    groupName: "Klyte45",
                    tooltip: "WTS: " + Locale.Get("K45_WTS_PICK_A_SEGMENT"),
                    tool: ToolsModifierControl.toolController.GetComponent<SegmentEditorPickerTool>(),
                    icon: KlyteResourceLoader.LoadTexture($"UI.Images.%K45_WTS_SegmentPickerIcon.png")
                    );
    }
}

