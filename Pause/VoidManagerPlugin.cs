using BepInEx;
using CG;
using CG.Game.SpaceObjects.Controllers;
using CG.Game;
using CG.Input;
using Photon.Pun;
using VoidManager.MPModChecks;
using VoidManager.Utilities;
using VoidManager;

namespace Pause
{
    public class VoidManagerPlugin : VoidManager.VoidPlugin
    {
        public VoidManagerPlugin()
        {
            VoidManager.Events.Instance.PlayerEnteredRoom += (_, playerEventArgs) =>
            {
                PauseManager.SendCanPause(playerEventArgs.player);
                PauseManager.SendPause(PauseManager.IsPaused, PauseManager.pausePlayer ?? PhotonNetwork.LocalPlayer, playerEventArgs.player);
            };

            VoidManager.Events.Instance.MasterClientSwitched += (_, _) =>
            {
                PauseManager.SendCanPause();
                PauseManager.SendPause(PauseManager.IsPaused, PauseManager.pausePlayer ?? PhotonNetwork.LocalPlayer);
            };

            VoidManager.Events.Instance.LeftRoom += (_, _) => PauseManager.Reset();

            VoidManager.Events.Instance.LateUpdate += (_, _) =>
            {
                if (Configs.pauseKeyConfig.Value != UnityEngine.KeyCode.None && UnityInput.Current.GetKeyDown(Configs.pauseKeyConfig.Value) &&
                    (!ServiceBase<InputService>.Instance.CursorVisibilityControl.IsCursorShown || PauseManager.IsPaused))
                {
                    if (!PhotonNetwork.IsMasterClient && !PauseManager.CanPause)
                    {
                        Messaging.Notification("Not permitted by host", 8000);
                        return;
                    }
                    VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
                    VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;
                    if (voidJumpState == null || (voidJumpState is not VoidJumpTravellingStable && voidJumpState is not VoidJumpTravellingUnstable))
                    {
                        Messaging.Notification("Not in void jump", 8000);
                        return;
                    }
                    PauseManager.TryTogglePause();
                }
            };
        }

        public override MultiplayerType MPType => MultiplayerType.All;

        public override string Author => MyPluginInfo.PLUGIN_AUTHORS;

        public override string Description => MyPluginInfo.PLUGIN_DESCRIPTION;

        public override string ThunderstoreID => MyPluginInfo.PLUGIN_THUNDERSTORE_ID;
    }
}
