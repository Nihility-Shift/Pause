using CG.Game.SpaceObjects.Controllers;
using CG.Game;
using Photon.Pun;
using VoidManager.ModMessages;
using Photon.Realtime;

namespace Pause
{
    internal class PauseManager : ModMessage
    {
        private const int version = 1;

        internal static bool IsPaused { get; private set; } = false;
        internal static bool CanPause { get; private set; } = false;

        internal static Player pausePlayer;

        private static bool stable = true;

        private enum MessageType
        {
            Pause,
            CanPause
        }

        internal static void TryTogglePause(Player pauser = null)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                RequestPause(!IsPaused);
                return;
            }

            VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
            VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;
            if (voidJumpState == null)
            {
                IsPaused = false;
                return;
            }

            if (voidJumpState is not VoidJumpTravellingStable && voidJumpState is not VoidJumpTravellingUnstable) return;

            pausePlayer = pauser ?? PhotonNetwork.LocalPlayer;

            if (IsPaused)
            {
                IsPaused = false;
                if (!stable)
                {
                    voidJumpSystem.ChangeActiveState<VoidJumpTravellingUnstable>();
                    stable = true;
                }
            }
            else
            {
                IsPaused = true;
                if (voidJumpState is VoidJumpTravellingUnstable)
                {
                    stable = false;
                    voidJumpSystem.ChangeActiveState<VoidJumpTravellingStable>();
                }
            }

            SendPause(IsPaused, pausePlayer);
        }

        internal static void Reset()
        {
            IsPaused = false;
        }

        public override void Handle(object[] arguments, Player sender)
        {
            if (arguments.Length < 3) return;
            if (((int)arguments[0]) != version)
            {
                BepinPlugin.Log.LogInfo($"Received version {(int)arguments[0]}, expected {version}");
                return;
            }

            if (PhotonNetwork.IsMasterClient)
            {
                if (((MessageType)arguments[1]) == MessageType.Pause && Configs.playersCanPauseConfig.Value && ((bool)arguments[2]) != IsPaused)
                {
                    pausePlayer = sender;
                    TryTogglePause(pausePlayer);
                }
                return;
            }

            if (sender != PhotonNetwork.MasterClient) return;

            switch ((MessageType)arguments[1])
            {
                case MessageType.Pause:
                    IsPaused = (bool)arguments[2];
                    if (arguments.Length >= 4)
                        pausePlayer = PhotonNetwork.CurrentRoom.GetPlayer((int)arguments[3]);
                    break;
                case MessageType.CanPause:
                    CanPause = (bool)arguments[2];
                    break;
            }
        }

        internal static void RequestPause(bool pause)
        {
            if (PhotonNetwork.IsMasterClient) return;

            Send(new object[] { version, MessageType.Pause, pause }, PhotonNetwork.MasterClient);
        }

        internal static void SendPause(bool pause, Player pauser, params Player[] players)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Send(new object[] { version, MessageType.Pause, pause, pauser.ActorNumber }, players);
        }

        internal static void SendCanPause(params Player[] players)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Send(new object[] { version, MessageType.CanPause, Configs.playersCanPauseConfig.Value }, players);
        }

        private static void Send(object[] arguments, params Player[] players)
        {
            if (players.Length > 0)
            {
                Send(MyPluginInfo.PLUGIN_GUID, GetIdentifier(typeof(PauseManager)), players, arguments, true);
            }
            else
            {
                Send(MyPluginInfo.PLUGIN_GUID, GetIdentifier(typeof(PauseManager)), ReceiverGroup.Others, arguments, true);
            }
        }
    }
}
