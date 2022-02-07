using Klyte.WriteTheSigns.Rendering;

namespace Klyte.WriteTheSigns.Xml
{
    public class BoardPreviewInstanceXml : BoardInstanceXml
    {
        public string m_currentText = "TEST TEXT";
        public string m_overrideText = null;
        public BoardDescriptorGeneralXml Descriptor { get; internal set; } = new BoardDescriptorGeneralXml();

        public override PrefabInfo TargetAssetParameter => Descriptor?.CachedProp;

        public override TextRenderingClass RenderingClass => Descriptor?.m_allowedRenderClass ?? TextRenderingClass.PlaceOnNet;

        public override string DescriptorOverrideFont => Descriptor?.FontName;

        public override TextParameterWrapper GetParameter(int idx) => null;
    }

}
