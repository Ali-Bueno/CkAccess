extern alias PugOther;
extern alias Core;

using System;
using System.Collections.Generic;
using DavyKager;
using UnityEngine;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Sistema de buffer específico para mensajes de tutorial.
    /// Se navega con las teclas ' (apóstrofe) y ¡ (exclamación invertida)
    /// </summary>
    public static class TutorialBufferSystem
    {
        // Buffer de tutoriales
        private static readonly List<string> _tutorialBuffer = new List<string>();
        private const int MAX_BUFFER_SIZE = 50; // Máximo 50 tutoriales en buffer

        // Navegación por el buffer
        private static int _currentIndex = -1; // -1 = no navegando

        /// <summary>
        /// Añade un mensaje de tutorial al buffer
        /// </summary>
        public static void AddTutorial(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Añadir al buffer
            _tutorialBuffer.Add(message);

            // Limitar tamaño del buffer
            if (_tutorialBuffer.Count > MAX_BUFFER_SIZE)
            {
                _tutorialBuffer.RemoveAt(0); // Eliminar el más antiguo
            }

            // Anunciar el tutorial automáticamente
            Tolk.Output(message, true);

            UnityEngine.Debug.Log($"[TutorialBuffer] Tutorial añadido ({_tutorialBuffer.Count}/{MAX_BUFFER_SIZE}): {message}");
        }

        /// <summary>
        /// Navega al siguiente tutorial (más reciente)
        /// Tecla: ' (apóstrofe)
        /// </summary>
        public static void NavigateToNext()
        {
            if (_tutorialBuffer.Count == 0)
            {
                Tolk.Output("No hay tutoriales", true);
                return;
            }

            // Si no estamos navegando, empezar desde el último
            if (_currentIndex == -1)
            {
                _currentIndex = _tutorialBuffer.Count - 1;
            }
            else
            {
                // Moverse hacia adelante (más reciente)
                _currentIndex++;
                if (_currentIndex >= _tutorialBuffer.Count)
                {
                    _currentIndex = _tutorialBuffer.Count - 1;
                    Tolk.Output("Fin del buffer de tutoriales", true);
                    return;
                }
            }

            AnnounceCurrent();
        }

        /// <summary>
        /// Navega al tutorial anterior (más antiguo)
        /// Tecla: ¡ (exclamación invertida)
        /// </summary>
        public static void NavigateToPrevious()
        {
            if (_tutorialBuffer.Count == 0)
            {
                Tolk.Output("No hay tutoriales", true);
                return;
            }

            // Si no estamos navegando, empezar desde el último
            if (_currentIndex == -1)
            {
                _currentIndex = _tutorialBuffer.Count - 1;
            }
            else
            {
                // Moverse hacia atrás (más antiguo)
                _currentIndex--;
                if (_currentIndex < 0)
                {
                    _currentIndex = 0;
                    Tolk.Output("Inicio del buffer de tutoriales", true);
                    return;
                }
            }

            AnnounceCurrent();
        }

        /// <summary>
        /// Salta al último tutorial (más reciente)
        /// Tecla: Shift + '
        /// </summary>
        public static void JumpToLatest()
        {
            if (_tutorialBuffer.Count == 0)
            {
                Tolk.Output("No hay tutoriales", true);
                return;
            }

            _currentIndex = _tutorialBuffer.Count - 1;
            Tolk.Output($"Último tutorial. {_currentIndex + 1} de {_tutorialBuffer.Count}", true);
            AnnounceCurrent();
        }

        /// <summary>
        /// Salta al primer tutorial (más antiguo)
        /// Tecla: Shift + ¡
        /// </summary>
        public static void JumpToFirst()
        {
            if (_tutorialBuffer.Count == 0)
            {
                Tolk.Output("No hay tutoriales", true);
                return;
            }

            _currentIndex = 0;
            Tolk.Output($"Primer tutorial. 1 de {_tutorialBuffer.Count}", true);
            AnnounceCurrent();
        }

        /// <summary>
        /// Anuncia el tutorial actual con su posición
        /// </summary>
        private static void AnnounceCurrent()
        {
            if (_currentIndex >= 0 && _currentIndex < _tutorialBuffer.Count)
            {
                string message = $"{_currentIndex + 1} de {_tutorialBuffer.Count}. {_tutorialBuffer[_currentIndex]}";
                Tolk.Output(message, true);
            }
        }

        /// <summary>
        /// Limpia el buffer de tutoriales
        /// </summary>
        public static void Clear()
        {
            _tutorialBuffer.Clear();
            _currentIndex = -1;
            UnityEngine.Debug.Log("[TutorialBuffer] Buffer limpiado");
        }

        /// <summary>
        /// Obtiene el número de tutoriales en el buffer
        /// </summary>
        public static int Count => _tutorialBuffer.Count;
    }
}
