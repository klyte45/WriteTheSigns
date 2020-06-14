using Klyte.Commons.Utils;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{
    public class BoardInstanceXml
    {
        [XmlIgnore]
        public Vector3 PropScale
        {
            get => Scale;//new Vector3(Scale.X, Scale.Y == 0 ? Scale.X : Scale.Y, Scale.Z == 0 ? Scale.X : Scale.Z);
            set => Scale = (Vector3Xml)value;
        }

        [XmlElement("position")]
        public Vector3Xml PropPosition { get; set; }

        [XmlElement("rotation")]
        public Vector3Xml PropRotation { get; set; }

        [XmlElement("scale")]
        public Vector3Xml Scale { get; set; } = (Vector3Xml)Vector3.one;
    }

}
