using CG.Game.SpaceObjects.Controllers;
using CG.Game;
using VoidManager.CustomGUI;
using Photon.Pun;
using VoidManager.Utilities;
using static UnityEngine.GUILayout;

namespace Pause
{
    internal class GUI : ModSettingsMenu
    {
        public override string Name() => "Pause";

        public override void Draw()
        {
            VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
            VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;

            Label("");
            if (PhotonNetwork.IsMasterClient || PauseManager.CanPause)
            {
                if (voidJumpState is VoidJumpTravellingStable || voidJumpState is VoidJumpTravellingUnstable)
                {
                    if (Button(PauseManager.IsPaused ? "Resume" : "Pause"))
                    {
                        PauseManager.TryTogglePause();
                    }
                }
                else
                {
                    Button("Pause only available in Void Jump");
                }
            }
            else
            {
                //Pause option never seen/used
                Button($"{(PauseManager.IsPaused ? "Resume" : "Pause")} not permitted by host");
            }
            Label("");

            if (GUITools.DrawCheckbox("Allow other players to pause/resume", ref Configs.playersCanPauseConfig))
            {
                PauseManager.SendCanPause();
            }
            GUITools.DrawChangeKeybindButton("Pause keybind", ref Configs.pauseKeyConfig);
        }
    }
}
