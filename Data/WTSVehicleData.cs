﻿using Klyte.WriteTheSigns.Xml;
using System.Xml.Serialization;

namespace Klyte.WriteTheSigns.Data
{

    [XmlRoot("WTSVehicleData")]
    public class WTSVehicleData : WTSBaseData<WTSVehicleData, BoardBunchContainerBuilding[]>
    {
        public override int ObjArraySize => VehicleManager.MAX_VEHICLE_COUNT;

        public override string SaveId => "K45_WTS_WTSVehicleData";

        public override int BoardCount => 1;

        public override int SubBoardCount => 1;

        public void CleanCache() => ResetBoards();
    }

}