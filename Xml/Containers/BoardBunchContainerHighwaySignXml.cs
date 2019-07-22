﻿using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using Klyte.DynamicTextBoards.Libraries;
using static Klyte.Commons.Utils.XmlUtils;

namespace Klyte.DynamicTextBoards.Overrides
{

    public partial class BoardGeneratorHighwaySigns
    {
        public class BoardBunchContainerHighwaySignXml : IBoardBunchContainer<CacheControlHighwaySign, BasicRenderInformation>, ILibable
        {
            [XmlIgnore]
            public bool cached = false;
            [XmlElement("descriptors")]
            public ListWrapper<BoardDescriptorHigwaySignXml> Descriptors
            {
                get => new ListWrapper<BoardDescriptorHigwaySignXml> { listVal = m_boardsData.Select(x => x.descriptor).ToList() };
                set => m_boardsData = value.listVal.Select(x => new CacheControlHighwaySign { descriptor = x }).ToArray();
            }

            [XmlAttribute("saveName")]
            public string SaveName { get; set; }
        }

    }
}