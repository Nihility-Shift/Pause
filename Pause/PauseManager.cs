using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using CG.Ship.Modules;
using Gameplay.Enhancements;
using Gameplay.Power;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VoidManager.ModMessages;

namespace Pause
{
    internal class PauseManager : ModMessage
    {
        private const int version = 1;

        //private static readonly FieldInfo characterHealthField = AccessTools.Field(typeof(CG.Game.Player.Player), "characterHealth");
        //private static readonly FieldInfo OxygenDepositField = AccessTools.Field(typeof(CG.Game.Player.LocalPlayer), "OxygenDeposit");
        //private static readonly FieldInfo activationEndTimeField = AccessTools.Field(typeof(Enhancement), "_activationEndTime");
        //private static readonly FieldInfo currentTemperatureField = AccessTools.Field(typeof(ProtectedPowerSystem), "currentTemperature");

        //private static CG.Game.Player.LocalPlayer player;
        //private static float playerOxygen;
        //private static bool wasInvulnerable;
        //private static Vector3 position;

        //private static int startTime;
        //private static Dictionary<Enhancement, int> EngineTrims;
        //private static ProtectedPowerSystem breakers;
        //private static float breakerTemperature;

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


            //Check void jump system is in a travelling state. Stop and unpause if not travelling (saves users who got stuck somehow, and blocks pausing outside of void jump travelling)
            VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
            VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;
            if (voidJumpState == null || (voidJumpState is not VoidJumpTravellingStable && voidJumpState is not VoidJumpTravellingUnstable))
            {
                IsPaused = false;
                return;
            }


            //Toggle pause and store or restore prior jump stability.
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
            BepinPlugin.Log.LogInfo($"Toggled Pause, next attempting to send.");

            //Actually pause.
            pausePlayer = pauser ?? PhotonNetwork.LocalPlayer;
            SendPause(IsPaused, pausePlayer, ServerTimestampsPatch.GetLocalTimeDif());
        }

        /*private static void WhilePaused(object o, EventArgs e)
        {
            Opsive.UltimateCharacterController.Traits.Attribute oxygenDeposit = (Opsive.UltimateCharacterController.Traits.Attribute)OxygenDepositField.GetValue(player);
            if (oxygenDeposit.Value < playerOxygen)
            {
                oxygenDeposit.Value = playerOxygen;
            }
            if (player.Locomotion.Abilities.FirstOrDefault(ability => ability is MoveThroughPoints).IsActive)
            {
                position = player.Locomotion.Transform.position;
            }
            if (player.Locomotion.Transform.position != position)
            {
                player.Locomotion.Transform.position = position;
            }
            int time = PhotonNetwork.ServerTimestamp;
            foreach (KeyValuePair<Enhancement, int> pair in EngineTrims)
            {
                Enhancement trim = pair.Key;
                int timeDifference = pair.Value;
                activationEndTimeField.SetValue(trim, time + timeDifference);
            }
            currentTemperatureField.SetValue(breakers, breakerTemperature);
        }*/

        internal static void Reset()
        {
            IsPaused = false;
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
                    BepinPlugin.Log.LogInfo($"Recieved Pause ({(bool)arguments[2]}) message from {sender.NickName}");
                    IsPaused = (bool)arguments[2];
                    if (arguments.Length >= 4)
                        pausePlayer = PhotonNetwork.CurrentRoom.GetPlayer((int)arguments[3]);
                    break;
                case MessageType.CanPause:
                    BepinPlugin.Log.LogInfo($"Recieved CanPause message from {sender.NickName}");
                    CanPause = (bool)arguments[2];
                    break;
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

            Send(new object[] { version, MessageType.Pause, pause, pauser.ActorNumber }, players);
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
