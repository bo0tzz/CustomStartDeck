using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace CustomStartDeck
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInIncompatibility("me.bo0tzz.peglin.CustomStartRelics")]
    [BepInDependency("io.github.crazyjackel.RelicLib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("Peglin.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

        private ConfigEntry<string> wantedRelicsCfg;
        private ConfigEntry<string> wantedOrbsCfg;
        private ConfigEntry<bool> printAvailableContent;

        public static List<string> wantedRelicEffects = new List<string>();
        public static List<string> wantedOrbs = new List<string>();
        public static bool printContent;

        internal static bool hasRelicLib;
        internal static MethodInfo GetCustomRelicEffect;

        private void LoadSoftDependency()
        {
            //Check Chain Loader to Get Plugin
            hasRelicLib =
                Chainloader.PluginInfos.TryGetValue("io.github.crazyjackel.RelicLib", out BepInEx.PluginInfo plugin);
            if (!hasRelicLib) return;

            //Find Method for Getting Custom RelicEffects
            Assembly assembly = plugin.Instance.GetType().Assembly;
            Type[] Types = AccessTools.GetTypesFromAssembly(assembly);
            Type register = Types.FirstOrDefault(x => x.Name == "RelicRegister");
            GetCustomRelicEffect = AccessTools.Method(register, "GetCustomRelicEffect");
        }

        private void Awake()
        {
            wantedRelicsCfg = Config.Bind("CustomDeck", "Relics", "", "What relics to start every run with");
            wantedOrbsCfg = Config.Bind("CustomDeck", "Orbs",
                "StoneOrb-Lvl1, StoneOrb-Lvl1, StoneOrb-Lvl1, Daggorb-Lvl1", "What orbs to start every run with");
            printAvailableContent = Config.Bind("CustomDeck", "Print all available relics and orbs", false);

            LoadSoftDependency();


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

            printContent = printAvailableContent.Value;


            if (printContent)
            {
                GameObject[] orbs = Resources.LoadAll<GameObject>("Prefabs/Orbs/");
                List<string> orbTexts = new List<string>();
                foreach (GameObject orb in orbs)
                {
                    orbTexts.Add(orb.name);
                }

                orbTexts.Sort();
                Debug.Log("Available orbs:");
                orbTexts.ForEach(Debug.Log);
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
            List<Relic> allRelics = __instance._commonRelicPool._relics
                .Union(__instance._rareRelicPool._relics)
                .Union(__instance._rareScenarioRelics._relics)
                .Union(__instance._bossRelicPool._relics)
                .ToList();


            if (Plugin.printContent)
            {
                var strings = allRelics.Select(r => $"{LocalizationManager.GetTranslation(r.nameKey)} - {r.effect}  ").ToList();
                strings.Sort();
                Debug.Log("Available relics:");
                strings.ForEach(Debug.Log);
            }

            foreach (string effectName in Plugin.wantedRelicEffects.ToList())
            {
                //If we have our soft dependency use it to get Relic Effect over Enum Parsing.
                RelicEffect relicEffect = Plugin.hasRelicLib
                    ? (RelicEffect) Plugin.GetCustomRelicEffect.Invoke(null, new object[] {effectName})
                    : (RelicEffect) Enum.Parse(typeof(RelicEffect), effectName);

                Relic relic = allRelics.Find(r => r.effect == relicEffect);
                if (relic == null)
                {
                    Debug.LogError($"Relic {effectName} does not exist!");
                    return;
                }

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
                    continue;
                }

                list.Add(gameObject);
            }

            ____deckManager.InstantiateDeck(list);
        }
    }
}