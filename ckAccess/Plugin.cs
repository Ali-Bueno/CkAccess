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
    public class Plugin : BaseUnityPlugin
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

            // Check specifically for PlayerInputMenuNavigationPatch
            try
            {
                var playerInputType = typeof(PugOther.PlayerInput);
                var wasButtonPressedMethod = playerInputType.GetMethod("WasButtonPressedDownThisFrame",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new Type[] { typeof(PugOther.PlayerInput.InputType), typeof(bool) },
                    null);

                if (wasButtonPressedMethod != null)
                {
                    var patches = Harmony.GetPatchInfo(wasButtonPressedMethod);
                    if (patches != null && patches.Postfixes.Count > 0)
                    {
                        Logger.LogInfo($"PlayerInput.WasButtonPressedDownThisFrame has {patches.Postfixes.Count} postfix patches");
                    }
                    else
                    {
                        Logger.LogWarning("PlayerInput.WasButtonPressedDownThisFrame has NO patches!");
                    }
                }
                else
                {
                    Logger.LogWarning("Could not find PlayerInput.WasButtonPressedDownThisFrame method!");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error checking patches: {ex}");
            }

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
