using BepInEx;
using BepInEx.Configuration;
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

        private ConfigEntry<string> wantedRelicsCfg;

        public static List<string> wantedRelicEffects = new List<string>();

        private void Awake()
        {
            wantedRelicsCfg = Config.Bind("Relics", "StartRelics", "", "What relics to start every run with");
            if (!wantedRelicsCfg.Value.IsNullOrWhiteSpace())
            {
                foreach (string wantedRelic in wantedRelicsCfg.Value.Split(','))
                {
                    wantedRelicEffects.Add(wantedRelic.Trim());
                }
            }
            harmony.PatchAll();
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    [HarmonyPatch(typeof(RelicManager), "Reset")]
    public class RelicManagerPatch
    {
        public static void Postfix(RelicManager __instance)
        {
            List<Relic> relics = __instance.FindRelicsByEffects(Plugin.wantedRelicEffects);
            foreach (Relic relic in relics)
            {
                __instance.AddRelic(relic);
            }
        }
    }
}
