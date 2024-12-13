using CG.Input;
using Photon.Pun;
using UnityEngine;
using static UnityEngine.GUILayout;

namespace Pause
{
    internal class PausedGUI : MonoBehaviour, IShowCursorSource, IInputActionMapRequest
    {
        private bool guiActive = false;
        private Rect WindowPos;

        private PausedGUI() {}

        private void Update()
        {
            if (PauseManager.IsPaused != guiActive)
            {
                InputActionMapRequests.RemoveRequest(this);
                guiActive = PauseManager.IsPaused;
                if (guiActive)
                {
                    WindowPos = new Rect(Screen.width / 4f, Screen.height / 4f, Screen.width / 2f, Screen.height / 2f);
                }
                CursorUtility.ShowCursor(this, guiActive);
            }
        }

        private void OnGUI()
        {
            if (guiActive)
            {
                InputActionMapRequests.AddOrChangeRequest(this, InputStateRequestType.UI);
                UnityEngine.GUI.Window(618107, WindowPos, WindowFunction, "Game Paused");
            }
        }

        private void WindowFunction(int WindowID)
        {
            FlexibleSpace();
            BeginHorizontal();
            {
                FlexibleSpace();
                if (PauseManager.pausePlayer != null)
                {
                    Label($"Game paused by {PauseManager.pausePlayer.NickName}");
                }
                else
                {
                    Label("Game paused");
                }
                FlexibleSpace();
            }
            EndHorizontal();
            Label("");

            BeginHorizontal();
            {
                FlexibleSpace();
                if (PhotonNetwork.IsMasterClient || PauseManager.CanPause)
                {
                    if (Button("     Resume     "))
                    {
                        PauseManager.TryTogglePause();
                    }
                }
                else
                {
                    // Pause option never visible.
                    Button($"{(PauseManager.IsPaused ? "Resume" : "Pause")} not permitted by host");
                }
                FlexibleSpace();
            }
            EndHorizontal();
            FlexibleSpace();
        }
    }
}
