﻿using Klyte.Commons;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSVehicleTextsSingleton : MonoBehaviour
    {
        public WTSVehicleData Data => WTSVehicleData.Instance;

        public SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> CityDescriptors => Data.CityDescriptors;
        public SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> GlobalDescriptors => Data.GlobalDescriptors;
        public SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> AssetsDescriptors => Data.AssetsDescriptors;

        #region Initialize
        public void Awake()
        {
        }

        public void Start() => LoadAllVehiclesConfigurations();


        #endregion


        public void AfterRenderExtraStuff(VehicleAI thiz, ushort vehicleID, ref Vehicle vehicleData, RenderManager.CameraInfo cameraInfo, InstanceID id, Vector3 position, Quaternion rotation, Vector4 tyrePosition, Vector4 lightState, Vector3 scale, Vector3 swayPosition, bool underground, bool overground)
        {


            if (thiz.m_info == null || thiz.m_info.m_vehicleAI == null || thiz.m_info.m_subMeshes == null)
            {
                return;
            }

            GetTargetDescriptor(thiz.m_info.name, out _, out LayoutDescriptorVehicleXml targetDescriptor);

            if (targetDescriptor != null)
            {
                Vehicle.Flags flags = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_flags;
                Matrix4x4 vehicleMatrix = thiz.m_info.m_vehicleAI.CalculateBodyMatrix(flags, ref position, ref rotation, ref scale, ref swayPosition);
                MaterialPropertyBlock materialBlock = VehicleManager.instance.m_materialBlock;
                materialBlock.Clear();

                UpdateSubmeshes(thiz.m_info, targetDescriptor);

                RenderDescriptor(ref vehicleData, cameraInfo, vehicleID, position, vehicleMatrix, ref targetDescriptor);
            }
        }

        internal static void UpdateSubmeshes(VehicleInfo info, LayoutDescriptorVehicleXml targetDescriptor)
        {
            if (!targetDescriptor?.SubmeshesUpdated ?? false)
            {
                if (info?.m_subMeshes != null && targetDescriptor.BlackSubmeshes != null)
                {
                    foreach (int idx in targetDescriptor.BlackSubmeshes)
                    {
                        if (idx < info.m_subMeshes.Length && info.m_subMeshes[idx].m_subInfo?.m_material != null)
                        {
                            info.m_subMeshes[idx].m_subInfo.m_material.mainTexture = Texture2D.blackTexture;
                            var aciReplacement = new Texture2D(info.m_subMeshes[idx].m_subInfo.m_material.mainTexture.width, info.m_subMeshes[idx].m_subInfo.m_material.mainTexture.height, TextureFormat.RGBA32, false);
                            aciReplacement.SetPixels(new Color[info.m_subMeshes[idx].m_subInfo.m_material.mainTexture.width * info.m_subMeshes[idx].m_subInfo.m_material.mainTexture.height].Select(x => Color.blue).ToArray());
                            info.m_subMeshes[idx].m_vehicleFlagsRequired = Vehicle.Flags.Created;
                            info.m_subMeshes[idx].m_vehicleFlagsForbidden = 0;
                            info.m_subMeshes[idx].m_subInfo.m_material.SetTexture("_ACIMap", aciReplacement);
                        }
                    }
                }
                targetDescriptor.SubmeshesUpdated = true;
            }
        }

        internal static void GetTargetDescriptor(string vehicle, out ConfigurationSource source, out LayoutDescriptorVehicleXml target)
        {
            if (vehicle == null)
            {
                source = ConfigurationSource.NONE;
                target = null;
                return;
            }

            if (WTSVehicleData.Instance.CityDescriptors.ContainsKey(vehicle))
            {
                source = ConfigurationSource.CITY;
                target = WTSVehicleData.Instance.CityDescriptors[vehicle];
                return;
            }

            if (WTSVehicleData.Instance.GlobalDescriptors.ContainsKey(vehicle))
            {
                source = ConfigurationSource.GLOBAL;
                target = WTSVehicleData.Instance.GlobalDescriptors[vehicle];
                return;
            }

            if (WTSVehicleData.Instance.AssetsDescriptors.ContainsKey(vehicle))
            {
                source = ConfigurationSource.ASSET;
                target = WTSVehicleData.Instance.AssetsDescriptors[vehicle];
                return;
            }

            source = ConfigurationSource.NONE;
            target = null;

        }



        private void RenderDescriptor(ref Vehicle v, RenderManager.CameraInfo cameraInfo, ushort vehicleId, Vector3 position, Matrix4x4 vehicleMatrix, ref LayoutDescriptorVehicleXml targetDescriptor)
        {
            var instance = VehicleManager.instance;
            for (int j = 0; j < targetDescriptor.TextDescriptors.Length; j++)
            {
                ref BoardTextDescriptorGeneralXml descriptor = ref targetDescriptor.TextDescriptors[j];
                if (cameraInfo.CheckRenderDistance(position, 200 * descriptor.m_textScale * (descriptor.IlluminationConfig?.IlluminationType == FontStashSharp.MaterialType.OPAQUE ? 1 : 2)))
                {
                    MaterialPropertyBlock properties = instance.m_materialBlock;
                    properties.Clear();
                    var flags = v.m_flags;
                    if ((flags & Vehicle.Flags.Inverted) != 0)
                    {
                        flags ^= Vehicle.Flags.Reversed;
                    }
                    ref Vehicle vehicle = ref instance.m_vehicles.m_buffer[vehicleId];
                    WTSDynamicTextRenderingRules.RenderTextMesh(vehicleId, 0, 0, targetDescriptor, vehicleMatrix, null, ref targetDescriptor.TextDescriptors[j], properties, (int)flags, vehicle.Info.m_vehicleAI.GetColor(vehicleId, ref vehicle, InfoManager.InfoMode.None), v.Info, ref VehicleManager.instance.m_drawCallData.m_batchedCalls);
                }
            }

        }

        #region IO 

        private static string DefaultFilename { get; } = $"{WTSController.m_defaultFileNameVehiclesXml}.xml";

        public void LoadAllVehiclesConfigurations()
        {
            LogUtils.DoLog("LOADING VEHICLE CONFIG START -----------------------------");
            var errorList = new List<string>();
            Data.GlobalDescriptors.Clear();
            Data.AssetsDescriptors.Clear();
            FileUtils.ScanPrefabsFolders<VehicleInfo>(DefaultFilename, LoadDescriptorsFromXmlAsset);
            LogUtils.DoLog($"DefaultVehiclesConfigurationFolder = {WTSController.DefaultVehiclesConfigurationFolder}");
            foreach (string filename in Directory.GetFiles(WTSController.DefaultVehiclesConfigurationFolder, "*.xml"))
            {
                try
                {
                    if (CommonProperties.DebugMode)
                    {
                        LogUtils.DoLog($"Trying deserialize {filename}:\n{File.ReadAllText(filename)}");
                    }
                    using (FileStream stream = File.OpenRead(filename))
                    {
                        LoadDescriptorsFromXmlCommon(stream, null);
                    }
                }
                catch (Exception e)
                {
                    LogUtils.DoWarnLog($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}\n{e}");
                    errorList.Add($"Error Loading file \"{filename}\" ({e.GetType()}): {e.Message}");
                }
            }

            if (errorList.Count > 0)
            {
                K45DialogControl.ShowModal(new K45DialogControl.BindProperties
                {
                    title = "WTS - Errors loading vehicle Files",
                    message = string.Join("\r\n", errorList.ToArray()),
                    useFullWindowWidth = true,
                    showButton1 = true,
                    textButton1 = "Okay...",
                    showClose = true

                }, (x) => true);

            }

            Data.CleanCache();

            LogUtils.DoLog("LOADING VEHICLE CONFIG END -----------------------------");
        }

        private void LoadDescriptorsFromXmlCommon(FileStream stream, VehicleInfo info) => LoadDescriptorsFromXml(stream, info, ref Data.GlobalDescriptors);
        private void LoadDescriptorsFromXmlAsset(FileStream stream, VehicleInfo info) => LoadDescriptorsFromXml(stream, info, ref Data.AssetsDescriptors);
        private void LoadDescriptorsFromXml(FileStream stream, VehicleInfo info, ref SimpleXmlDictionary<string, LayoutDescriptorVehicleXml> referenceDic)
        {
            var serializer = new XmlSerializer(typeof(ExportableLayoutDescriptorVehicleXml));

            LogUtils.DoLog($"trying deserialize: {info}");

            if (serializer.Deserialize(stream) is ExportableLayoutDescriptorVehicleXml configs)
            {
                foreach (var config in configs.Descriptors)
                {
                    if (info != null)
                    {
                        string[] propEffName = info.name.Split(".".ToCharArray(), 2);
                        string[] xmlEffName = config.VehicleAssetName.Split(".".ToCharArray(), 2);
                        if (propEffName.Length == 2 && xmlEffName.Length == 2 && xmlEffName[1] == propEffName[1])
                        {
                            config.VehicleAssetName = info.name;
                        }
                    }
                    else if (config.VehicleAssetName == null)
                    {
                        throw new Exception("Vehicle name not set at file!!!!");
                    }
                    referenceDic[config.VehicleAssetName] = config;
                }
            }
            else
            {
                throw new Exception("The file wasn't recognized as a valid descriptor!");
            }
        }
        #endregion


    }
}
