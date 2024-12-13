using HarmonyLib;
using Photon.Pun;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Pause
{
    [HarmonyPatch(typeof(PhotonNetwork), "ServerTimestamp", MethodType.Getter)]
    internal class ServerTimestampPatch
    {
        // stored server time for after unpaused
        internal static int cachedServerTime;

        //You will run out of avalable pause time after 24 days of being paused
        internal static int PauseTotal;

        internal static int GetPauseTotalForClient()
        {
            if (PauseManager.IsPaused)
                return PauseTotal + UnmodifiedServerTimestamp() - cachedServerTime;
            else
                return PauseTotal;
        }


        static FieldInfo StartupStopwatchFI = AccessTools.Field(typeof(PhotonNetwork), "StartupStopwatch");

        public static int UnmodifiedServerTimestamp()
        {
            if (!PhotonNetwork.OfflineMode)
            {
                return PhotonNetwork.NetworkingClient.LoadBalancingPeer.ServerTimeInMilliSeconds;
            }
            if (StartupStopwatchFI.GetValue(null) != null && ((Stopwatch)StartupStopwatchFI.GetValue(null)).IsRunning)
            {
                return (int)((Stopwatch)StartupStopwatchFI.GetValue(null)).ElapsedMilliseconds;
            }
            return Environment.TickCount;
        }

        internal static void UpdateTiming(bool paused)
        {
            if (paused)
                cachedServerTime = UnmodifiedServerTimestamp();
            else
                PauseTotal += UnmodifiedServerTimestamp() - cachedServerTime;
        }

        //ServerTime -= PauseTotal
        static void Postfix(ref int __result)
        {
            if (PauseManager.IsPaused)
                __result = cachedServerTime - PauseTotal;
            else
                __result -= PauseTotal;
        }
    }
}
