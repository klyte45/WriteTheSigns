using ColossalFramework;
using Klyte.Commons.UI.Sprites;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ItemClass;

namespace Klyte.WriteTheSigns.ModShared
{
    internal class BridgeTLMFallback : IBridgeTLM
    {
        public override Tuple<string, Color, string> GetLineLogoParameters(WTSLine lineObj)
        {
            if (!lineObj.regional)
            {
                Color lineColor = TransportManager.instance.GetLineColor((ushort)lineObj.lineId);
                LineIconSpriteNames lineIcon;
                switch (TransportManager.instance.m_lines.m_buffer[lineObj.lineId].Info.m_transportType)
                {
                    case TransportInfo.TransportType.Bus:
                        lineIcon = LineIconSpriteNames.K45_HexagonIcon;
                        break;
                    case TransportInfo.TransportType.Trolleybus:
                        lineIcon = LineIconSpriteNames.K45_OvalIcon;
                        break;
                    case TransportInfo.TransportType.Helicopter:
                        lineIcon = LineIconSpriteNames.K45_S05StarIcon;
                        break;
                    case TransportInfo.TransportType.Metro:
                        lineIcon = LineIconSpriteNames.K45_SquareIcon;
                        break;
                    case TransportInfo.TransportType.Train:
                        lineIcon = LineIconSpriteNames.K45_CircleIcon;
                        break;
                    case TransportInfo.TransportType.Ship:
                        if (TransportManager.instance.m_lines.m_buffer[lineObj.lineId].Info.m_vehicleType == VehicleInfo.VehicleType.Ferry)
                        {
                            lineIcon = LineIconSpriteNames.K45_S08StarIcon;
                        }
                        else
                        {
                            lineIcon = LineIconSpriteNames.K45_DiamondIcon;
                        }
                        break;
                    case TransportInfo.TransportType.Airplane:
                        if (TransportManager.instance.m_lines.m_buffer[lineObj.lineId].Info.m_vehicleType == VehicleInfo.VehicleType.Blimp)
                        {
                            lineIcon = LineIconSpriteNames.K45_ParachuteIcon;
                        }
                        else
                        {
                            lineIcon = LineIconSpriteNames.K45_PentagonIcon;
                        }
                        break;
                    case TransportInfo.TransportType.Tram:
                        lineIcon = LineIconSpriteNames.K45_TrapezeIcon;
                        break;
                    case TransportInfo.TransportType.EvacuationBus:
                        lineIcon = LineIconSpriteNames.K45_CrossIcon;
                        break;
                    case TransportInfo.TransportType.Monorail:
                        lineIcon = LineIconSpriteNames.K45_RoundedSquareIcon;
                        break;
                    case TransportInfo.TransportType.Pedestrian:
                        lineIcon = LineIconSpriteNames.K45_MountainIcon;
                        break;
                    case TransportInfo.TransportType.TouristBus:
                        lineIcon = LineIconSpriteNames.K45_CameraIcon;
                        break;
                    default:
                        lineIcon = LineIconSpriteNames.K45_S05StarIcon;
                        break;
                }

                return Tuple.New(KlyteResourceLoader.GetDefaultSpriteNameFor(lineIcon), lineColor, TransportManager.instance.m_lines.m_buffer[lineObj.lineId].m_lineNumber.ToString());
            }
            else
            {
                ref NetNode node = ref NetManager.instance.m_nodes.m_buffer[lineObj.lineId];
                return Tuple.New(KlyteResourceLoader.GetDefaultSpriteNameFor(LineIconSpriteNames.K45_S10StarIcon), Color.gray, lineObj.lineId.ToString("00\n000"));
            }
        }

        public override string GetStopName(ushort stopId, WTSLine lineObj) => GetStopName(stopId, lineObj, out _, out _, out _);


        private string GetStopName(ushort stopId, WTSLine lineObj, out ushort buildingID, out ushort parkID, out ushort districtID)
        {
            if (stopId == 0)
            {
                buildingID = 0;
                parkID = 0;
                districtID = 0;
                return "";
            }

            buildingID = WTSBuildingDataCaches.GetStopBuilding(stopId, lineObj);

            if (buildingID > 0)
            {
                string name = BuildingUtils.GetBuildingName(buildingID, out _, out _);
                parkID = 0;
                districtID = 0;
                return name;
            }
            NetManager nm = Singleton<NetManager>.instance;
            NetNode nn = nm.m_nodes.m_buffer[stopId];
            Vector3 location = nn.m_position;
            parkID = DistrictManager.instance.GetPark(location);
            if (parkID > 0)
            {
                districtID = 0;
                return DistrictManager.instance.GetParkName(parkID);
            }
            districtID = DistrictManager.instance.GetDistrict(location);
            if (districtID > 0)
            {
                return DistrictManager.instance.GetDistrictName(districtID);
            }
            return WriteTheSignsMod.Controller.ConnectorADR.GetAddressStreetAndNumber(location, location, out int number, out string streetName) && !string.IsNullOrEmpty(streetName)
                ? streetName + ", " + number
                : "????";
        }


        public override ushort GetStopBuildingInternal(ushort stopId, WTSLine lineObj)
        {
            NetManager nm = Singleton<NetManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            ushort tempBuildingId;
            if (stopId > nm.m_nodes.m_buffer.Length)
            {
                return stopId;
            }

            Vector3 position = nm.m_nodes.m_buffer[stopId].m_position;

            SubService ss = lineObj.regional ? TransportManager.instance.m_lines.m_buffer[lineObj.lineId].Info.m_class.m_subService : (NetManager.instance.m_nodes.m_buffer[stopId].Info).m_class.m_subService;

            if (ss != ItemClass.SubService.None)
            {
                tempBuildingId = BuildingUtils.FindBuilding(position, 100f, ItemClass.Service.PublicTransport, ss, m_defaultAllowedVehicleTypes, Building.Flags.None, Building.Flags.None);

                while (BuildingManager.instance.m_buildings.m_buffer[tempBuildingId].m_parentBuilding != 0)
                {
                    tempBuildingId = BuildingManager.instance.m_buildings.m_buffer[tempBuildingId].m_parentBuilding;
                }
                if (BuildingUtils.IsBuildingValidForStation(true, bm, tempBuildingId))
                {
                    return tempBuildingId;
                }
            }

            tempBuildingId = BuildingUtils.FindBuilding(position, 100f, ItemClass.Service.PublicTransport, ItemClass.SubService.None, m_defaultAllowedVehicleTypes, Building.Flags.None, Building.Flags.None);
            while (BuildingManager.instance.m_buildings.m_buffer[tempBuildingId].m_parentBuilding != 0)
            {
                tempBuildingId = BuildingManager.instance.m_buildings.m_buffer[tempBuildingId].m_parentBuilding;
            }
            if (BuildingUtils.IsBuildingValidForStation(true, bm, tempBuildingId))
            {
                return tempBuildingId;
            }

            tempBuildingId = BuildingUtils.FindBuilding(position, 100f, ItemClass.Service.Road, ItemClass.SubService.None, null, Building.Flags.None, Building.Flags.None);
            while (BuildingManager.instance.m_buildings.m_buffer[tempBuildingId].m_parentBuilding != 0)
            {
                tempBuildingId = BuildingManager.instance.m_buildings.m_buffer[tempBuildingId].m_parentBuilding;
            }
            if (BuildingUtils.IsBuildingValidForStation(true, bm, tempBuildingId))
            {
                return tempBuildingId;
            }

            return 0;

        }

        public override string GetLineSortString(WTSLine lineObj)
        {
            if (lineObj.regional)
            {
                ref NetNode tl = ref NetManager.instance.m_nodes.m_buffer[lineObj.lineId];
                return (((int)tl.Info.m_class.m_subService << 16) + lineObj.lineId).ToString("D8");
            }
            else
            {
                ref TransportLine tl = ref TransportManager.instance.m_lines.m_buffer[lineObj.lineId];
                return (((int)tl.Info.m_class.m_subService << 16) + tl.m_lineNumber).ToString("D8");
            }
        }

        public override string GetVehicleIdentifier(ushort vehicleId) => vehicleId.ToString("D5");
        public override string GetLineIdString(WTSLine lineObj) => lineObj.regional ? lineObj.lineId.ToString() : TransportManager.instance.m_lines.m_buffer[lineObj.lineId].m_lineNumber.ToString();
        public override void MapLineDestinations(WTSLine lineObj)
        {
            CalculatePath(lineObj, out ushort startStation, out ushort endStation);
            FillStops(lineObj, new List<BridgeTLM.DestinationPoco>{
                new BridgeTLM.DestinationPoco{ stopId = startStation},
                new BridgeTLM.DestinationPoco{ stopId = endStation}
            });
        }

        private enum NamingType
        {
            STREET,
            DISTRICT,
            PARK,
            BUILDING
        }

        private void CalculatePath(WTSLine lineObj, out ushort startStation, out ushort endStation)
        {
            ushort firstStop;
            ushort nextStop;
            if (!lineObj.regional)
            {
                ref TransportLine t = ref Singleton<TransportManager>.instance.m_lines.m_buffer[lineObj.lineId];
                if ((t.m_flags & TransportLine.Flags.Complete) == TransportLine.Flags.None)
                {
                    startStation = 0;
                    endStation = 0;
                    return;
                }
                firstStop = nextStop = t.m_stops;
            }
            else
            {
                firstStop = nextStop = (ushort)lineObj.lineId;
            }
            var stations = new List<Tuple<NamingType, string, ushort>>();
            do
            {
                NetNode stopNode = NetManager.instance.m_nodes.m_buffer[nextStop];
                string stationName = GetStopName(nextStop, lineObj, out ushort buildingId, out ushort parkId, out ushort districtId);
                var tuple = Tuple.New(buildingId > 0 ? NamingType.BUILDING : parkId > 0 ? NamingType.PARK : districtId > 0 ? NamingType.DISTRICT : NamingType.STREET, stationName, nextStop);
                stations.Add(tuple);
                nextStop = TransportLine.GetNextStop(nextStop);
            } while (nextStop != firstStop && nextStop != 0);

            var idxStations = stations.Select((x, y) => Tuple.New(y, x.First, x.Second, x.Third)).OrderByDescending(x => x.Second).ToList();

            int targetStart = 0;
            int mostRelevantEndIdx = -1;
            int j = 0;
            int maxDistanceEnd = (int)(idxStations.Count / 8f + 0.5f);
            do
            {
                Tuple<int, NamingType, string> peerCandidate = idxStations.Where(x => x.Third != idxStations[j].Third && Math.Abs((x.First < idxStations[j].First ? x.First + idxStations.Count : x.First) - idxStations.Count / 2 - idxStations[j].First) <= maxDistanceEnd).OrderByDescending(x => x.Second).FirstOrDefault();
                if (peerCandidate != null && (mostRelevantEndIdx == -1 || stations[mostRelevantEndIdx].First < peerCandidate.Second))
                {
                    targetStart = j;
                    mostRelevantEndIdx = peerCandidate.First;
                }
                j++;
            } while (j < idxStations.Count && idxStations[j].Second == idxStations[0].Second);


            if (mostRelevantEndIdx >= 0)
            {
                startStation = idxStations[targetStart].Fourth;
                endStation = idxStations[mostRelevantEndIdx].Fourth;
            }
            else
            {
                startStation = idxStations[0].Fourth;
                endStation = 0;
            }
        }

        public override WTSLine GetVehicleLine(ushort vehicleId)
        {
            ref Vehicle[] buffer7 = ref VehicleManager.instance.m_vehicles.m_buffer;
            ref Vehicle targetVehicle7 = ref buffer7[buffer7[vehicleId].GetFirstVehicle(vehicleId)];
            return new WTSLine(targetVehicle7.m_transportLine, false);
        }

        public override WTSLine GetStopLine(ushort stopId) => new WTSLine(NetManager.instance.m_nodes.m_buffer[stopId].m_transportLine, false);
        internal override string GetLineName(WTSLine lineObj) => lineObj.regional ? "" : TransportManager.instance.GetLineName((ushort)lineObj.lineId);
        internal override Color GetLineColor(WTSLine lineObj) => lineObj.regional ? Color.white: TransportManager.instance.GetLineColor((ushort)lineObj.lineId);

        private readonly TransferManager.TransferReason[] m_defaultAllowedVehicleTypes = {
            TransferManager.TransferReason.Blimp ,
            TransferManager.TransferReason.CableCar ,
            TransferManager.TransferReason.Ferry ,
            TransferManager.TransferReason.MetroTrain ,
            TransferManager.TransferReason.Monorail ,
            TransferManager.TransferReason.PassengerTrain ,
            TransferManager.TransferReason.PassengerPlane ,
            TransferManager.TransferReason.PassengerShip ,
            TransferManager.TransferReason.IntercityBus ,
            TransferManager.TransferReason.TouristBus ,
            TransferManager.TransferReason.Tram ,
            TransferManager.TransferReason.Bus
        };


    }
}

