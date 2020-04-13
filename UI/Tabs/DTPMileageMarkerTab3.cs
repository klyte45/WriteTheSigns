using ColossalFramework.Globalization;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.Data;
using Klyte.DynamicTextProps.Libraries;
using Klyte.DynamicTextProps.Overrides;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwayMileage;

namespace Klyte.DynamicTextProps.UI
{

    internal class DTPMileageMarkerTab3 : DTPNoPropEditorTemplateTab<BoardGeneratorHighwayMileage, IBoardBunchContainer<CacheControl>, DTPHighwayMileageData, BoardDescriptorMileageMarkerXml, BoardTextDescriptorMileageMarkerXml, DTPLibTextMeshMileageMarker, DTPLibMileageMarkerGroup>
    {
        protected override BoardDescriptorMileageMarkerXml CurrentConfig
        {
            get => DTPHighwayMileageData.Instance.CurrentDescriptor;
            set => DTPHighwayMileageData.Instance.CurrentDescriptor = value;
        }

        protected override void BeforeCreatingTextScrollPanel() => KlyteMonoUtils.LimitWidth(m_uiHelperHS.AddCheckboxLocale("K45_DTP_USE_MILES", DTPHighwayMileageData.Instance.CurrentDescriptor.UseMiles, OnSetUseMiles).label, m_uiHelperHS.Self.width - 50);
        protected override void CleanDescriptor() => BoardGeneratorHighwayMileage.Instance.CleanDescriptor();
        protected override void GenerateDefaultModels() => GenerateDefaultSignModelAtLibrary();
        protected override TextType[] GetAvailableTextTypes() => AVAILABLE_TEXT_TYPES;
        protected override string GetFontLabelString() => Locale.Get("K45_DTP_FONT_MILEAGE_MARKES");
        protected override string GetLibLocaleEntry() => "K45_DTP_MILEAGE_MARKERS_LIB_TITLE";
        protected override string GetLocaleNameForContentTypes() => "K45_DTP_OWN_NAME_CONTENT_MM";

        private void OnSetUseMiles(bool value)
        {
            DTPHighwayMileageData.Instance.CurrentDescriptor.UseMiles = value;
            BoardGeneratorHighwayMileage.Instance.SoftReset();
        }

    }
}