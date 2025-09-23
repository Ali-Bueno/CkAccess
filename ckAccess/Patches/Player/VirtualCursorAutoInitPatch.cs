extern alias PugOther;
extern alias Core;
using HarmonyLib;
using ckAccess.VirtualCursor;
using UnityEngine;

namespace ckAccess.Patches.Player
{
    /// <summary>
    /// Patch para inicializar automáticamente el cursor virtual cuando el jugador entra al mundo
    /// </summary>
    [HarmonyPatch]
    public static class VirtualCursorAutoInitPatch
    {
        private static bool _hasInitialized = false;
        private static float _initDelay = 0f;
        private const float INIT_DELAY_TIME = 2f; // 2 segundos de delay para asegurar que todo esté cargado

        /// <summary>
        /// Patch en PlayerController.Awake para detectar cuando se crea el jugador
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "Awake")]
        [HarmonyPostfix]
        public static void PlayerController_Awake_Postfix(PugOther.PlayerController __instance)
        {
            // Resetear el flag cuando se crea un nuevo jugador
            _hasInitialized = false;
            _initDelay = 0f;
        }

        /// <summary>
        /// Patch en PlayerController.ManagedUpdate para inicializar el cursor después de un delay
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "ManagedUpdate")]
        [HarmonyPostfix]
        public static void PlayerController_ManagedUpdate_Postfix(PugOther.PlayerController __instance)
        {
            try
            {
                // Solo ejecutar si no se ha inicializado aún
                if (_hasInitialized) return;

                // Verificar que el jugador esté completamente cargado
                if (__instance == null || PugOther.Manager.main?.player != __instance)
                    return;

                // Esperar un poco para asegurar que todo esté listo
                _initDelay += UnityEngine.Time.deltaTime;
                if (_initDelay < INIT_DELAY_TIME)
                    return;

                // Verificar que estamos en el juego y no en menús
                var uiManager = PugOther.Manager.ui;
                if (uiManager == null)
                    return;

                // No inicializar si estamos en un menú principal o similar
                if (UnityEngine.Time.timeScale == 0f) // Juego pausado
                    return;

                // Inicializar el cursor virtual
                VirtualCursor.VirtualCursor.Initialize();
                _hasInitialized = true;

                UnityEngine.Debug.Log("[VirtualCursorAutoInit] Cursor virtual inicializado automáticamente");
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Error inicializando cursor virtual automáticamente: {ex}");
            }
        }

        /// <summary>
        /// Patch cuando el jugador sale del mundo para resetear el estado
        /// </summary>
        [HarmonyPatch(typeof(PugOther.PlayerController), "OnDestroy")]
        [HarmonyPostfix]
        public static void PlayerController_OnDestroy_Postfix()
        {
            _hasInitialized = false;
            _initDelay = 0f;
        }
    }
}