using System.Collections.Generic;
using ckAccess.Patches.UI;

namespace ckAccess.Tutorials
{
    /// <summary>
    /// Stores tutorial messages in a navigable buffer.
    /// Similar to NotificationSystem but specifically for tutorial messages.
    /// </summary>
    public static class TutorialBuffer
    {
        private static List<string> tutorialMessages = new List<string>();
        private static int currentIndex = -1;
        private const int MAX_BUFFER_SIZE = 50;

        /// <summary>
        /// Add a tutorial message to the buffer
        /// </summary>
        public static void AddTutorial(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            // Add to buffer
            tutorialMessages.Add(message);

            // Keep buffer size manageable
            if (tutorialMessages.Count > MAX_BUFFER_SIZE)
            {
                tutorialMessages.RemoveAt(0);
            }

            // Update current index to point to the latest message
            currentIndex = tutorialMessages.Count - 1;
        }

        /// <summary>
        /// Navigate to next tutorial (more recent)
        /// </summary>
        public static void NavigateNext()
        {
            if (tutorialMessages.Count == 0)
            {
                UIManager.Speak(Localization.LocalizationManager.GetText("no_tutorials"));
                return;
            }

            if (currentIndex < tutorialMessages.Count - 1)
            {
                currentIndex++;
            }

            AnnounceCurrent();
        }

        /// <summary>
        /// Navigate to previous tutorial (older)
        /// </summary>
        public static void NavigatePrevious()
        {
            if (tutorialMessages.Count == 0)
            {
                UIManager.Speak(Localization.LocalizationManager.GetText("no_tutorials"));
                return;
            }

            if (currentIndex > 0)
            {
                currentIndex--;
            }

            AnnounceCurrent();
        }

        /// <summary>
        /// Jump to the last tutorial
        /// </summary>
        public static void JumpToLast()
        {
            if (tutorialMessages.Count == 0)
            {
                UIManager.Speak(Localization.LocalizationManager.GetText("no_tutorials"));
                return;
            }

            currentIndex = tutorialMessages.Count - 1;
            AnnounceCurrent();
        }

        /// <summary>
        /// Jump to the first tutorial
        /// </summary>
        public static void JumpToFirst()
        {
            if (tutorialMessages.Count == 0)
            {
                UIManager.Speak(Localization.LocalizationManager.GetText("no_tutorials"));
                return;
            }

            currentIndex = 0;
            AnnounceCurrent();
        }

        /// <summary>
        /// Announce the current tutorial message with position
        /// </summary>
        private static void AnnounceCurrent()
        {
            if (currentIndex < 0 || currentIndex >= tutorialMessages.Count)
                return;

            string message = tutorialMessages[currentIndex];
            string position = $"{currentIndex + 1} de {tutorialMessages.Count}";

            UIManager.Speak($"{position}. {message}");
        }

        /// <summary>
        /// Clear all tutorials
        /// </summary>
        public static void Clear()
        {
            tutorialMessages.Clear();
            currentIndex = -1;
        }
    }
}
