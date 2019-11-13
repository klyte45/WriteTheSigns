using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using Klyte.Commons.Utils;
using Klyte.DynamicTextProps.TextureAtlas;
using System;
using UnityEngine;
using static ItemClass;
using static Klyte.DynamicTextProps.TextureAtlas.LineIconsTextureAtlas;

namespace Klyte.DynamicTextProps.Utils
{
    internal static class DTPHookable
    {
        public static Func<ushort, string> GetStreetFullName = (ushort idx) => NetManager.instance.GetSegmentName(idx);

        public static Func<ushort, string> GetStreetSuffix = (ushort idx) =>
        {
            string result;
            if ((NetManager.instance.m_segments.m_buffer[idx].m_flags & NetSegment.Flags.CustomName) != 0)
            {
                LogUtils.DoLog($"!UpdateMeshStreetSuffix Custom");
                InstanceID id = default;
                id.NetSegment = idx;
                result = Singleton<InstanceManager>.instance.GetName(id);
            }
            else
            {
                LogUtils.DoLog($"!UpdateMeshStreetSuffix NonCustom {NetManager.instance.m_segments.m_buffer[idx].m_nameSeed}");
                if (NetManager.instance.m_segments.m_buffer[idx].Info.m_netAI is RoadBaseAI ai)
                {
                    var randomizer = new Randomizer(NetManager.instance.m_segments.m_buffer[idx].m_nameSeed);
                    randomizer.Int32(12);
                    result = ReflectionUtils.RunPrivateMethod<string>(ai, "GenerateStreetName", randomizer);
                    //}
                }
                else
                {
                    result = "???";
                }
            }

            return result;
        };

        private static Vector2? m_cachedPos;
        private static readonly Color[] m_randomColors = { Color.black, Color.gray, Color.white, Color.red, new Color32(0xFF, 0x88, 0, 0xFf), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta };


        public static Func<ushort, Color> GetDistrictColor = (ushort districtId) => m_randomColors[districtId % m_randomColors.Length];
        public static Func<Vector2> GetStartPoint = () =>
        {
            if (m_cachedPos == null)
            {
                GameAreaManager.instance.GetStartTile(out int x, out int y);
                m_cachedPos = new Vector2((x - 2) * 1920, (y - 2) * 1920);
            }
            return m_cachedPos.GetValueOrDefault();
        };


        public static Func<ushort, Tuple<UITextureAtlas, string, Color, string>> GetLineLogoParameters = (ushort lineID) =>
         {
             Color lineColor = TransportManager.instance.GetLineColor(lineID);
             LineIconsTextureAtlas.LineIcon lineIcon;
             switch (TransportManager.instance.m_lines.m_buffer[lineID].Info.m_transportType)
             {
                 case TransportInfo.TransportType.Bus:
                     lineIcon = LineIcon.OvalIcon;
                     break;
                 case TransportInfo.TransportType.Metro:
                     lineIcon = LineIcon.SquareIcon;
                     break;
                 case TransportInfo.TransportType.Train:
                     lineIcon = LineIcon.CircleIcon;
                     break;
                 case TransportInfo.TransportType.Ship:
                     if (TransportManager.instance.m_lines.m_buffer[lineID].Info.m_vehicleType == VehicleInfo.VehicleType.Ferry)
                     {
                         lineIcon = LineIcon.S08StarIcon;
                     }
                     else
                     {
                         lineIcon = LineIcon.DiamondIcon;
                     }
                     break;
                 case TransportInfo.TransportType.Airplane:
                     if (TransportManager.instance.m_lines.m_buffer[lineID].Info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                     {
                         lineIcon = LineIcon.ParachuteIcon;
                     }
                     else
                     {
                         lineIcon = LineIcon.PentagonIcon;
                     }
                     break;
                 case TransportInfo.TransportType.Tram:
                     lineIcon = LineIcon.TrapezeIcon;
                     break;
                 case TransportInfo.TransportType.EvacuationBus:
                     lineIcon = LineIcon.CrossIcon;
                     break;
                 case TransportInfo.TransportType.Monorail:
                     lineIcon = LineIcon.RoundedSquareIcon;
                     break;
                 case TransportInfo.TransportType.Pedestrian:
                     lineIcon = LineIcon.MountainIcon;
                     break;
                 case TransportInfo.TransportType.TouristBus:
                     lineIcon = LineIcon.CameraIcon;
                     break;
                 default:
                     lineIcon = LineIcon.S05StarIcon;
                     break;
             }

             return Tuple.New(LineIconsTextureAtlas.instance.Atlas, lineIcon.ToString(), lineColor, lineID.ToString());
         };

        public static Func<ushort, ushort, string> GetStopName = (ushort stopId, ushort lineId) =>
        {
            ushort buildingID = DTPLineUtils.GetStopBuilding(stopId, lineId);

            if (buildingID > 0)
            {
                string name = BuildingUtils.GetBuildingName(buildingID, out _, out _);
                return name;
            }
            NetManager nm = Singleton<NetManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            NetNode nn = nm.m_nodes.m_buffer[stopId];
            Vector3 location = nn.m_position;
            if (DistrictManager.instance.GetPark(location) > 0)
            {
                return DistrictManager.instance.GetParkName(DistrictManager.instance.GetPark(location));
            }
            if (SegmentUtils.GetAddressStreetAndNumber(location, location, out int number, out string streetName) && !string.IsNullOrEmpty(streetName))
            {
                return streetName + ", " + number;
            }

            return "????";


        };


        public static Func<ushort, ushort, ushort> GetStopBuildingInternal = (ushort stopId, ushort lineId) =>
         {
             NetManager nm = Singleton<NetManager>.instance;
             BuildingManager bm = Singleton<BuildingManager>.instance;
             ushort tempBuildingId;
             Vector3 position = nm.m_nodes.m_buffer[stopId].m_position;

             SubService ss = TransportManager.instance.m_lines.m_buffer[lineId].Info.m_class.m_subService;

             if (ss != ItemClass.SubService.None)
             {
                 tempBuildingId = BuildingUtils.FindBuilding(position, 100f, ItemClass.Service.PublicTransport, ss, m_defaultAllowedVehicleTypes, Building.Flags.None, Building.Flags.Untouchable);
                 if (BuildingUtils.IsBuildingValidForStation(true, bm, tempBuildingId))
                 {
                     return tempBuildingId;
                 }
             }

             tempBuildingId = BuildingUtils.FindBuilding(position, 100f, ItemClass.Service.PublicTransport, ItemClass.SubService.None, m_defaultAllowedVehicleTypes, Building.Flags.None, Building.Flags.Untouchable);
             if (BuildingUtils.IsBuildingValidForStation(true, bm, tempBuildingId))
             {
                 return tempBuildingId;
             }

             return 0;

         };

        private static readonly TransferManager.TransferReason[] m_defaultAllowedVehicleTypes = {
            TransferManager.TransferReason.Blimp ,
            TransferManager.TransferReason.CableCar ,
            TransferManager.TransferReason.Ferry ,
            TransferManager.TransferReason.MetroTrain ,
            TransferManager.TransferReason.Monorail ,
            TransferManager.TransferReason.PassengerTrain ,
            TransferManager.TransferReason.PassengerPlane ,
            TransferManager.TransferReason.PassengerShip ,
            TransferManager.TransferReason.Tram ,
            TransferManager.TransferReason.Bus
        };


    }
}

