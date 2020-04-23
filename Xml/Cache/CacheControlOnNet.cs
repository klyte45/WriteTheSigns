﻿using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Xml
{

    public class CacheControlOnNet 
    {
        [XmlElement("descriptor")]
        public BoardDescriptorOnNetXml descriptor;
        [XmlIgnore]
        public Vector3 cachedPosition;
        [XmlIgnore]
        public Vector3 cachedRotation;
    }


}
