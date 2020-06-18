using Klyte.Commons.Utils;
using Klyte.WriteTheSigns.Data;
using Klyte.WriteTheSigns.Rendering;
using Klyte.WriteTheSigns.Xml;
using UnityEngine;

namespace Klyte.WriteTheSigns.Singleton
{
    public class WTSVehicleTextsSingleton : MonoBehaviour
    {
        public WTSVehicleData Data => WTSVehicleData.Instance;


        #region Initialize
        public void Awake()
        {

        }

        public void Start()
        {
        }


        #endregion



        public void AfterRenderExtraStuff(VehicleAI thiz, ushort vehicleID, ref Vehicle vehicleData, RenderManager.CameraInfo cameraInfo, InstanceID id, Vector3 position, Quaternion rotation, Vector4 tyrePosition, Vector4 lightState, Vector3 scale, Vector3 swayPosition, bool underground, bool overground)
        {


            if (thiz.m_info == null || thiz.m_info.m_vehicleAI == null || thiz.m_info.m_subMeshes == null)
            {
                return;
            }

            Vehicle.Flags flags = VehicleManager.instance.m_vehicles.m_buffer[vehicleID].m_flags;
            Matrix4x4 vehicleMatrix = thiz.m_info.m_vehicleAI.CalculateBodyMatrix(flags, ref position, ref rotation, ref scale, ref swayPosition);
            MaterialPropertyBlock materialBlock = VehicleManager.instance.m_materialBlock;
            materialBlock.Clear();


            //BasicRenderInformation renderInfo = RenderUtils.GetTextData($"{vehicleID}", "", "", FontServer.instance[Data.DefaultFont ?? WTSController.DEFAULT_FONT_KEY], null);

            //WTSPropRenderingRules.DrawTextBri(vehicleID, 0, 0, vehicleMatrix, basicDescr, materialBlock, renderInfo, KlyteMonoUtils.ContrastColor(thiz.GetColor(vehicleID, ref vehicleData, InfoManager.InfoMode.None)), new Vector3(thiz.m_info.m_mesh.bounds.max.x, 1, 0), new Vector3(0, -90), Vector3.one, false);
            //WTSPropRenderingRules.DrawTextBri(vehicleID, 0, 0, vehicleMatrix, basicDescr, materialBlock, renderInfo, KlyteMonoUtils.ContrastColor(thiz.GetColor(vehicleID, ref vehicleData, InfoManager.InfoMode.None)), new Vector3(thiz.m_info.m_mesh.bounds.min.x, 1, 0), new Vector3(0, 90), Vector3.one, false);

            RenderSign(cameraInfo, vehicleID, position, vehicleMatrix, ref basicDescr);
        }
        private LayoutDescriptorVehicleXml basicDescr = new LayoutDescriptorVehicleXml
        {
            TextDescriptors = new BoardTextDescriptorGeneralXml[]{  new BoardTextDescriptorGeneralXml
                {
                    m_textType = TextType.LinesSymbols,
                    m_textScale = 2.5f,
                    PlacingConfig = new BoardTextDescriptorGeneralXml.PlacingSettings
                    {
                        Position = (Vector3Xml)new Vector3(0,4,0),
                        m_create180degYClone = true
                    },
                    ColoringConfig = new BoardTextDescriptorGeneralXml.ColoringSettings
                    {
                        m_defaultColor =Color.white,
                        MaterialType = FontStashSharp.MaterialType.BRIGHT,
                        m_useContrastColor = false
                    }

                }, new BoardTextDescriptorGeneralXml
                {
                    m_textType = TextType.LastStopLine,
                    m_textScale = 2.5f,
                    PlacingConfig = new BoardTextDescriptorGeneralXml.PlacingSettings
                    {
                        Position = (Vector3Xml)new Vector3(0,6,0),
                        m_create180degYClone = true
                    },
                    ColoringConfig = new BoardTextDescriptorGeneralXml.ColoringSettings
                    {
                        m_defaultColor = new Color32(255,192,0,255),
                        MaterialType = FontStashSharp.MaterialType.BRIGHT,
                        m_useContrastColor = false
                    }

                }
            }
        };


        private void RenderSign(RenderManager.CameraInfo cameraInfo, ushort vehicleId, Vector3 position, Matrix4x4 vehicleMatrix, ref LayoutDescriptorVehicleXml targetDescriptor)
        {
            for (int j = 0; j < targetDescriptor.TextDescriptors.Length; j++)
            {
                if (cameraInfo.CheckRenderDistance(position, 200 * targetDescriptor.TextDescriptors[j].m_textScale * (targetDescriptor.TextDescriptors[j].ColoringConfig.MaterialType == FontStashSharp.MaterialType.OPAQUE ? 1 : 2)))
                {
                    MaterialPropertyBlock properties = VehicleManager.instance.m_materialBlock;
                    properties.Clear();
                    WTSPropRenderingRules.RenderTextMesh(vehicleId, 0, 0, targetDescriptor, vehicleMatrix, null, ref targetDescriptor.TextDescriptors[j], properties);
                }
            }

        }




    }
}
