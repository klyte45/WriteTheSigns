using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheSigns.Xml
{
    internal enum VariableVehicleSubType
    {
        OwnNumber,
        LineIdentifier,
        NextStopLine,
        PrevStopLine,
        LastStopLine,
        LineFullName,
    }

    internal static class VariableVehicleSubTypeExtensions
    {
        public static Dictionary<Enum, CommandLevel> ReadCommandTree()
        {
            Dictionary<Enum, CommandLevel> result = new Dictionary<Enum, CommandLevel>();
            foreach (var value in Enum.GetValues(typeof(VariableVehicleSubType)).Cast<VariableVehicleSubType>())
            {
                if (value == 0)
                {
                    continue;
                }

                result[value] = value.GetCommandLevel();
            }
            return result;
        }
        public static CommandLevel GetCommandLevel(this VariableVehicleSubType var)
        {
            switch (var)
            {
                case VariableVehicleSubType.LastStopLine:
                case VariableVehicleSubType.LineFullName:
                    return CommandLevel.m_stringFormat;
                case VariableVehicleSubType.LineIdentifier:
                case VariableVehicleSubType.OwnNumber:
                case VariableVehicleSubType.NextStopLine:
                case VariableVehicleSubType.PrevStopLine:
                    return CommandLevel.m_appendPrefix;
                default:
                    return null;
            }
        }
        public static bool ReadData(this VariableVehicleSubType var, string[] relativeParams, ref Enum subtype, ref string numberFormat, ref string stringFormat, ref string prefix, ref string suffix)
        {
            var cmdLevel = var.GetCommandLevel();
            if (cmdLevel is null)
            {
                return false;
            }

            cmdLevel.ParseFormatting(relativeParams, ref numberFormat, ref stringFormat, ref prefix, ref suffix);
            subtype = var;
            return true;
        }
        public static string GetFormattedString(this VariableVehicleSubType var, IEnumerable<int> platforms, TextParameterVariableWrapper varWrapper, ushort vehicleId)
        {
            if (vehicleId == 0)
            {
                return null;
            }
            switch (var)
            {
                case VariableVehicleSubType.NextStopLine:
                    ref Vehicle[] buffer7 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    ref Vehicle targetVehicle7 = ref buffer7[buffer7[vehicleId].GetFirstVehicle(vehicleId)];
                    var regLine2 = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(vehicleId);
                    return WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(targetVehicle7.m_targetBuilding, regLine2);
                case VariableVehicleSubType.PrevStopLine:
                    ref Vehicle[] buffer5 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    ref Vehicle targetVehicle5 = ref buffer5[buffer5[vehicleId].GetFirstVehicle(vehicleId)];
                    var regLine3 = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(vehicleId);
                    return WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(TransportLine.GetPrevStop(targetVehicle5.m_targetBuilding), regLine3);
                case VariableVehicleSubType.LastStopLine:
                    ref Vehicle[] buffer2 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    ref Vehicle targetVehicle = ref buffer2[buffer2[vehicleId].GetFirstVehicle(vehicleId)];
                    var regLine4 = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(vehicleId);
                    if (regLine4.ZeroLine)
                    {
                        return varWrapper.TryFormat(targetVehicle.m_targetBuilding == 0 || (targetVehicle.m_flags & Vehicle.Flags.GoingBack) != 0
                            ? WTSCacheSingleton.instance.GetBuilding(targetVehicle.m_sourceBuilding).Name
                            : WTSCacheSingleton.instance.GetBuilding(WTSBuildingDataCaches.GetStopBuilding(targetVehicle.m_targetBuilding, regLine4)).Name);
                    }
                    else
                    {
                        var target = targetVehicle.m_targetBuilding;
                        var lastTarget = TransportLine.GetPrevStop(target);
                        StopInformation stopInfo = WTSStopUtils.GetStopDestinationData(lastTarget);
                        var result =
                              stopInfo.m_destinationString ?? (
                              stopInfo.m_destinationId != 0
                                ? WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(stopInfo.m_destinationId, regLine4)
                                : WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(targetVehicle.m_targetBuilding, regLine4)
                            );
                        return result;
                    }
                case VariableVehicleSubType.OwnNumber:
                    return WTSCacheSingleton.instance.GetVehicle(vehicleId).Identifier;
                case VariableVehicleSubType.LineIdentifier:
                    ref Vehicle[] buffer = ref VehicleManager.instance.m_vehicles.m_buffer;
                    var targetVehicleId = buffer[vehicleId].GetFirstVehicle(vehicleId);
                    var transportLine = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(targetVehicleId);
                    if (!transportLine.ZeroLine)
                    {
                        return WTSCacheSingleton.instance.GetATransportLine(transportLine).Identifier;
                    }
                    else
                    {
                        ref Vehicle vehicle = ref buffer[targetVehicleId];
                        return vehicle.m_targetBuilding == 0 || (vehicle.m_flags & Vehicle.Flags.GoingBack) != 0
                            ? vehicle.m_sourceBuilding.ToString("D5")
                            : $"R{vehicle.m_targetBuilding.ToString("X4")}";
                    }
                case VariableVehicleSubType.LineFullName:
                    var regLine = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(vehicleId);
                    return regLine.regional ? null : varWrapper.TryFormat(WTSCacheSingleton.instance.GetATransportLine(regLine).Name);
                default:
                    return null;
            }
        }

    }
}
