using Klyte.Commons.Utils;
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class BoardGeneratorHighwaySigns
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
                XmlSerializer xmlser = new XmlSerializer(typeof(BoardDescriptorHigwaySignXml));
                XmlWriterSettings settings = new XmlWriterSettings { Indent = false };
                using StringWriter textWriter = new StringWriter();
                using XmlWriter xw = XmlWriter.Create(textWriter, settings);
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                xmlser.Serialize(xw, descriptor, ns);
                return textWriter.ToString();
            }

            public void Deserialize(string s)
            {
                XmlSerializer xmlser = new XmlSerializer(typeof(BoardDescriptorHigwaySignXml));
                try
                {
                    using TextReader tr = new StringReader(s);
                    using XmlReader reader = XmlReader.Create(tr);
                    if (xmlser.CanDeserialize(reader))
                    {
                        descriptor = (BoardDescriptorHigwaySignXml) xmlser.Deserialize(reader);
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
}
