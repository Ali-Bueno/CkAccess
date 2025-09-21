extern alias PugOther;

using System.Collections.Generic;
using UnityEngine;
using PugTilemap;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Información completa REAL de una posición del mundo.
    /// </summary>
    public class RealWorldPositionInfo
    {
        public RealTileInfo TileInfo { get; set; } = new RealTileInfo();
        public RealEntitiesInfo EntitiesInfo { get; set; } = new RealEntitiesInfo();
        public bool IsInitialized { get; set; } = false;
        public bool HasAnyTile { get; set; } = false;
        public bool HasAnyEntity { get; set; } = false;
    }

    /// <summary>
    /// Información REAL de tiles usando el sistema nativo.
    /// </summary>
    public class RealTileInfo
    {
        public TileInfo TopTile { get; set; }
        public bool HasValidTile { get; set; } = false;
        public string TileName { get; set; } = "";
        public string TilesetName { get; set; } = "";
        public string MaterialCategory { get; set; } = "";
        public bool IsBlocking { get; set; } = false;
        public bool IsDamageable { get; set; } = false;
        public bool IsDangerous { get; set; } = false;
        public float Hardness { get; set; } = 0f;
        public string RecommendedTool { get; set; } = "";
    }

    /// <summary>
    /// Información REAL de entidades usando Manager.memory.entityMonoLookUp.
    /// </summary>
    public class RealEntitiesInfo
    {
        public List<RealEntityInfo> Entities { get; set; } = new List<RealEntityInfo>();
        public List<RealEntityInfo> EntitiesAtExactPosition { get; set; } = new List<RealEntityInfo>();
    }

    /// <summary>
    /// Información detallada de una entidad real.
    /// </summary>
    public class RealEntityInfo
    {
        // Información básica
        public PugOther.EntityMonoBehaviour EntityMono { get; set; }
        public Vector3 Position { get; set; }
        public float Distance { get; set; }
        public string EntityName { get; set; } = "";

        // Información del objeto
        public ObjectID ObjectID { get; set; } = ObjectID.None;
        public string ObjectName { get; set; } = "";
        public string Category { get; set; } = "";
        public int Amount { get; set; } = 1;

        // Propiedades del objeto
        public bool IsTool { get; set; } = false;
        public bool IsResource { get; set; } = false;
        public bool IsFood { get; set; } = false;
        public bool IsStorage { get; set; } = false;

        // Tipo de entidad
        public EntityType EntityType { get; set; } = EntityType.Unknown;

        // Propiedades de interacción
        public bool IsInteractable { get; set; } = false;
        public bool IsPickupable { get; set; } = false;
        public bool IsDestructible { get; set; } = false;
    }

    /// <summary>
    /// Tipos de entidades que se pueden encontrar en el mundo.
    /// </summary>
    public enum EntityType
    {
        Unknown,
        Enemy,
        Item,
        Storage,
        CraftingStation,
        Decoration,
        NPC,
        Player,
        Core,       // El núcleo del juego
        Mineral     // Minerales y recursos minables
    }

    /// <summary>
    /// Niveles de detalle para las descripciones.
    /// </summary>
    public enum DetailLevel
    {
        Brief,      // Solo lo esencial
        Standard,   // Información estándar
        Detailed,   // Información detallada
        Complete    // Información completa
    }
}