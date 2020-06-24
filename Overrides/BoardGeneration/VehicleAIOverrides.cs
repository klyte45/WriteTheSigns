using Harmony;
using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using Klyte.WriteTheSigns;
using Klyte.WriteTheSigns.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace Klyte.DynamicTextProps.Overrides
{

    public partial class VehicleAIOverrides : Redirector, IRedirectable
    {

        public void Awake()
        {


            #region Hooks
            System.Reflection.MethodInfo postRenderExtraStuff = GetType().GetMethod("AfterRenderExtraStuff", RedirectorUtils.allFlags);
            LogUtils.DoLog($"Patching=> {postRenderExtraStuff}");
            AddRedirect(typeof(VehicleAI).GetMethod("RenderExtraStuff", RedirectorUtils.allFlags), null, postRenderExtraStuff);
            System.Reflection.MethodInfo afterSpawn = GetType().GetMethod("AfterSpawnTranspile", RedirectorUtils.allFlags);
            AddRedirect(typeof(Vehicle).GetMethod("Spawn", RedirectorUtils.allFlags), null, null, afterSpawn);

            #endregion
        }

        public static IEnumerable<CodeInstruction> AfterSpawnTranspile(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var result = new List<CodeInstruction>(instructions);
            var labelsEnd = result[result.Count - 1].labels;
            result[result.Count - 1].labels = new List<Label>();
            var instr1 = new CodeInstruction(OpCodes.Ldarg_1)
            {
                labels = labelsEnd
            };
            result.InsertRange(result.Count - 1, new CodeInstruction[] {
                instr1,
                new CodeInstruction(OpCodes.Call, typeof(RenderUtils).GetMethod("ClearCacheVehicleNumber", RedirectorUtils.allFlags, null, new Type[]{ typeof(ushort)},null))
            });
            LogUtils.PrintMethodIL(result);
            return result;
        }


        //public static void AfterEndOverlayImpl(RenderManager.CameraInfo cameraInfo)
        //{ 
        //    if (Instance.EditorInstance.component.isVisible) 
        //    {
        //        foreach (Tuple<Vector3, float, Color> tuple in Instance.m_onOverlayRenderQueue)
        //        {
        //            Singleton<RenderManager>.instance.OverlayEffect.DrawCircle(cameraInfo,
        //               tuple.Third,
        //               tuple.First,
        //               tuple.Second * 2,
        //               -1, 1280f, false, true);
        //        }
        //        Instance.m_onOverlayRenderQueue.Clear();
        //    }
        //}


        public static void AfterRenderExtraStuff(VehicleAI __instance, ushort vehicleID, ref Vehicle data, RenderManager.CameraInfo cameraInfo, InstanceID id, Vector3 position, Quaternion rotation, Vector4 tyrePosition, Vector4 lightState, Vector3 scale, Vector3 swayPosition, bool underground, bool overground)
                => WriteTheSignsMod.Controller?.VehicleTextsSingleton?.AfterRenderExtraStuff(__instance, vehicleID, ref data, cameraInfo, id, position, rotation, tyrePosition, lightState, scale, swayPosition, underground, overground);
    }
}