extern alias PugOther;
extern alias I2Loc;

using BepInEx;
using BepInEx.Logging;
using DavyKager;
using HarmonyLib;
using BepInEx.Unity.Mono;
using ckAccess.Patches.UI;
using ckAccess.MapReader;
using System.Reflection;
using System;

namespace ckAccess
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
#pragma warning disable BepInEx002 // False positive: Plugin correctly inherits from BaseUnityPlugin
    public class Plugin : BaseUnityPlugin
#pragma warning restore BepInEx002
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }
        private Harmony harmony;

        private void Awake()
        {
            // Plugin startup logic
            Instance = this;
            Log = Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo("Harmony automatic patches applied.");

            // Log all patches that were applied
            var patchedMethods = harmony.GetPatchedMethods();
            int count = 0;
            foreach (var method in patchedMethods)
            {
                count++;
            }
            Logger.LogInfo($"Total patched methods: {count}");

            Tolk.Load();
            Logger.LogInfo($"Tolk loaded. Detected screen reader: {Tolk.DetectScreenReader()}");
            Tolk.Output("C K Access loaded");

            Logger.LogInfo("SimpleWorldReader accessibility system initialized.");

        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            Tolk.Unload();
        }
    }
}
