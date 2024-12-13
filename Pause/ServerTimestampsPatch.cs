using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Pause
{
    [HarmonyPatch(typeof(PhotonNetwork), "ServerTimestamp", MethodType.Getter)]
    internal class ServerTimestampsPatch
    {
        //Host's time difference, to account for players joining after the host has already paused.
        internal static float hostTimeDifference;

        internal static float GetLocalTimeDif()
        {
            return Time.realtimeSinceStartup - Time.time;
        }

        //ServerTime -= LocalTimeDif + HostTimeDif
        static void Postfix(ref int __result)
        {
            __result -= (int)(GetLocalTimeDif() + hostTimeDifference) * 1000;
        }
    }
}
