using ColossalFramework.UI;
using Klyte.Commons.Utils;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Xml
{
    public partial class BoardTextDescriptorGeneralXml
    {
        public class SubItemSettings
        {
            [XmlAttribute("subItemsPerRow")]
            public int SubItemsPerRow { get => m_subItemsPerRow; set => m_subItemsPerRow = Math.Max(1, Math.Min(value, 10)); }
            [XmlAttribute("subItemsPerColumn")]
            public int SubItemsPerColumn { get => m_subItemsPerColumn; set => m_subItemsPerColumn = Math.Max(1, Math.Min(value, 10)); }

            [XmlAttribute("verticalFirst")]
            public bool VerticalFirst { get; set; }
            [XmlAttribute("verticalAlign")]
            public UIVerticalAlignment VerticalAlign { get; set; } = UIVerticalAlignment.Top;


            [XmlElement("subItemSpacing")]
            public Vector2Xml SubItemSpacing { get; set; } = new Vector2Xml();
            [XmlIgnore]
            private int m_subItemsPerColumn;
            [XmlIgnore]
            private int m_subItemsPerRow;
        }
    }

}

