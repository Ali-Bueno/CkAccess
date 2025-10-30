extern alias PugOther;
extern alias Core;

using System;
using System.Collections.Generic;
using DavyKager;
using UnityEngine;
using ckAccess.Localization;

namespace ckAccess.Notifications
{
    /// <summary>
    /// Sistema centralizado de notificaciones con historial navegable.
    /// Gestiona anuncios automáticos y permite revisar notificaciones anteriores.
    /// </summary>
    public static class NotificationSystem
    {
        // Historial de notificaciones
        private static readonly List<Notification> _history = new List<Notification>();
        private const int MAX_HISTORY_SIZE = 100; // Máximo 100 notificaciones en historial (buffer inteligente)

        // Navegación por el historial
        private static int _currentHistoryIndex = -1; // -1 = no navegando

        // Control de anuncios automáticos
        private static float _lastNotificationTime = 0f;
        private const float MIN_NOTIFICATION_INTERVAL = 0.5f; // Mínimo 500ms entre notificaciones automáticas

        /// <summary>
        /// Estructura de una notificación
        /// </summary>
        public class Notification
        {
            public string Message { get; set; }
            public NotificationType Type { get; set; }
            public float Timestamp { get; set; }

            public Notification(string message, NotificationType type)
            {
                Message = message;
                Type = type;
                Timestamp = Time.time;
            }
        }

        /// <summary>
        /// Tipos de notificaciones
        /// </summary>
        public enum NotificationType
        {
            ItemPickup,     // Item recogido
            LevelUp,        // Subida de nivel
            SkillUp,        // Mejora de skill
            Achievement,    // Logro desbloqueado
            Info,           // Información general
            Warning,        // Advertencia
            Error           // Error
        }

        /// <summary>
        /// Agrega una notificación al historial y la anuncia automáticamente
        /// </summary>
        public static void AddNotification(string message, NotificationType type = NotificationType.Info)
        {
            if (string.IsNullOrEmpty(message))
                return;

            try
            {
                // Crear notificación
                var notification = new Notification(message, type);

                // Agregar al historial
                _history.Add(notification);

                // Limitar tamaño del historial
                if (_history.Count > MAX_HISTORY_SIZE)
                {
                    _history.RemoveAt(0); // Eliminar la más antigua
                }

                // Resetear índice de navegación (estamos en tiempo real)
                _currentHistoryIndex = -1;

                // Anunciar automáticamente si ha pasado suficiente tiempo
                if (Time.time - _lastNotificationTime >= MIN_NOTIFICATION_INTERVAL)
                {
                    AnnounceNotification(notification);
                    _lastNotificationTime = Time.time;
                }

                UnityEngine.Debug.Log($"[Notification] {type}: {message}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NotificationSystem] Error agregando notificación: {ex}");
            }
        }

        /// <summary>
        /// Navega a la notificación anterior (más antigua) - Coma
        /// </summary>
        public static void NavigateToPrevious()
        {
            try
            {
                if (_history.Count == 0)
                {
                    Tolk.Output(LocalizationManager.GetText("no_notifications"), true);
                    return;
                }

                // Si no estamos navegando, empezar desde el último
                if (_currentHistoryIndex == -1)
                {
                    _currentHistoryIndex = _history.Count - 1;
                }
                else if (_currentHistoryIndex > 0)
                {
                    _currentHistoryIndex--;
                }
                else
                {
                    // Ya estamos en el primero
                    Tolk.Output(LocalizationManager.GetText("first_notification"), true);
                    return;
                }

                AnnounceCurrentHistoryItem();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NotificationSystem] Error navegando a anterior: {ex}");
            }
        }

        /// <summary>
        /// Navega a la notificación siguiente (más reciente) - Punto
        /// </summary>
        public static void NavigateToNext()
        {
            try
            {
                if (_history.Count == 0)
                {
                    Tolk.Output(LocalizationManager.GetText("no_notifications"), true);
                    return;
                }

                // Si no estamos navegando, empezar desde el primero
                if (_currentHistoryIndex == -1)
                {
                    _currentHistoryIndex = 0;
                }
                else if (_currentHistoryIndex < _history.Count - 1)
                {
                    _currentHistoryIndex++;
                }
                else
                {
                    // Ya estamos en el último
                    Tolk.Output(LocalizationManager.GetText("last_notification"), true);
                    return;
                }

                AnnounceCurrentHistoryItem();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NotificationSystem] Error navegando a siguiente: {ex}");
            }
        }

        /// <summary>
        /// Salta a la última notificación (más reciente) - Shift + Punto
        /// </summary>
        public static void JumpToLatest()
        {
            try
            {
                if (_history.Count == 0)
                {
                    Tolk.Output(LocalizationManager.GetText("no_notifications"), true);
                    return;
                }

                _currentHistoryIndex = _history.Count - 1;
                AnnounceCurrentHistoryItem();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NotificationSystem] Error saltando a última: {ex}");
            }
        }

        /// <summary>
        /// Salta a la primera notificación (más antigua) - Shift + Coma
        /// </summary>
        public static void JumpToFirst()
        {
            try
            {
                if (_history.Count == 0)
                {
                    Tolk.Output(LocalizationManager.GetText("no_notifications"), true);
                    return;
                }

                _currentHistoryIndex = 0;
                AnnounceCurrentHistoryItem();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[NotificationSystem] Error saltando a primera: {ex}");
            }
        }

        /// <summary>
        /// Anuncia la notificación actual del historial
        /// </summary>
        private static void AnnounceCurrentHistoryItem()
        {
            if (_currentHistoryIndex >= 0 && _currentHistoryIndex < _history.Count)
            {
                var notification = _history[_currentHistoryIndex];

                // Anunciar con contexto de posición
                int position = _currentHistoryIndex + 1;
                string announcement = $"{position} de {_history.Count}. {notification.Message}";

                Tolk.Output(announcement, true);
            }
        }

        /// <summary>
        /// Anuncia una notificación con TTS
        /// </summary>
        private static void AnnounceNotification(Notification notification)
        {
            // Interrupt = false para no interrumpir navegación si el usuario está revisando historial
            bool interrupt = _currentHistoryIndex == -1; // Solo interrumpir si no está navegando
            Tolk.Output(notification.Message, interrupt);
        }

        /// <summary>
        /// Obtiene el número de notificaciones en el historial
        /// </summary>
        public static int GetHistoryCount()
        {
            return _history.Count;
        }

        /// <summary>
        /// Limpia todo el historial
        /// </summary>
        public static void ClearHistory()
        {
            _history.Clear();
            _currentHistoryIndex = -1;
            UnityEngine.Debug.Log("[NotificationSystem] Historial limpiado");
        }

        /// <summary>
        /// Sale del modo de navegación y vuelve a tiempo real
        /// </summary>
        public static void ExitNavigationMode()
        {
            _currentHistoryIndex = -1;
        }

        /// <summary>
        /// Obtiene información de debug del sistema
        /// </summary>
        public static string GetDebugInfo()
        {
            return $"Notifications: Count={_history.Count}, CurrentIndex={_currentHistoryIndex}, " +
                   $"MaxSize={MAX_HISTORY_SIZE}";
        }
    }
}
