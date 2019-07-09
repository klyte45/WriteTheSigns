using Klyte.Commons.Extensors;
using Klyte.Commons.Utils;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace Klyte.DynamicTextBoards.Overrides
{
    public class NetManagerOverrides : MonoBehaviour, IRedirectable
    {
        public Redirector RedirectorInstance { get; set; }


        #region Events
        public static event Action<ushort> eventNodeChanged;
        public static event Action<ushort> eventSegmentChanged;
        public static event Action<ushort> eventSegmentReleased;
        public static event Action<ushort> eventSegmentNameChanged;

#pragma warning disable IDE0051 // Remover membros privados não utilizados
        private static void OnNodeChanged(ref ushort node)
        {
            var node_ = node;
            new AsyncAction(() =>
            {
                eventNodeChanged?.Invoke(node_);
            }).Execute();
        }
        private static void OnSegmentCreated(ref ushort segment, ref ushort startNode, ref ushort endNode)
        {
            var startNode_ = startNode;
            var segment_ = segment;
            var endNode_ = endNode;

            new AsyncAction(() =>
            {
                eventNodeChanged?.Invoke(startNode_);
                eventNodeChanged?.Invoke(endNode_);
                eventSegmentChanged?.Invoke(segment_);
            }).Execute();
        }
        private static void OnSegmentReleased(ref ushort segment)
        {
            var segment_ = segment;
            new AsyncAction(() =>
            {
                eventNodeChanged?.Invoke(NetManager.instance.m_segments.m_buffer[segment_].m_startNode);
                eventNodeChanged?.Invoke(NetManager.instance.m_segments.m_buffer[segment_].m_endNode);
                eventSegmentChanged?.Invoke(segment_);
                eventSegmentReleased?.Invoke(segment_);
            }).Execute();
        }
        private static void OnSegmentNameChanged(ref ushort segmentID)
        {
            var segment_ = segmentID;
            new AsyncAction(() =>
            {
                eventSegmentNameChanged?.Invoke(segment_);
            }).Execute();
        }
        #endregion
#pragma warning restore IDE0051 // Remover membros privados não utilizados

        #region Hooking

        public void Awake()
        {
            LogUtils.DoLog("Loading Net Manager Overrides");
            RedirectorInstance = KlyteMonoUtils.CreateElement<Redirector>(transform);
            #region Net Manager Hooks
            MethodInfo OnNodeChanged = GetType().GetMethod("OnNodeChanged", RedirectorUtils.allFlags);
            MethodInfo OnSegmentCreated = GetType().GetMethod("OnSegmentCreated", RedirectorUtils.allFlags);
            MethodInfo OnSegmentReleased = GetType().GetMethod("OnSegmentReleased", RedirectorUtils.allFlags);
            MethodInfo OnSegmentNameChanged = GetType().GetMethod("OnSegmentNameChanged", RedirectorUtils.allFlags);

            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("CreateNode", RedirectorUtils.allFlags), null, OnNodeChanged);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("ReleaseNode", RedirectorUtils.allFlags), null, OnNodeChanged);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("CreateSegment", RedirectorUtils.allFlags), null, OnSegmentCreated);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("ReleaseSegment", RedirectorUtils.allFlags), OnSegmentReleased);
            RedirectorInstance.AddRedirect(typeof(NetManager).GetMethod("SetSegmentNameImpl", RedirectorUtils.allFlags), null, OnSegmentNameChanged);
            #endregion

        }
        #endregion


    }
}
