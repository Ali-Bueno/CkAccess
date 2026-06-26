extern alias PugOther;
using HarmonyLib;
using Unity.Mathematics;
using ckAccess.Helpers;

namespace ckAccess.VirtualCursor
{
    /// <summary>
    /// Closes the "lobbed / summoning ranged weapon + gamepad-connected" gap in auto-aim.
    ///
    /// Straight projectiles travel in clientInput.aimDirection (which our UpdateAim patch already redirects),
    /// but LOBBED projectiles and SUMMONING weapons (many staffs) compute their landing point via
    /// RangeWeaponSlot.CalculateAimMarkerTargetPosition. That method only honors our overridden
    /// mouseOrJoystickWorldPoint when clientInput.prefersKeyboardAndMouse is true; with a controller merely
    /// CONNECTED (stick drift / device-priority can flip the game to "joystick" mode even while the player
    /// uses the keyboard), it instead uses the neutral placement indicator, so shots land near the player
    /// and miss. We override the computed target with the current auto-target enemy so these weapons hit it
    /// in ANY input mode.
    ///
    /// Guarded to ONLY apply when an enemy auto-target is active, so it does not disturb the placement-indicator
    /// visual or minion target selection (which also call this method) outside of combat aiming.
    /// </summary>
    [HarmonyPatch(typeof(PugOther.RangeWeaponSlot))]
    public static class RangeWeaponAimPatch
    {
        [HarmonyPatch("CalculateAimMarkerTargetPosition")]
        [HarmonyPostfix]
        public static void CalculateAimMarkerTargetPosition_Postfix(ref float3 __result)
        {
            try
            {
                if (!GameplayStateHelper.IsInGameplayWithoutInventory())
                    return;

                var autoTargetPos = Patches.Player.AutoTargetingPatch.GetCurrentTargetPosition();
                if (autoTargetPos.HasValue)
                {
                    // Keep the original height; redirect the horizontal landing point onto the locked enemy.
                    __result = new float3(autoTargetPos.Value.x, __result.y, autoTargetPos.Value.z);
                }
            }
            catch
            {
                // Never let aim assist break the game's attack pipeline.
            }
        }
    }
}
