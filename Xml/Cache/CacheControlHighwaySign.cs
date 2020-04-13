using Klyte.Commons.Utils;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using static Klyte.DynamicTextProps.Overrides.BoardGeneratorHighwaySigns;

namespace Klyte.DynamicTextProps.Overrides
{

    public class CacheControlHighwaySign : CacheControl
    {
        [XmlElement("descriptor")]
        public BoardDescriptorHigwaySignXml descriptor;
        [XmlIgnore]
        public Vector3 cachedPosition;
        [XmlIgnore]
        public Vector3 cachedRotation;

        public string Serialize()
        {
            var xmlser = new XmlSerializer(typeof(BoardDescriptorHigwaySignXml));
            var settings = new XmlWriterSettings { Indent = false };
            using var textWriter = new StringWriter();
            using var xw = XmlWriter.Create(textWriter, settings);
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            xmlser.Serialize(xw, descriptor, ns);
            return textWriter.ToString();
        }

        public void Deserialize(string s)
        {
            var xmlser = new XmlSerializer(typeof(BoardDescriptorHigwaySignXml));
            try
            {
                using TextReader tr = new StringReader(s);
                using var reader = XmlReader.Create(tr);
                if (xmlser.CanDeserialize(reader))
                {
                    descriptor = (BoardDescriptorHigwaySignXml)xmlser.Deserialize(reader);
                }
                else
                {
                    LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}");
                }
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog($"CAN'T DESERIALIZE BOARD DESCRIPTOR!\nText : {s}\n{e.Message}\n{e.StackTrace}");
            }

        }
    }


}
