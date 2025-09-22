extern alias PugOther;
extern alias PugComps;
using HarmonyLib;
using UnityEngine;
using ckAccess.Localization;
using PugComps;

namespace ckAccess.Patches.UI
{
    /// <summary>
    /// Parche para accesibilidad de la mesa de trabajo y otras estaciones de crafting
    /// </summary>
    [HarmonyPatch]
    public static class CraftingAccessibilityPatch
    {
        private static string lastAnnouncedRecipe = "";

        /// <summary>
        /// Parche para anunciar recetas cuando se selecciona un slot de receta
        /// </summary>
        [HarmonyPatch(typeof(PugOther.RecipeSlotUI), "OnSelected")]
        [HarmonyPostfix]
        public static void OnRecipeSelected_Postfix(PugOther.RecipeSlotUI __instance)
        {
            try
            {
                // Solo proceder si estamos en una UI de crafting
                if (!PugOther.Manager.ui.isCraftingUIShowing)
                    return;

                AnnounceRecipeDetails(__instance);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in OnRecipeSelected_Postfix: {ex}");
            }
        }

        /// <summary>
        /// Anuncia los detalles de una receta de crafting
        /// </summary>
        private static void AnnounceRecipeDetails(PugOther.RecipeSlotUI recipeSlot)
        {
            try
            {
                var player = PugOther.Manager.main.player;
                if (player?.activeCraftingHandler == null) return;

                // Obtener información de la receta
                var recipeInfo = player.activeCraftingHandler.GetRecipeInfo(recipeSlot.visibleSlotIndex);
                if (!recipeInfo.isValid || recipeInfo.objectID == 0) return;

                // Obtener información del objeto a craftear
                var objectInfo = PugOther.PugDatabase.GetObjectInfo(recipeInfo.objectID);
                if (objectInfo == null) return;

                string objectName = GetLocalizedObjectName(recipeInfo.objectID);

                // Evitar anuncios duplicados
                string currentRecipe = $"{objectName}_{recipeSlot.visibleSlotIndex}";
                if (currentRecipe == lastAnnouncedRecipe) return;
                lastAnnouncedRecipe = currentRecipe;

                // Construir el anuncio
                string announcement = "";

                // Nombre del objeto y cantidad
                if (recipeInfo.amount > 1)
                {
                    announcement = LocalizationManager.GetText("recipe_with_amount", objectName, recipeInfo.amount.ToString());
                }
                else
                {
                    announcement = LocalizationManager.GetText("recipe", objectName);
                }

                // Verificar si se puede craftear
                var nearbyChests = player.activeCraftingHandler.GetNearbyChests();
                bool canCraft = player.activeCraftingHandler.HasMaterialsInCraftingInventoryToCraftRecipe(
                    recipeSlot.visibleSlotIndex, checkPlayerInventoryToo: true, nearbyChests);

                if (canCraft)
                {
                    announcement += ". " + LocalizationManager.GetText("can_craft");
                }
                else
                {
                    announcement += ". " + LocalizationManager.GetText("cannot_craft");

                    // Obtener materiales necesarios
                    var materialInfos = player.activeCraftingHandler.GetCraftingMaterialInfosForRecipe(
                        recipeSlot.visibleSlotIndex, nearbyChests, isRepairing: false, isReinforcing: false);

                    if (materialInfos != null && materialInfos.Count > 0)
                    {
                        announcement += ". " + LocalizationManager.GetText("required_materials") + ": ";

                        for (int i = 0; i < materialInfos.Count && i < 3; i++) // Limitar a 3 materiales para no saturar
                        {
                            var material = materialInfos[i];
                            string materialName = GetLocalizedObjectName(material.objectID);

                            if (i > 0) announcement += ", ";

                            int amountMissing = material.amountNeeded - material.amountAvailable;
                            if (amountMissing > 0)
                            {
                                announcement += LocalizationManager.GetText("missing_material",
                                    materialName, amountMissing.ToString());
                            }
                            else
                            {
                                announcement += LocalizationManager.GetText("available_material",
                                    materialName, material.amountAvailable.ToString());
                            }
                        }

                        if (materialInfos.Count > 3)
                        {
                            announcement += LocalizationManager.GetText("and_more_materials");
                        }
                    }
                }

                UIManager.Speak(announcement);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in AnnounceRecipeDetails: {ex}");
            }
        }

        /// <summary>
        /// Obtiene el nombre localizado de un objeto
        /// </summary>
        private static string GetLocalizedObjectName(ObjectID objectID)
        {
            try
            {
                // Simplificado: usar solo el nombre del enum
                // El sistema de localización del juego se encargará de traducirlo si es necesario
                string enumName = objectID.ToString();

                // Intentar obtener el nombre localizado usando el patrón del juego
                string localizedKey = "Objects/" + enumName;
                string localizedName = UIManager.GetLocalizedText(localizedKey);

                if (!string.IsNullOrEmpty(localizedName) && localizedName != localizedKey)
                {
                    return localizedName;
                }

                // Fallback: Hacer más legible el nombre del enum
                // Convertir "WoodenSword" a "Wooden Sword"
                return MakeReadable(enumName);
            }
            catch
            {
                return objectID.ToString();
            }
        }

        /// <summary>
        /// Convierte un nombre de enum a texto legible
        /// </summary>
        private static string MakeReadable(string enumName)
        {
            if (string.IsNullOrEmpty(enumName)) return enumName;

            var result = new System.Text.StringBuilder();
            result.Append(enumName[0]);

            for (int i = 1; i < enumName.Length; i++)
            {
                if (char.IsUpper(enumName[i]))
                {
                    result.Append(' ');
                }
                result.Append(enumName[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Extiende HandleItemSelection para incluir soporte de crafting
        /// </summary>
        public static bool HandleCraftingSelection(PugOther.UIManager uiManager)
        {
            try
            {
                if (!uiManager.isCraftingUIShowing) return false;

                var currentElement = uiManager.currentSelectedUIElement;
                if (currentElement == null) return false;

                // Verificar si es un slot de receta
                var recipeSlot = currentElement.GetComponent<PugOther.RecipeSlotUI>();
                if (recipeSlot != null)
                {
                    HandleRecipeCrafting(recipeSlot);
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in HandleCraftingSelection: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Maneja el crafting de una receta cuando se presiona U
        /// </summary>
        private static void HandleRecipeCrafting(PugOther.RecipeSlotUI recipeSlot)
        {
            try
            {
                var player = PugOther.Manager.main.player;
                if (player?.activeCraftingHandler == null) return;

                // Verificar que la receta es válida
                var recipeInfo = player.activeCraftingHandler.GetRecipeInfo(recipeSlot.visibleSlotIndex);
                if (!recipeInfo.isValid || recipeInfo.objectID == 0)
                {
                    UIManager.Speak(LocalizationManager.GetText("invalid_recipe"));
                    return;
                }

                // Verificar si se puede craftear
                var nearbyChests = player.activeCraftingHandler.GetNearbyChests();
                bool canCraft = player.activeCraftingHandler.HasMaterialsInCraftingInventoryToCraftRecipe(
                    recipeSlot.visibleSlotIndex, checkPlayerInventoryToo: true, nearbyChests);

                if (!canCraft)
                {
                    UIManager.Speak(LocalizationManager.GetText("cannot_craft_materials"));
                    return;
                }

                // Verificar si hay espacio básico en el inventario
                // Nota: Esta es una verificación simplificada, el juego hará la verificación completa
                var mouseInventoryHandler = player.mouseInventoryHandler;
                var playerInventoryHandler = player.playerInventoryHandler;

                // Si el cursor del ratón tiene algo, verificar si se puede mover al inventario del jugador
                var mouseObject = mouseInventoryHandler.GetContainedObjectData(0);
                if (mouseObject.objectID != ObjectID.None)
                {
                    // El cursor tiene algo, pero el juego se encargará de manejarlo automáticamente
                    // Solo procedemos con el crafting
                }

                // Craftear el objeto usando la funcionalidad existente del juego
                var uiManager = PugOther.Manager.ui;
                var craftingUI = uiManager.activeCraftingUI;
                if (craftingUI != null)
                {
                    // Usar el método ActivateRecipeSlot del CraftingUIBase
                    // mod1 = false (no craft múltiple), mod2 = false (no modificador adicional)
                    craftingUI.ActivateRecipeSlot(recipeSlot.visibleSlotIndex, false, false);

                    string objectName = GetLocalizedObjectName(recipeInfo.objectID);
                    UIManager.Speak(LocalizationManager.GetText("crafted_item", objectName));
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Exception in HandleRecipeCrafting: {ex}");
                UIManager.Speak(LocalizationManager.GetText("crafting_error"));
            }
        }
    }
}