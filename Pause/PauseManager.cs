using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using VoidManager.ModMessages;

namespace Pause
{
    internal class PauseManager : ModMessage
    {
        private const int version = 2;

        private static bool _isPaused = false;
        internal static bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (value == _isPaused) return;

                //assign isPaused value and timescale for unity pausing. Fix Photon non communicating during pause.
                _isPaused = value;
                if (value)
                {
                    PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = 0f;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = 1f;
                    PhotonNetwork.MinimalTimeScaleToDispatchInFixedUpdate = -1f;
                }
            }
        }
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
            //If not master, send request to master and stop.
            if (!PhotonNetwork.IsMasterClient)
            {
                BepinPlugin.Log.LogInfo("TryToggle non-host triggered.");
                RequestPause(!IsPaused);
                return;
            }

            IsPaused = !IsPaused;
            BepinPlugin.Log.LogInfo($"Toggled Pause from handler");

            ServerTimestampPatch.UpdateTiming(IsPaused);

            //Actually pause.
            pausePlayer = pauser ?? PhotonNetwork.LocalPlayer;
            SendPause(IsPaused, pausePlayer);
        }

        internal static void Reset()
        {
            IsPaused = false;
            ServerTimestampPatch.PauseTotal = 0;
        }

        public override void Handle(object[] arguments, Player sender)
        {
            if (arguments.Length < 3) return;
            if (((int)arguments[0]) != version) //Message Send Version
            {
                BepinPlugin.Log.LogInfo($"Received version {(int)arguments[0]}, expected {version}");
                return;
            }

            //If message is request pause, is host, and players are allowed to pause, attempt pausing.
            if (PhotonNetwork.IsMasterClient)
            {
                BepinPlugin.Log.LogInfo($"Recieved Pause Request ({(bool)arguments[2]}) message from {sender.NickName}");
                if (((MessageType)arguments[1]) == MessageType.Pause && Configs.playersCanPauseConfig.Value && ((bool)arguments[2]) != IsPaused)
                {
                    pausePlayer = sender;
                    TryTogglePause(pausePlayer);
                }
                return;
            }
            
            //stop early if sender isn't host.
            if (!sender.IsMasterClient) return;

            //execute message type, pause vs allowing pause.
            switch ((MessageType)arguments[1])
            {
                case MessageType.Pause:
                    {
                        BepinPlugin.Log.LogInfo($"Recieved Pause ({(bool)arguments[2]}) message from {sender.NickName}");
                        IsPaused = (bool)arguments[2];
                        if (arguments.Length <= 3)
                            break;
                        pausePlayer = PhotonNetwork.CurrentRoom.GetPlayer((int)arguments[3]);
                        ServerTimestampPatch.PauseTotal = (int)arguments[4];

                        //Update CachedTime for client
                        ServerTimestampPatch.UpdateTiming(true);
                        break;
                    }
                case MessageType.CanPause:
                    {
                        BepinPlugin.Log.LogInfo($"Recieved CanPause message from {sender.NickName}");
                        CanPause = (bool)arguments[2];
                        break;
                    }
            }
        }

        internal static void RequestPause(bool pause)
        {
            if (PhotonNetwork.IsMasterClient) return;

            Send(new object[] { version, MessageType.Pause, pause }, PhotonNetwork.MasterClient);
            BepinPlugin.Log.LogInfo("RequestPause sent.");
        }

        internal static void SendPause(bool pause, Player pauser, params Player[] players)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Send(new object[] { version, MessageType.Pause, pause, pauser.ActorNumber, ServerTimestampPatch.GetPauseTotalForClient() }, players);
            BepinPlugin.Log.LogInfo("SendPause sent.");
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
