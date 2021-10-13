using BepInEx;
using HarmonyLib;
using Relics;
using System.Collections.Generic;

namespace CustomStartRelics
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Peglin.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        private void Awake()
        {
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    [HarmonyPatch(typeof(RelicManager), "Reset")]
    public class RelicManagerPatch
    {
        public static void Postfix(RelicManager __instance)
        {
            UnityEngine.Debug.Log("Available relics:");
            foreach (RelicSet set in new List<RelicSet>() { __instance._commonRelicPool, __instance._rareRelicPool, __instance._bossRelicPool})
            {
                foreach (Relic relic in set.relics)
                {
                    UnityEngine.Debug.Log(relic.englishDisplayName + ": " + relic.effect);
                }
            }
            List<string> wantedRelics = new List<string>() { "BOMB_FORCE_ALWAYS", "UNPOPPABLE_PEGS" };
            List<Relic> relics = __instance.FindRelicsByEffects(wantedRelics);
            foreach (Relic relic in relics)
            {
                __instance.AddRelic(relic);
            }
        }
    }
}
