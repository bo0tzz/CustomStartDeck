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
        internal static MethodInfo GetRelicEffect;

        private void LoadSoftDependency()
        {
            hasRelicLib = Chainloader.PluginInfos.TryGetValue("io.github.crazyjackel.RelicLib", out BepInEx.PluginInfo plugin);
            if (!hasRelicLib) return;

            Assembly assembly = plugin.Instance.GetType().Assembly;
            Type[] Types = AccessTools.GetTypesFromAssembly(assembly);
            Type register = Types.FirstOrDefault(x => x.Name == "RelicRegister");
            Debug.Log(register);
            GetRelicEffect = AccessTools.Method(register,"GetCustomRelicEffect");
        }

        private void Awake()
        {
            wantedRelicsCfg = Config.Bind("CustomDeck", "Relics", "", "What relics to start every run with");
            wantedOrbsCfg = Config.Bind("CustomDeck", "Orbs", "StoneOrb-Lvl1, StoneOrb-Lvl1, StoneOrb-Lvl1, Daggorb-Lvl1", "What orbs to start every run with");
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
            if (Plugin.printContent)
            {
                List<RelicSet> pools = new List<RelicSet>() { __instance._commonRelicPool, __instance._rareRelicPool, __instance._bossRelicPool};
                List<string> relicStrings = new List<string>();
                pools.ForEach(pool => pool.relics.ForEach(relic => relicStrings.Add(LocalizationManager.GetTranslation(relic.nameKey) + " - " + relic.effect)));
                relicStrings.Sort();
                Debug.Log("Available relics:");
                relicStrings.ForEach(Debug.Log);
            }

            foreach (string effectName in Plugin.wantedRelicEffects.ToList())
            {
                Debug.Log($"Adding {effectName}");
                try
                {
                    RelicEffect relicEffect = Plugin.hasRelicLib ?
                        (RelicEffect)Plugin.GetRelicEffect.Invoke(null, new object[] { effectName }) :
                        (RelicEffect)Enum.Parse(typeof(RelicEffect), effectName);

                    if (relicEffect == RelicEffect.NONE)
                    {
                        Debug.Log($"Cannot Add {effectName}. Effect Evaluates to None.");
                        return;
                    }
                    Relic relic = __instance.GetRelicForEffect(relicEffect);
                    __instance.AddRelic(relic);
                } catch(ArgumentException ex)
                {
                    Debug.LogError("Relic " + effectName + " does not exist!");
                }
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
