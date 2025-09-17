# Core Keeper Accessibility Mod

This is a mod that aims to improve the accessibility of the game Core Keeper for visually impaired players, using screen readers to announce the user interface.

## Building

To build this project, you will need to add the following references from the game and Unity libraries to your `.csproj` file.

### Required References

*   tolk/
*   I2.dll
*   Pug.Base.dll
*   Pug.Other.dll
*   Pug.UnityExtensions.dll
*   PugWorldGen.Authoring.dll
*   PugWorldGen.Components.dll
*   PugWorldGen.Conversion.dll
*   PugWorldGen.CustomScenes.dll
*   PugWorldGen.Dungeons.dll
*   Unity.TextMeshPro.dll
*   UnityEngine.CoreModule.dll
*   UnityEngine.dll
*   UnityEngine.UI.dll
*   UnityEngine.UIModule.dll

Make sure the paths to these libraries are correct in your development environment.

## Development Plan

### Completed
- Refactoring and centralization of all menu patches.
- Accessibility for the main menu, settings menu, world slots, character type selection, and character customization menu.
- Accessibility for the join game menu, including keyboard navigation and reading dropdown options.

### Next Steps
1.  **Make the player inventory accessible:** Analyze the inventory structure and apply patches to read item information (name, quantity, description).
2.  **Improve character customization:** Investigate how to get the names or descriptions of appearance options (e.g., "Long Hair", "Red") instead of just "Style X of Y".
3.  **Make character slots accessible:** Apply the same centralized approach to read the information for each character slot.
4.  **Verify and polish:** Thoroughly test all menus to ensure that the reading is fluid and there are no regressions.
