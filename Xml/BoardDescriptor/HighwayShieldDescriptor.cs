using Klyte.Commons.Interfaces;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public class HighwayShieldDescriptor : ILibable
    {
        [XmlIgnore]
        public TextParameterWrapper BackgroundImageParameter { get; private set; } = new TextParameterWrapper("image://K45_WTS_HWSHIELD_00");

        [XmlAttribute("name")]
        public string SaveName { get; set; }

        [XmlAttribute("backgroundImage")]
        public string BackgroundImage
        {
            get => BackgroundImageParameter?.ToString();
            set
            {
                var parameter = new TextParameterWrapper(value);
                BackgroundImageParameter = parameter.ParamType == TextParameterWrapper.ParameterType.IMAGE ? parameter : null;
            }
        }
        [XmlAttribute("backgroundColorIsFromHighway")]
        public bool BackgroundColorIsFromHighway { get; set; }
        [XmlElement("backgroundColor")]
        public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value.a < 1 ? Color.white : value; } 
        [XmlElement("textDescriptor")]
        public List<ImageLayerTextDescriptorXml> TextDescriptors { get; set; } = new List<ImageLayerTextDescriptorXml>();

        [XmlElement("overrideFontName")]
        public string FontName { get; internal set; }

        [XmlIgnore]
        internal ConfigurationSource m_configurationSource;
        private Color backgroundColor = Color.white;
    }
}
