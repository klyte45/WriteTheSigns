using ColossalFramework;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Singleton;
using Klyte.WriteTheSigns.Utils;
using Klyte.WriteTheSigns.Xml;
using SpriteFontPlus;
using SpriteFontPlus.Utility;
using System.Collections.Generic;
using System.Linq;

namespace Klyte.WriteTheSigns.Rendering
{

    public static class WTSTextMeshProcess
    {
        internal static BasicRenderInformation GetTextMesh(BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, BoardInstanceXml instance, BoardDescriptorGeneralXml propLayout, out IEnumerable<BasicRenderInformation> multipleOutput, PrefabInfo refPrefab)
        {
            multipleOutput = null;
            DynamicSpriteFont baseFont = FontServer.instance[WTSEtcData.Instance.FontSettings.GetTargetFont(textDescriptor.m_fontClass)] ?? FontServer.instance[propLayout?.FontName];

            if (instance is BoardPreviewInstanceXml preview)
            {
                return GetTextForPreview(textDescriptor, propLayout, ref multipleOutput, ref baseFont, preview, refPrefab);
            }
            else if (instance is LayoutDescriptorVehicleXml vehicleDescriptor)
            {
                return GetTextForVehicle(textDescriptor, refID, ref baseFont, vehicleDescriptor);
            }
            else if (instance is BoardInstanceBuildingXml buildingDescritpor)
            {
                return GetTextForBuilding(textDescriptor, refID, boardIdx, ref multipleOutput, ref baseFont, buildingDescritpor);
            }
            else if (instance is BoardInstanceRoadNodeXml)
            {
                return GetTextForRoadNode(textDescriptor, refID, boardIdx, secIdx, ref baseFont);
            }
            else if (instance is OnNetInstanceCacheContainerXml onNet)
            {
                return GetTextForOnNet(textDescriptor, refID, boardIdx, secIdx, onNet, ref baseFont, out multipleOutput);
            }
            return WTSCacheSingleton.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, null, textDescriptor.m_overrideFont);
        }

        private static BasicRenderInformation GetTextForRoadNode(BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, int secIdx, ref DynamicSpriteFont baseFont)
        {
            CacheRoadNodeItem data = WTSRoadNodesData.Instance.BoardsContainers[refID, boardIdx, secIdx];
            if (data == null)
            {
                return null;
            }

            if (baseFont == null)
            {
                baseFont = FontServer.instance[WTSRoadNodesData.Instance.DefaultFont];
            }

            TextType targetType = textDescriptor.m_textType;
            switch (targetType)
            {
                case TextType.ParkOrDistrict: targetType = data.m_districtParkId > 0 ? TextType.Park : TextType.District; break;
                case TextType.DistrictOrPark: targetType = data.m_districtId == 0 && data.m_districtParkId > 0 ? TextType.Park : TextType.District; break;
                case TextType.Park:
                    if (data.m_districtParkId == 0)
                    {
                        return null;
                    }
                    break;
            }
            switch (targetType)
            {
                case TextType.GameSprite: return GetSpriteFromParameter(data.m_currentDescriptor.Descriptor.CachedProp, textDescriptor.m_spriteParam);
                case TextType.DistanceFromReference: return WTSCacheSingleton.GetTextData($"{data.m_distanceRefKm}", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.Fixed: return WTSCacheSingleton.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.StreetSuffix: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(data.m_segmentId).StreetName, baseFont);
                case TextType.StreetNameComplete: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(data.m_segmentId).FullStreetName, baseFont);
                case TextType.StreetPrefix: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(data.m_segmentId).StreetQualifier, baseFont);
                case TextType.District: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetDistrict(data.m_districtId).Name, baseFont);
                case TextType.Park: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetPark(data.m_districtParkId).Name, baseFont);
                case TextType.PostalCode: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(data.m_segmentId).PostalCode, baseFont);
                case TextType.CityName: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetDistrict(0).Name, baseFont);
                default: return null;
            };
        }

        private static BasicRenderInformation GetTextForBuilding(BoardTextDescriptorGeneralXml textDescriptor, ushort refID, int boardIdx, ref IEnumerable<BasicRenderInformation> multipleOutput, ref DynamicSpriteFont baseFontRef, BoardInstanceBuildingXml buildingDescritpor)
        {
            ref BoardBunchContainerBuilding data = ref WTSBuildingsData.Instance.BoardsContainers[refID, 0, 0][boardIdx];
            if (data == null)
            {
                return null;
            }
            var baseFont = baseFontRef ?? FontServer.instance[WTSBuildingsData.Instance.DefaultFont];
            TextType targetType = textDescriptor.m_textType;
            switch (targetType)
            {
                case TextType.GameSprite: return GetSpriteFromParameter(buildingDescritpor.Descriptor.CachedProp, textDescriptor.m_spriteParam);
                case TextType.Fixed: return WTSCacheSingleton.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.OwnName: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(refID).Name, baseFont);
                case TextType.NextStopLine: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(WTSStopUtils.GetTargetStopInfo(buildingDescritpor, refID).FirstOrDefault().NextStopBuildingId).Name, baseFont);
                case TextType.PrevStopLine: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(WTSStopUtils.GetTargetStopInfo(buildingDescritpor, refID).FirstOrDefault().PrevStopBuildingId).Name, baseFont);
                case TextType.LastStopLine: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(WTSStopUtils.GetTargetStopInfo(buildingDescritpor, refID).FirstOrDefault().DestinationBuildingId).Name, baseFont);
                case TextType.StreetPrefix: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(WTSCacheSingleton.instance.GetBuilding(refID).SegmentId).StreetQualifier, baseFont);
                case TextType.StreetSuffix: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(WTSCacheSingleton.instance.GetBuilding(refID).SegmentId).StreetName, baseFont);
                case TextType.StreetNameComplete: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(WTSCacheSingleton.instance.GetBuilding(refID).SegmentId).FullStreetName, baseFont);
                case TextType.PlatformNumber: return WTSCacheSingleton.GetTextData((buildingDescritpor.m_platforms.FirstOrDefault() + 1).ToString(), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.TimeTemperature: return GetTimeTemperatureText(textDescriptor, ref baseFont, refID, boardIdx, 0);
                case TextType.CityName: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetDistrict(0).Name, baseFont);
                case TextType.LinesSymbols:
                    multipleOutput = WriteTheSignsMod.Controller.AtlasesLibrary.DrawLineFormats(WTSStopUtils.GetAllTargetStopInfo(buildingDescritpor, refID).GroupBy(x => x.m_lineId).Select(x => x.First()).Select(x => new WTSLine(x.m_lineId, x.m_regionalLine)));
                    return null;
                case TextType.LineFullName:
                    multipleOutput = WTSStopUtils.GetAllTargetStopInfo(buildingDescritpor, refID).GroupBy(x => x.m_lineId).Select(x => x.First()).Select(x => GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetCityTransportLine(x.m_lineId).Name, baseFont));
                    return null;
                case TextType.ParameterizedText:
                    var param = buildingDescritpor.GetTextParameter(textDescriptor.m_parameterIdx) ?? textDescriptor.DefaultParameterValue;
                    string text = param is null
                        ? $"<PARAM#{textDescriptor.m_parameterIdx} NOT SET>"
                        : param.IsEmpty
                            ? ""
                            : param.GetTargetTextForBuilding(buildingDescritpor, refID, textDescriptor);
                    return WTSCacheSingleton.GetTextData(text, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.ParameterizedGameSprite: return GetSpriteFromCycle(textDescriptor, buildingDescritpor, refID, boardIdx, textDescriptor.m_parameterIdx);
                case TextType.ParameterizedGameSpriteIndexed: return GetSpriteFromParameter(textDescriptor, buildingDescritpor, textDescriptor.m_parameterIdx);
                default:
                    return null;
            }
        }

        private static BasicRenderInformation GetTextForVehicle(BoardTextDescriptorGeneralXml textDescriptor, ushort refID, ref DynamicSpriteFont baseFont, LayoutDescriptorVehicleXml vehicleDescriptor)
        {
            if (baseFont is null)
            {
                baseFont = FontServer.instance[vehicleDescriptor.FontName] ?? FontServer.instance[WTSVehicleData.Instance.DefaultFont];
            }

            TextType targetType = textDescriptor.m_textType;
            switch (targetType)
            {
                case TextType.CityName: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetDistrict(0).Name, baseFont);
                case TextType.GameSprite: return GetSpriteFromParameter(vehicleDescriptor.CachedInfo, textDescriptor.m_spriteParam);
                case TextType.Fixed: return WTSCacheSingleton.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.OwnName: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetVehicle(refID).Identifier, baseFont);
                case TextType.LineIdentifier:
                    ref Vehicle[] buffer = ref VehicleManager.instance.m_vehicles.m_buffer;
                    var targetVehicleId = buffer[refID].GetFirstVehicle(refID);
                    var transportLine = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(targetVehicleId);
                    if (!transportLine.ZeroLine)
                    {
                        return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetATransportLine(transportLine).Identifier, baseFont);
                    }
                    else
                    {
                        ref Vehicle vehicle = ref buffer[targetVehicleId];
                        return vehicle.m_targetBuilding == 0 || (vehicle.m_flags & Vehicle.Flags.GoingBack) != 0
                            ? WTSCacheSingleton.GetTextData(vehicle.m_sourceBuilding.ToString("D5"), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont)
                            : WTSCacheSingleton.GetTextData($"R{vehicle.m_targetBuilding.ToString("X4")}", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                    }
                case TextType.LinesSymbols:
                    ref Vehicle[] buffer1 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    return WriteTheSignsMod.Controller.AtlasesLibrary.DrawLineFormats(new WTSLine[] { WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(refID) }).FirstOrDefault();
                case TextType.LineFullName:
                    var regLine = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(refID);
                    return regLine.regional ? null : GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetATransportLine(regLine).Name, baseFont);
                case TextType.NextStopLine:
                    ref Vehicle[] buffer7 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    ref Vehicle targetVehicle7 = ref buffer7[buffer7[refID].GetFirstVehicle(refID)];
                    var regLine2 = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(refID);
                    return WTSCacheSingleton.GetTextData(WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(targetVehicle7.m_targetBuilding, regLine2), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.PrevStopLine:
                    ref Vehicle[] buffer5 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    ref Vehicle targetVehicle5 = ref buffer5[buffer5[refID].GetFirstVehicle(refID)];
                    var regLine3 = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(refID);
                    return WTSCacheSingleton.GetTextData(WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(TransportLine.GetPrevStop(targetVehicle5.m_targetBuilding), regLine3), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.LastStopLine:
                    ref Vehicle[] buffer2 = ref VehicleManager.instance.m_vehicles.m_buffer;
                    ref Vehicle targetVehicle = ref buffer2[buffer2[refID].GetFirstVehicle(refID)];
                    var regLine4 = WriteTheSignsMod.Controller.ConnectorTLM.GetVehicleLine(refID);
                    if (regLine4.ZeroLine)
                    {
                        return targetVehicle.m_targetBuilding == 0 || (targetVehicle.m_flags & Vehicle.Flags.GoingBack) != 0
                            ? GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(targetVehicle.m_sourceBuilding).Name, baseFont)
                            : GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(WTSBuildingDataCaches.GetStopBuilding(targetVehicle.m_targetBuilding, regLine4)).Name, baseFont);
                    }
                    else
                    {
                        var target = targetVehicle.m_targetBuilding;
                        var lastTarget = TransportLine.GetPrevStop(target);
                        StopInformation stopInfo = WTSStopUtils.GetStopDestinationData(lastTarget);

                        BasicRenderInformation result =
                              stopInfo.m_destinationString != null ? WTSCacheSingleton.GetTextData(stopInfo.m_destinationString, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont)
                            : stopInfo.m_destinationId != 0 ? WTSCacheSingleton.GetTextData(WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(stopInfo.m_destinationId, regLine4), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont)
                            : WTSCacheSingleton.GetTextData(WriteTheSignsMod.Controller.ConnectorTLM.GetStopName(targetVehicle.m_targetBuilding, regLine4), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                        return result;
                    }

                default:
                    return null;
            }
        }

        private static BasicRenderInformation GetTextForPreview(BoardTextDescriptorGeneralXml textDescriptor, BoardDescriptorGeneralXml propLayout, ref IEnumerable<BasicRenderInformation> multipleOutput, ref DynamicSpriteFont baseFont, BoardPreviewInstanceXml preview, PrefabInfo refInfo)
        {
            if (baseFont is null)
            {
                switch (propLayout?.m_allowedRenderClass)
                {
                    case TextRenderingClass.RoadNodes:
                        baseFont = FontServer.instance[WTSRoadNodesData.Instance.DefaultFont];
                        break;
                    case TextRenderingClass.MileageMarker:
                        baseFont = FontServer.instance[WTSRoadNodesData.Instance.DefaultFont];
                        break;
                    case TextRenderingClass.Buildings:
                        baseFont = FontServer.instance[WTSBuildingsData.Instance.DefaultFont];
                        break;
                    case TextRenderingClass.PlaceOnNet:
                        baseFont = FontServer.instance[WTSOnNetData.Instance.DefaultFont];
                        break;
                    case null:
                        baseFont = FontServer.instance[WTSVehicleData.Instance.DefaultFont];
                        break;
                }
            }

            if (!preview.m_overrideText.IsNullOrWhiteSpace() && !textDescriptor.IsSpriteText())
            {
                return WTSCacheSingleton.GetTextData(preview.m_overrideText, "", "", baseFont, textDescriptor.m_overrideFont);
            }

            string otherText = "";
            if (textDescriptor.IsTextRelativeToSegment())
            {
                otherText = $"({textDescriptor.m_destinationRelative}) ";
            }
            switch (textDescriptor.m_textType)
            {
                case TextType.CityName: return WTSCacheSingleton.GetTextData("[City Name]", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.Fixed: return WTSCacheSingleton.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.DistanceFromReference: return WTSCacheSingleton.GetTextData("00", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.PostalCode: return WTSCacheSingleton.GetTextData("00000", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.StreetSuffix: return WTSCacheSingleton.GetTextData($"{otherText}Suffix", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.StreetPrefix: return WTSCacheSingleton.GetTextData($"{otherText}Pre.", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.StreetNameComplete: return WTSCacheSingleton.GetTextData($"{otherText}Full road name", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.District: return WTSCacheSingleton.GetTextData($"{otherText}District", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.DistrictOrPark: return WTSCacheSingleton.GetTextData($"{otherText}District or Area", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.ParkOrDistrict: return WTSCacheSingleton.GetTextData($"{otherText}Area or District", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.Park: return WTSCacheSingleton.GetTextData($"{otherText}Area", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.PlatformNumber: return WTSCacheSingleton.GetTextData("00", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.ParameterizedText: return WTSCacheSingleton.GetTextData(textDescriptor.DefaultParameterValue?.GetTargetTextForNet(null, 0, textDescriptor) ?? $"##PARAM{textDescriptor.m_parameterIdx}##", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.HwShield: return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameBorder");
                case TextType.ParameterizedGameSprite: return textDescriptor.DefaultParameterValue != null ? GetSpriteFromCycle(textDescriptor, propLayout.CachedProp, textDescriptor.DefaultParameterValue, 0, 0, 0) : WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameBorder");
                case TextType.ParameterizedGameSpriteIndexed: return textDescriptor.DefaultParameterValue != null ? GetSpriteFromParameter(propLayout.CachedProp, textDescriptor.DefaultParameterValue) : WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameBorder");
                case TextType.TimeTemperature: return WTSCacheSingleton.GetTextData(WriteTheSignsMod.Clock12hFormat ? "12:60AM" : "24:60", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.LinesSymbols:
                    multipleOutput = WriteTheSignsMod.Controller.AtlasesLibrary.DrawLineFormats(new WTSLine[textDescriptor.MultiItemSettings.SubItemsPerColumn * textDescriptor.MultiItemSettings.SubItemsPerRow].Select((x, y) => new WTSLine((ushort)y, false, true)));
                    return null;
                case TextType.GameSprite:
                    return GetSpriteFromParameter(refInfo, textDescriptor.m_spriteParam) ?? WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsInvalidImage");
                default:
                    string text = $"{textDescriptor.m_textType}: {preview.m_currentText}";
                    if (textDescriptor.m_allCaps)
                    {
                        text = text.ToUpper();
                    }
                    return WTSCacheSingleton.GetTextData(text, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
            };
        }

        private static BasicRenderInformation GetTextForOnNet(BoardTextDescriptorGeneralXml textDescriptor, ushort segmentId, int boardIdx, int secIdx, OnNetInstanceCacheContainerXml propDescriptor, ref DynamicSpriteFont baseFont, out IEnumerable<BasicRenderInformation> multipleOutput)
        {
            multipleOutput = null;
            var data = WTSOnNetData.Instance.m_boardsContainers[segmentId];
            if (data == null)
            {
                return null;
            }
            if (baseFont is null)
            {
                baseFont = FontServer.instance[WTSOnNetData.Instance.DefaultFont];
            }

            TextType targetType = textDescriptor.m_textType;
            ushort targetSegment = 0;
            switch (textDescriptor.m_destinationRelative)
            {
                case DestinationReference.Self: targetSegment = segmentId; break;
                case DestinationReference.Target1: targetSegment = propDescriptor.m_targetSegment1; break;
                case DestinationReference.Target2: targetSegment = propDescriptor.m_targetSegment2; break;
                case DestinationReference.Target3: targetSegment = propDescriptor.m_targetSegment3; break;
                case DestinationReference.Target4: targetSegment = propDescriptor.m_targetSegment4; break;
            };

            if (targetSegment == 0)
            {
                return WTSCacheSingleton.GetTextData($"<TARGET NOT SET! ({textDescriptor.m_destinationRelative})>", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
            }

            switch (targetType)
            {
                case TextType.CityName: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetDistrict(0).Name, baseFont);
                case TextType.GameSprite: return GetSpriteFromParameter(propDescriptor.Descriptor.CachedProp, textDescriptor.m_spriteParam);
                case TextType.Fixed: return WTSCacheSingleton.GetTextData(textDescriptor.m_fixedText ?? "", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.StreetSuffix: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(targetSegment).StreetName, baseFont);
                case TextType.StreetNameComplete: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(targetSegment).FullStreetName, baseFont);
                case TextType.StreetPrefix: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(targetSegment).StreetQualifier, baseFont);
                case TextType.ParkOrDistrict:
                case TextType.DistrictOrPark:
                    var segmentData = WTSCacheSingleton.instance.GetSegment(targetSegment);
                    if (segmentData.DistrictId == 0 && segmentData.ParkId > 0 && targetType == TextType.ParkOrDistrict)
                    {
                        goto case TextType.Park;
                    }
                    else
                    {
                        goto case TextType.District;
                    }
                case TextType.District:
                    var segmentData2 = WTSCacheSingleton.instance.GetSegment(targetSegment);
                    return segmentData2.OutsideConnectionId != 0
                        ? GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetBuilding(segmentData2.OutsideConnectionId).Name, baseFont)
                        : GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetDistrict(segmentData2.DistrictId).Name, baseFont);
                case TextType.Park: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetPark(WTSCacheSingleton.instance.GetSegment(targetSegment).ParkId).Name, baseFont);
                case TextType.PostalCode: return GetFromCacheArray(textDescriptor, WTSCacheSingleton.instance.GetSegment(targetSegment).PostalCode, baseFont);
                case TextType.TimeTemperature: return GetTimeTemperatureText(textDescriptor, ref baseFont, segmentId, boardIdx, secIdx);
                case TextType.ParameterizedText:
                    var param = propDescriptor.GetTextParameter(textDescriptor.m_parameterIdx) ?? textDescriptor.DefaultParameterValue;
                    string text = param is null
                        ? $"<PARAM#{textDescriptor.m_parameterIdx} NOT SET>"
                        : param.IsEmpty
                            ? ""
                            : param.GetTargetTextForNet(propDescriptor, segmentId, textDescriptor);
                    return WTSCacheSingleton.GetTextData(text, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
                case TextType.ParameterizedGameSprite: return GetSpriteFromCycle(textDescriptor, propDescriptor, segmentId, boardIdx, secIdx, textDescriptor.m_parameterIdx);
                case TextType.ParameterizedGameSpriteIndexed: return GetSpriteFromParameter(textDescriptor, propDescriptor, textDescriptor.m_parameterIdx);
                case TextType.HwShield: return WriteTheSignsMod.Controller.HighwayShieldsAtlasLibrary.DrawHwShield(NetManager.instance.m_segments.m_buffer[targetSegment].m_nameSeed);
                default: return null;
            };
        }

        private static BasicRenderInformation GetTimeTemperatureText(BoardTextDescriptorGeneralXml textDescriptor, ref DynamicSpriteFont baseFont, ushort refId, int boardIdx, int secIdx)
        {
            if ((SimulationManager.instance.m_currentFrameIndex + (refId * (1 + boardIdx) + (11345476 * secIdx))) % 760 < 380)
            {
                var time = SimulationManager.instance.m_currentDayTimeHour;
                if (WriteTheSignsMod.Clock12hFormat)
                {
                    time = ((time + 11) % 12) + 1;
                }
                var precision = WriteTheSignsMod.ClockPrecision.value;
                return WTSCacheSingleton.GetTextData($"{((int)time).ToString($"D{(WriteTheSignsMod.ClockShowLeadingZero ? "2" : "1")}")}:{((int)(((int)(time % 1 * 60 / precision)) * precision)).ToString("D2")}", textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
            }
            else
            {
                return WTSCacheSingleton.GetTextData(WTSEtcData.FormatTemp(WeatherManager.instance.m_currentTemperature), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
            }
        }

        private static BasicRenderInformation GetSpriteFromCycle(BoardTextDescriptorGeneralXml textDescriptor, OnNetInstanceCacheContainerXml descriptor, ushort refId, int boardIdx, int secIdx, int parameterIdx)
        {
            var param = descriptor.GetTextParameter(parameterIdx) ?? textDescriptor.DefaultParameterValue;
            return GetSpriteFromCycle(textDescriptor, descriptor.Descriptor.CachedProp, param, refId, boardIdx, secIdx);
        }
        private static BasicRenderInformation GetSpriteFromCycle(BoardTextDescriptorGeneralXml textDescriptor, BoardInstanceBuildingXml descriptor, ushort refId, int secIdx, int parameterIdx)
        {
            var param = descriptor.GetTextParameter(parameterIdx) ?? textDescriptor.DefaultParameterValue;
            return GetSpriteFromCycle(textDescriptor, descriptor.Descriptor.CachedProp, param, refId, secIdx, 0);
        }

        private static BasicRenderInformation GetSpriteFromCycle(BoardTextDescriptorGeneralXml textDescriptor, PrefabInfo cachedPrefab, TextParameterWrapper param, ushort refId, int boardIdx, int secIdx)
        {
            if (param is null)
            {
                return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsNotSet");
            }
            if (param.IsEmpty)
            {
                return null;
            }
            if (param.ParamType != TextParameterWrapper.ParameterType.FOLDER)
            {
                return WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsFolderRequired");
            }
            if (textDescriptor.AnimationSettings.m_itemCycleFramesDuration < 1)
            {
                textDescriptor.AnimationSettings.m_itemCycleFramesDuration = 100;
            }
            return param.GetCurrentSprite(cachedPrefab, (int length) => (int)(((SimulationManager.instance.m_currentFrameIndex + textDescriptor.AnimationSettings.m_extraDelayCycleFrames + (refId * (1 + boardIdx) + (11345476 * secIdx))) % (length * textDescriptor.AnimationSettings.m_itemCycleFramesDuration) / textDescriptor.AnimationSettings.m_itemCycleFramesDuration)));
        }

        private static BasicRenderInformation GetSpriteFromParameter(BoardTextDescriptorGeneralXml textDescriptor, OnNetInstanceCacheContainerXml descriptor, int idx)
            => GetSpriteFromParameter(descriptor.Descriptor.CachedProp, descriptor.GetTextParameter(idx) ?? textDescriptor.DefaultParameterValue);
        private static BasicRenderInformation GetSpriteFromParameter(BoardTextDescriptorGeneralXml textDescriptor, BoardInstanceBuildingXml descriptor, int idx)
            => GetSpriteFromParameter(descriptor.Descriptor.CachedProp, descriptor.GetTextParameter(idx) ?? textDescriptor.DefaultParameterValue);
        private static BasicRenderInformation GetSpriteFromParameter(PrefabInfo prop, TextParameterWrapper param)
            => param is null
                ? WriteTheSignsMod.Controller.AtlasesLibrary.GetFromLocalAtlases(null, "K45_WTS FrameParamsNotSet")
                : param.IsEmpty
                    ? null
                    : param.GetImageBRI(prop);

        public static BasicRenderInformation GetFromCacheArray(BoardTextDescriptorGeneralXml textDescriptor, string text, DynamicSpriteFont baseFont) => text is null ? null : WTSCacheSingleton.GetTextData(text, textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
        public static BasicRenderInformation GetFromCacheArray(BoardTextDescriptorGeneralXml textDescriptor, FormatableString text, DynamicSpriteFont baseFont) => text is null ? null : WTSCacheSingleton.GetTextData(text.Get(textDescriptor.m_allCaps, textDescriptor.m_applyAbbreviations), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
        public static BasicRenderInformation GetFromCacheArray(BoardTextDescriptorGeneralXml textDescriptor, int value, string mask, DynamicSpriteFont baseFont) => WTSCacheSingleton.GetTextData(value.ToString(mask), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
        public static BasicRenderInformation GetFromCacheArray(BoardTextDescriptorGeneralXml textDescriptor, float value, string mask, DynamicSpriteFont baseFont) => WTSCacheSingleton.GetTextData(value.ToString(mask), textDescriptor.m_prefix, textDescriptor.m_suffix, baseFont, textDescriptor.m_overrideFont);
    }


}
