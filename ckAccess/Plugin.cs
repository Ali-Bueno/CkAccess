extern alias PugOther;
extern alias I2Loc;

using BepInEx;
using BepInEx.Logging;
using DavyKager;
using HarmonyLib;
using BepInEx.Unity.Mono;
using ckAccess.Patches.UI;
using System.Reflection;
using System;

namespace ckAccess
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        private Harmony harmony;

        private void Awake()
        {
            // Plugin startup logic
            Instance = this;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            Logger.LogInfo("Harmony automatic patches applied.");

            Tolk.Load();
            Logger.LogInfo($"Tolk loaded. Detected screen reader: {Tolk.DetectScreenReader()}");
            Tolk.Output("C K Access loaded");
        }

        private void OnDestroy()
        {
            harmony.UnpatchSelf();
            Tolk.Unload();
        }
    }
}
