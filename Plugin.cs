using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Relics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CustomStartDeck
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInIncompatibility("me.bo0tzz.peglin.CustomStartRelics")]
    [BepInProcess("Peglin.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private ConfigEntry<string> wantedRelicsCfg;
        private ConfigEntry<string> wantedOrbsCfg;

        public static List<string> wantedRelicEffects = new List<string>();
        public static List<string> wantedOrbs = new List<string>();

        private void Awake()
        {
            wantedRelicsCfg = Config.Bind("CustomDeck", "Relics", "", "What relics to start every run with");
            wantedOrbsCfg = Config.Bind("CustomDeck", "Orbs", "StoneOrb-Lvl1, StoneOrb-Lvl1, StoneOrb-Lvl1, CritOrb-Lvl1", "What orbs to start every run with");

            if (!wantedRelicsCfg.Value.IsNullOrWhiteSpace())
            {
                foreach (string wantedRelic in wantedRelicsCfg.Value.Split(','))
                {
                    wantedRelicEffects.Add(wantedRelic.Trim());
                }
            }

            if (!wantedOrbsCfg.Value.IsNullOrWhiteSpace())
            {
                foreach (string wantedOrb in wantedOrbsCfg.Value.Split(','))
                {
                    wantedOrbs.Add(wantedOrb.Trim());
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
            List<Relic> relics = __instance.FindRelicsByEffects(Plugin.wantedRelicEffects.ToList());
            foreach (Relic relic in relics)
            {
                __instance.AddRelic(relic);
            }
        }
    }
    
    [HarmonyPatch(typeof(GameInit), "Start")]
    public class GameInitPatch
    {
        public static void Postfix(DeckManager ____deckManager)
        {
            List<GameObject> list = new List<GameObject>();
            foreach (string str in Plugin.wantedOrbs)
            {
                GameObject gameObject = Resources.Load<GameObject>("Prefabs/Orbs/" + str);
                if (gameObject == null)
                {
                    Debug.LogError("Orb " + str + " does not exist!");
                }
                list.Add(gameObject);
            }
            ____deckManager.InstantiateDeck(list);
        }
    }
}
