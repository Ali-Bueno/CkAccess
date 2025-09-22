extern alias PugOther;

using HarmonyLib;
using ckAccess.Localization;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para anunciar apertura y cierre de inventarios
    /// </summary>
    [HarmonyPatch]
    public static class InventoryStatePatch
    {
        private static bool _lastInventoryState = false;
        private static bool _lastCharacterWindowState = false;

        /// <summary>
        /// Verifica cambios en el estado del inventario cada frame
        /// </summary>
        [HarmonyPatch(typeof(PugOther.UIManager), "Update")]
        [HarmonyPostfix]
        public static void UIManager_Update_Postfix(PugOther.UIManager __instance)
        {
            try
            {
                // Verificar estado del inventario
                bool currentInventoryState = __instance.isAnyInventoryShowing;
                if (currentInventoryState != _lastInventoryState)
                {
                    if (currentInventoryState)
                    {
                        UIManager.Speak(LocalizationManager.GetText("inventory_opened"));
                    }
                    else
                    {
                        UIManager.Speak(LocalizationManager.GetText("inventory_closed"));
                    }
                    _lastInventoryState = currentInventoryState;
                }

                // Verificar estado de la ventana de personaje
                bool currentCharacterWindowState = __instance.characterWindow?.isShowing ?? false;
                if (currentCharacterWindowState != _lastCharacterWindowState)
                {
                    if (currentCharacterWindowState)
                    {
                        UIManager.Speak(LocalizationManager.GetText("character_window_opened"));
                    }
                    else
                    {
                        UIManager.Speak(LocalizationManager.GetText("character_window_closed"));
                    }
                    _lastCharacterWindowState = currentCharacterWindowState;
                }
            }
            catch
            {
                // Error silencioso para evitar problemas en Update
            }
        }
    }
}