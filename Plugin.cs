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

        public static List<string> wantedRelicEffects = new List<string>();

        private void Awake()
        {

            GameObject[] allOrbs = Resources.LoadAll<GameObject>("Prefabs/Orbs/");
            Logger.LogInfo("All orbs:");
            foreach (GameObject orb in allOrbs)
            {
                Logger.LogInfo(orb.name);
            }

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
            Debug.Log("Running gameinit postfix");
            List<string> startDeck = new List<string>() { "FireBall-Lv3" };
            List<GameObject> list = new List<GameObject>();
            foreach (string str in startDeck)
            {
                GameObject gameObject = Resources.Load<GameObject>("Prefabs/Orbs/" + str);
                if (gameObject == null)
                {
                    Debug.LogError("Prefab/Orbs" + str + ".prefab not found when loading deck!");
                }
                list.Add(gameObject);
            }
            ____deckManager.InstantiateDeck(list);
        }
    }
}
