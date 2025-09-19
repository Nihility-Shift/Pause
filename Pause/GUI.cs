using Photon.Pun;
using VoidManager.CustomGUI;
using VoidManager.Utilities;
using static UnityEngine.GUILayout;

namespace Pause
{
    internal class GUI : ModSettingsMenu
    {
        public override string Name() => "Pause";

        public override void Draw()
        {
            Label("");
            if (PhotonNetwork.IsMasterClient || PauseManager.CanPause && Button(PauseManager.IsPaused ? "Resume" : "Pause"))
            {
                PauseManager.TryTogglePause();
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
