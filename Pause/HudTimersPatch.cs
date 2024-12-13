using HarmonyLib;
using UnityEngine;

namespace Pause
{
    //Uses real time since start, which may be nice for certain UI elements while paused, but is terrible for the Void Jump Stability number.
    [HarmonyPatch("UnityEngine.UIElements.Panel, UnityEngine.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", "DefaultTimeSinceStartupMs")]
    internal class HudTimersPatch
    {
        static bool Prefix(ref long __result)
        {
            __result = (long)(Time.time * 1000);
            return false;
        }
    }
}
