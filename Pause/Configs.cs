using BepInEx.Configuration;
using UnityEngine;

namespace Pause
{
    internal class Configs
    {
        internal static ConfigEntry<KeyCode> pauseKeyConfig;
        internal static ConfigEntry<bool> playersCanPauseConfig;

        internal static void Load(BepinPlugin plugin)
        {
            pauseKeyConfig = plugin.Config.Bind("Pause", "PauseKey", KeyCode.Home);
            playersCanPauseConfig = plugin.Config.Bind("Pause", "PlayersCanPause", false);
        }
    }
}
