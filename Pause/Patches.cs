using CG.GameLoopStateMachine.GameStates;
using HarmonyLib;

namespace Pause
{
    [HarmonyPatch(typeof(GSTallyScreen))]
    internal class GSTallyScreenPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEnter")]
        static void OnEnter()
        {
            PauseManager.Reset();
        }
    }

    [HarmonyPatch(typeof(VoidJumpTravellingStable))]
    internal class VoidJumpTravellingStablePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnExit")]
        static void OnExit()
        {
            PauseManager.Reset();
        }
    }
}
