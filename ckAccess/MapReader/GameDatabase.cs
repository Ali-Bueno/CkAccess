extern alias PugOther;

using System.Collections.Generic;

namespace ckAccess.MapReader
{
    /// <summary>
    /// Base de datos completa con todos los objetos, enemigos, tiles y elementos del juego.
    /// </summary>
    public static class GameDatabase
    {
        /// <summary>
        /// Diccionario completo de nombres de ObjectIDs importantes.
        /// </summary>
        public static readonly Dictionary<ObjectID, string> ObjectNames = new Dictionary<ObjectID, string>
        {
            // HERRAMIENTAS DE MINERÍA
            { ObjectID.CopperMiningPick, "Pico de cobre" },
            { ObjectID.WoodMiningPick, "Pico de madera" },
            { ObjectID.TinMiningPick, "Pico de estaño" },
            { ObjectID.IronMiningPick, "Pico de hierro" },
            { ObjectID.ScarletMiningPick, "Pico escarlata" },
            { ObjectID.OctarineMiningPick, "Pico de octarina" },
            { ObjectID.GalaxiteMiningPick, "Pico de galaxita" },
            { ObjectID.SolariteMiningPick, "Pico de solarita" },
            { ObjectID.LegendaryMiningPick, "Pico legendario" },
            { ObjectID.AncientMiningPick, "Pico antiguo" },

            // PALAS
            { ObjectID.CopperShovel, "Pala de cobre" },
            { ObjectID.WoodShovel, "Pala de madera" },
            { ObjectID.TinShovel, "Pala de estaño" },
            { ObjectID.IronShovel, "Pala de hierro" },
            { ObjectID.ScarletShovel, "Pala escarlata" },
            { ObjectID.OctarineShovel, "Pala de octarina" },
            { ObjectID.GalaxiteShovel, "Pala de galaxita" },

            // AZADAS
            { ObjectID.CopperHoe, "Azada de cobre" },
            { ObjectID.WoodHoe, "Azada de madera" },
            { ObjectID.TinHoe, "Azada de estaño" },
            { ObjectID.IronHoe, "Azada de hierro" },
            { ObjectID.ScarletHoe, "Azada escarlata" },

            // HERRAMIENTAS ESPECIALES
            { ObjectID.WaterCan, "Regadera" },
            { ObjectID.LargeWaterCan, "Regadera grande" },
            { ObjectID.BugNet, "Red para insectos" },
            { ObjectID.Bucket, "Cubo" },
            { ObjectID.DrillTool, "Taladro" },
            { ObjectID.LaserDrillTool, "Taladro láser" },
            { ObjectID.DrillToolScarlet, "Taladro escarlata" },
            { ObjectID.RoofingTool, "Herramienta de techado" },
            { ObjectID.Leash, "Correa" },
            { ObjectID.CattleCage, "Jaula para ganado" },

            // EL NÚCLEO Y RELACIONADOS
            { ObjectID.TheCore, "El Núcleo" },
            { ObjectID.BrokenCore, "Núcleo Roto" },
            { ObjectID.OracleCardCore, "Núcleo de Carta del Oráculo" },
            { ObjectID.CoreBoss, "Comandante del Núcleo" },
            { ObjectID.CoreBossBeam, "Rayo del Comandante" },
            { ObjectID.CoreBossOrb, "Orbe del Comandante" },
            { ObjectID.CoreCommanderChest, "Cofre del Comandante del Núcleo" },
            { ObjectID.CoreCommanderTrophy, "Trofeo del Comandante del Núcleo" },
            { ObjectID.CoreBossSummoningItem, "Objeto de invocación del Comandante" },
            { ObjectID.CoreEye, "Ojo del Núcleo" },

            // COFRES
            { ObjectID.InventoryChest, "Cofre" },
            { ObjectID.InventoryLarvaHiveChest, "Cofre de panal de larvas" },
            { ObjectID.InventoryMoldDungeonChest, "Cofre de mazmorra de moho" },
            { ObjectID.InventoryAncientChest, "Cofre antiguo" },
            { ObjectID.InventorySeaBiomeChest, "Cofre del bioma marino" },
            { ObjectID.InventoryDesertBiomeChest, "Cofre del bioma desértico" },
            { ObjectID.InventoryLavaChest, "Cofre de lava" },
            { ObjectID.GingerbreadChest, "Cofre de jengibre" },
            { ObjectID.ValentineChest, "Cofre de San Valentín" },
            { ObjectID.AlienChest, "Cofre alienígena" },
            { ObjectID.BossChest, "Cofre de jefe" },
            { ObjectID.GlurchChest, "Cofre de Glurch" },
            { ObjectID.GhormChest, "Cofre de Ghorm" },
            { ObjectID.HivemotherChest, "Cofre de la Reina Colmena" },
            { ObjectID.IvyChest, "Cofre de Ivy" },
            { ObjectID.EasterChest, "Cofre de Pascua" },
            { ObjectID.MorphaChest, "Cofre de Morpha" },
            { ObjectID.OctopusBossChest, "Cofre de Omoroth" },
            { ObjectID.KingSlimeChest, "Cofre del Rey Slime" },
            { ObjectID.LavaSlimeBossChest, "Cofre del Slime de Lava" },
            { ObjectID.AtlantianWormChest, "Cofre del Gusano Atlante" },
            { ObjectID.HydraBossNatureChest, "Cofre de la Hidra de Naturaleza" },
            { ObjectID.HydraBossSeaChest, "Cofre de la Hidra Marina" },
            { ObjectID.HydraBossDesertChest, "Cofre de la Hidra del Desierto" },
            { ObjectID.WallBossChest, "Cofre del Jefe Muro" },
            { ObjectID.PassageChest, "Cofre del Pasaje" },

            // COFRES CON LLAVE
            { ObjectID.LockedPrinceChest, "Cofre del Príncipe (Cerrado)" },
            { ObjectID.LockedQueenChest, "Cofre de la Reina (Cerrado)" },
            { ObjectID.LockedKingChest, "Cofre del Rey (Cerrado)" },
            { ObjectID.UnlockedPrinceChest, "Cofre del Príncipe" },
            { ObjectID.UnlockedQueenChest, "Cofre de la Reina" },
            { ObjectID.UnlockedKingChest, "Cofre del Rey" },
            { ObjectID.LockedCopperChest, "Cofre de cobre (Cerrado)" },
            { ObjectID.CopperChest, "Cofre de cobre" },
            { ObjectID.LockedIronChest, "Cofre de hierro (Cerrado)" },
            { ObjectID.IronChest, "Cofre de hierro" },
            { ObjectID.LockedScarletChest, "Cofre escarlata (Cerrado)" },
            { ObjectID.ScarletChest, "Cofre escarlata" },
            { ObjectID.LockedOctarineChest, "Cofre de octarina (Cerrado)" },
            { ObjectID.OctarineChest, "Cofre de octarina" },
            { ObjectID.LockedGalaxiteChest, "Cofre de galaxita (Cerrado)" },
            { ObjectID.GalaxiteChest, "Cofre de galaxita" },
            { ObjectID.LockedSolariteChest, "Cofre de solarita (Cerrado)" },
            { ObjectID.SolariteChest, "Cofre de solarita" },

            // MESAS DE TRABAJO Y CRAFTEO
            { ObjectID.WoodenWorkBench, "Mesa de trabajo de madera" },
            { ObjectID.CopperWorkbench, "Mesa de trabajo de cobre" },
            { ObjectID.TinWorkbench, "Mesa de trabajo de estaño" },
            { ObjectID.IronWorkBench, "Mesa de trabajo de hierro" },
            { ObjectID.ScarletWorkBench, "Mesa de trabajo escarlata" },
            { ObjectID.OctarineWorkbench, "Mesa de trabajo de octarina" },
            { ObjectID.GalaxiteWorkbench, "Mesa de trabajo de galaxita" },
            { ObjectID.SolariteWorkbench, "Mesa de trabajo de solarita" },
            { ObjectID.ChristmasWorkbench, "Mesa de trabajo navideña" },
            { ObjectID.ValentineWorkbench, "Mesa de trabajo de San Valentín" },
            { ObjectID.HalloweenWorkbench, "Mesa de trabajo de Halloween" },
            { ObjectID.EasterWorkbench, "Mesa de trabajo de Pascua" },
            { ObjectID.BoatWorkbench, "Mesa de trabajo de botes" },
            { ObjectID.GoKartWorkbench, "Mesa de trabajo de karts" },
            { ObjectID.MusicWorkbench, "Mesa de trabajo musical" },
            { ObjectID.GlassWorkbench, "Mesa de trabajo de vidrio" },
            { ObjectID.CattleWorkbench, "Mesa de trabajo de ganadería" },
            { ObjectID.HydraWorkbench, "Mesa de trabajo de hidra" },
            { ObjectID.PouchWorkbench, "Mesa de trabajo de bolsas" },
            { ObjectID.LaboratoryWorkbench, "Mesa de trabajo de laboratorio" },

            // YUNQUES
            { ObjectID.CopperAnvil, "Yunque de cobre" },
            { ObjectID.TinAnvil, "Yunque de estaño" },
            { ObjectID.IronAnvil, "Yunque de hierro" },
            { ObjectID.ScarletAnvil, "Yunque escarlata" },
            { ObjectID.OctarineAnvil, "Yunque de octarina" },
            { ObjectID.GalaxiteAnvil, "Yunque de galaxita" },
            { ObjectID.SolariteAnvil, "Yunque de solarita" },

            // HORNOS Y PROCESADORES
            { ObjectID.Furnace, "Horno" },
            { ObjectID.SmelterKiln, "Horno de fundición" },
            { ObjectID.GlassSmelter, "Fundidor de vidrio" },
            { ObjectID.CookingPot, "Olla de cocina" },
            { ObjectID.AlchemyTable, "Mesa de alquimia" },
            { ObjectID.DistilleryTable, "Destilería" },
            { ObjectID.FuryForge, "Forja de furia" },
            { ObjectID.UpgradeForge, "Forja de mejoras" },

            // OTRAS ESTACIONES
            { ObjectID.ElectronicsTable, "Mesa de electrónica" },
            { ObjectID.Carpenter, "Carpintero" },
            { ObjectID.TableSaw, "Sierra de mesa" },
            { ObjectID.JewelryWorkBench, "Mesa de joyería" },
            { ObjectID.AdvancedJewelryWorkBench, "Mesa de joyería avanzada" },
            { ObjectID.RailwayForge, "Forja ferroviaria" },
            { ObjectID.PaintersTable, "Mesa de pintor" },
            { ObjectID.AutomationTable, "Mesa de automatización" },
            { ObjectID.CartographyTable, "Mesa de cartografía" },
            { ObjectID.SalvageAndRepairStation, "Estación de reciclaje y reparación" },
            { ObjectID.FishingWorkBench, "Mesa de pesca" },
            { ObjectID.KeyCraftingTable, "Mesa de fabricación de llaves" },
            { ObjectID.LoomSpinningWheel, "Telar y rueca" },
            { ObjectID.GreeneryPod, "Cápsula de vegetación" },

            // ENEMIGOS - SLIMES
            { ObjectID.Slime, "Slime naranja" },
            { ObjectID.SlimeBlob, "Slime naranja" },
            { ObjectID.AggressiveSlimeBlob, "Slime agresivo" },
            { ObjectID.PoisonSlime, "Slime venenoso" },
            { ObjectID.PoisonSlimeBlob, "Slime venenoso" },
            { ObjectID.SlipperySlime, "Slime resbaladizo" },
            { ObjectID.SlipperySlimeBlob, "Slime resbaladizo" },
            { ObjectID.LavaSlime, "Slime de lava" },
            { ObjectID.LavaSlimeBlob, "Slime de lava" },
            { ObjectID.RoyalSlimeBlob, "Slime real" },

            // JEFES - SLIMES
            { ObjectID.SlimeBoss, "Glurch el Slime Abominable" },
            { ObjectID.PoisonSlimeBoss, "Slime Venenoso Gigante" },
            { ObjectID.SlipperySlimeBoss, "Slime Resbaladizo Gigante" },
            { ObjectID.KingSlime, "Rey Slime" },
            { ObjectID.LavaSlimeBoss, "Igneous el Slime Fundido" },

            // ENEMIGOS - LARVAS
            { ObjectID.Larva, "Larva" },
            { ObjectID.BigLarva, "Larva grande" },
            { ObjectID.AcidLarva, "Larva ácida" },
            { ObjectID.BossLarva, "Larva gigante" },

            // JEFE - REINA COLMENA
            { ObjectID.LarvaHiveBoss, "Reina Colmena" },
            { ObjectID.LarvaHiveHalloweenBoss, "Reina Colmena de Halloween" },
            { ObjectID.LarvaHiveEgg, "Huevo de colmena" },
            { ObjectID.Cocoon, "Capullo" },

            // ENEMIGOS - CAVELINGS
            { ObjectID.Caveling, "Caveling" },
            { ObjectID.CavelingShaman, "Chamán Caveling" },
            { ObjectID.CavelingGardener, "Jardinero Caveling" },
            { ObjectID.CavelingHunter, "Cazador Caveling" },
            { ObjectID.InfectedCaveling, "Caveling infectado" },
            { ObjectID.CavelingBrute, "Bruto Caveling" },
            { ObjectID.CavelingScholar, "Erudito Caveling" },
            { ObjectID.CavelingAssassin, "Asesino Caveling" },
            { ObjectID.CavelingMummy, "Momia Caveling" },
            { ObjectID.CavelingSkirmisher, "Escaramuzador Caveling" },
            { ObjectID.CavelingSpearman, "Lancero Caveling" },
            { ObjectID.GhostCaveling, "Fantasma Caveling" },
            { ObjectID.GhostScholar, "Fantasma Erudito" },

            // JEFE - CHAMÁN
            { ObjectID.ShamanBoss, "Ivy el Chamán Cruel" },
            { ObjectID.AncientGolem, "Gólem Antiguo" },

            // JEFE - AZEOS
            { ObjectID.BirdBoss, "Azeos el Pájaro del Cielo" },
            { ObjectID.BirdBossBeam, "Rayo de Azeos" },
            { ObjectID.BirdBossStone, "Piedra de Azeos" },

            // JEFE - OMOROTH
            { ObjectID.OctopusBoss, "Omoroth el Monstruo del Mar" },
            { ObjectID.OctopusTentacle, "Tentáculo de Omoroth" },

            // JEFE - RA-AKAR
            { ObjectID.ScarabBoss, "Ra-Akar el Escarabajo de Arena" },
            { ObjectID.BombScarab, "Escarabajo bomba" },
            { ObjectID.GoldenBombScarab, "Escarabajo bomba dorado" },

            // ENEMIGOS - HONGOS
            { ObjectID.MushroomEnemy, "Hongo enemigo" },
            { ObjectID.MushroomBrute, "Bruto Hongo" },

            // PLANTAS HOSTILES
            { ObjectID.SnarePlant, "Planta trampa" },
            { ObjectID.SmallTentacle, "Tentáculo pequeño" },
            { ObjectID.MoldTentacle, "Tentáculo de moho" },

            // CRIATURAS MARINAS
            { ObjectID.CrabEnemy, "Cangrejo" },
            { ObjectID.JellyFish, "Medusa" },

            // ENEMIGOS DEL DESIERTO
            { ObjectID.LavaButterfly, "Mariposa de lava" },
            { ObjectID.DesertBrute, "Bruto del desierto" },

            // HIDRAS
            { ObjectID.HydraBossNature, "Azeos la Hidra de Naturaleza" },
            { ObjectID.HydraBossSea, "Omoroth la Hidra Marina" },
            { ObjectID.HydraBossDesert, "Ra-Akar la Hidra del Desierto" },
            { ObjectID.SnakeBossSegment, "Segmento de Hidra" },

            // ENEMIGOS CRISTALINOS
            { ObjectID.CrystalBigSnail, "Caracol cristalino gigante" },
            { ObjectID.OrbitalTurret, "Torreta orbital" },
            { ObjectID.Mimite, "Mimita" },

            // GUSANOS
            { ObjectID.WormSegment, "Segmento de gusano" },
            { ObjectID.WormSegmentTail, "Cola de gusano" },
            { ObjectID.ClayWormSegment, "Segmento de gusano de arcilla" },
            { ObjectID.ClayWormSegmentTail, "Cola de gusano de arcilla" },
            { ObjectID.NatureWormSegment, "Segmento de gusano de naturaleza" },
            { ObjectID.NatureWormSegmentTail, "Cola de gusano de naturaleza" },
            { ObjectID.AmoebaWormSegment, "Segmento de gusano ameba" },
            { ObjectID.AmoebaWormSegmentTail, "Cola de gusano ameba" },
            { ObjectID.AmoebaGiantSegment, "Segmento de ameba gigante" },
            { ObjectID.AmoebaGiantSegmentTail, "Cola de ameba gigante" },

            // JEFE - MURO VIVIENTE
            { ObjectID.WallBoss, "Muro Viviente" },
            { ObjectID.WallBossBulb, "Bulbo del Muro" },
            { ObjectID.WallBossHead, "Cabeza del Muro" },

            // CIGARRAS
            { ObjectID.NatureCicadaEnemy, "Cigarra de naturaleza" },
            { ObjectID.DesertCicadaEnemy, "Cigarra del desierto" },
            { ObjectID.GiantCicadaBoss, "Cigarra Gigante" },
            { ObjectID.CicadaNymph, "Ninfa de cigarra" },
            { ObjectID.GiantCicadaBossShield, "Escudo de Cigarra Gigante" },

            // NPCS
            { ObjectID.CavelingMerchant, "Mercader Caveling" },
            { ObjectID.SlimeMerchant, "Mercader Slime" },
            { ObjectID.FishingMerchant, "Mercader Pescador" },
            { ObjectID.SeasonalMerchant, "Mercader Estacional" },
            { ObjectID.CrystalMerchant, "Mercader de Cristales" },
            { ObjectID.AncientHologramPod, "Cápsula holográfica antigua" },

            // MINERALES Y RECURSOS
            { ObjectID.CopperOre, "Mineral de cobre" },
            { ObjectID.TinOre, "Mineral de estaño" },
            { ObjectID.IronOre, "Mineral de hierro" },
            { ObjectID.GoldOre, "Mineral de oro" },
            { ObjectID.ScarletOre, "Mineral escarlata" },
            { ObjectID.OctarineOre, "Mineral de octarina" },
            { ObjectID.GalaxiteOre, "Mineral de galaxita" },
            { ObjectID.SolariteOre, "Mineral de solarita" },
            { ObjectID.PandoriumOre, "Mineral de pandorio" },

            // ROCAS DE MINERAL
            { ObjectID.CopperOreBoulder, "Roca de mineral de cobre" },
            { ObjectID.TinOreBoulder, "Roca de mineral de estaño" },
            { ObjectID.IronOreBoulder, "Roca de mineral de hierro" },
            { ObjectID.GoldOreBoulder, "Roca de mineral de oro" },
            { ObjectID.ScarletOreBoulder, "Roca de mineral escarlata" },
            { ObjectID.OctarineOreBoulder, "Roca de mineral de octarina" },
            { ObjectID.GalaxiteOreBoulder, "Roca de mineral de galaxita" },
            { ObjectID.SolariteOreBoulder, "Roca de mineral de solarita" },
            { ObjectID.PandoriumOreBoulder, "Roca de mineral de pandorio" },

            // LINGOTES
            { ObjectID.CopperBar, "Lingote de cobre" },
            { ObjectID.TinBar, "Lingote de estaño" },
            { ObjectID.IronBar, "Lingote de hierro" },
            { ObjectID.GoldBar, "Lingote de oro" },
            { ObjectID.ScarletBar, "Lingote escarlata" },
            { ObjectID.OctarineBar, "Lingote de octarina" },
            { ObjectID.GalaxiteBar, "Lingote de galaxita" },
            { ObjectID.SolariteBar, "Lingote de solarita" },
            { ObjectID.PandoriumBar, "Lingote de pandorio" },

            // MATERIALES
            { ObjectID.Wood, "Madera" },
            { ObjectID.ThornWood, "Madera espinosa" },
            { ObjectID.CoralWood, "Madera de coral" },
            { ObjectID.GleamWood, "Madera brillante" },
            { ObjectID.Plank, "Tablón" },
            { ObjectID.CoralPlank, "Tablón de coral" },
            { ObjectID.GleamWoodPlank, "Tablón brillante" },
            { ObjectID.Glass, "Vidrio" },
            { ObjectID.Fiber, "Fibra" },
            { ObjectID.Wool, "Lana" },

            // GEMAS
            { ObjectID.AncientGemstone, "Gema antigua" },
            { ObjectID.NatureGemstone, "Gema de naturaleza" },
            { ObjectID.SeaGemstone, "Gema marina" },
            { ObjectID.DesertGemstone, "Gema del desierto" },

            // ILUMINACIÓN
            { ObjectID.Torch, "Antorcha" },
            { ObjectID.Campfire, "Fogata" },
            { ObjectID.DecorativeTorch1, "Antorcha decorativa" },
            { ObjectID.FishoilTorch, "Antorcha de aceite de pescado" },
            { ObjectID.CrystalLamp, "Lámpara de cristal" },
            { ObjectID.Lamp, "Lámpara" },
            { ObjectID.Lantern, "Linterna" },
            { ObjectID.OrbLantern, "Linterna orbe" },
            { ObjectID.SmallLantern, "Linterna pequeña" },
            { ObjectID.PumpkinLantern, "Linterna de calabaza" },
            { ObjectID.PearlLantern, "Linterna de perla" },
            { ObjectID.SoulLantern, "Linterna de alma" },

            // DECORACIÓN
            { ObjectID.DecorativePot, "Maceta decorativa" },
            { ObjectID.PlanterBox, "Jardinera" },
            { ObjectID.Pedestal, "Pedestal" },
            { ObjectID.StonePedestal, "Pedestal de piedra" },
            { ObjectID.WoodStool, "Taburete de madera" },
            { ObjectID.WoodTable, "Mesa de madera" },
            { ObjectID.Painting, "Pintura" },
            { ObjectID.WoodPillar, "Pilar de madera" },
            { ObjectID.Bed, "Cama" },
            { ObjectID.WoodDoor, "Puerta de madera" },
            { ObjectID.StoneDoor, "Puerta de piedra" },

            // AUTOMATIZACIÓN
            { ObjectID.Drill, "Perforadora" },
            { ObjectID.CrudeDrill, "Perforadora rudimentaria" },
            { ObjectID.ConveyorBelt, "Cinta transportadora" },
            { ObjectID.RobotArm, "Brazo robótico" },
            { ObjectID.Sprinkler, "Aspersor" },

            // TRAMPAS
            { ObjectID.SpikeTrap, "Trampa de pinchos" },
            { ObjectID.WoodSpikeTrap, "Trampa de pinchos de madera" },
            { ObjectID.HiveSpikeTrap, "Trampa de pinchos de colmena" },
            { ObjectID.FireTrap, "Trampa de fuego" },
            { ObjectID.PoisonTrap, "Trampa de veneno" },
            { ObjectID.GalaxiteTrap, "Trampa de galaxita" },

            // TORRETAS
            { ObjectID.TempleTurret, "Torreta del templo" },
            { ObjectID.GalaxiteTurret, "Torreta de galaxita" },

            // VEHÍCULOS
            { ObjectID.Minecart, "Vagoneta" },
            { ObjectID.WonkyMinecart, "Vagoneta destartalada" },
            { ObjectID.Boat, "Bote" },
            { ObjectID.SpeederBoat, "Bote rápido" },
            { ObjectID.SpeederGoKart, "Kart rápido" },
            { ObjectID.RenegadeGoKart, "Kart renegado" },
            { ObjectID.PrimitiveGoKart, "Kart primitivo" },

            // CIRCUITOS Y ELECTRICIDAD
            { ObjectID.ElectricityGenerator, "Generador eléctrico" },
            { ObjectID.Lever, "Palanca" },
            { ObjectID.ElectricalWire, "Cable eléctrico" },
            { ObjectID.ElectricalDoor, "Puerta eléctrica" },
            { ObjectID.LogicCircuit, "Circuito lógico" },
            { ObjectID.PulseCircuit, "Circuito de pulso" },
            { ObjectID.DelayCircuit, "Circuito de retraso" },
            { ObjectID.CrossCircuit, "Circuito cruzado" },
            { ObjectID.PressurePlate, "Placa de presión" },

            // OBJETOS ESPECIALES
            { ObjectID.Portal, "Portal" },
            { ObjectID.RecallIdol, "Ídolo de retorno" },
            { ObjectID.MapMarker, "Marcador de mapa" },
            { ObjectID.MagicMirror, "Espejo mágico" },
            { ObjectID.AncientFountain, "Fuente antigua" },

            // MASCOTAS
            { ObjectID.EggIncubator, "Incubadora de huevos" },
            { ObjectID.PetBed, "Cama para mascotas" },
            { ObjectID.PetDog, "Perro" },
            { ObjectID.PetCat, "Gato" },
            { ObjectID.PetBird, "Pájaro" },
            { ObjectID.PetBunny, "Conejo" },
            { ObjectID.PetSlimeBlob, "Slime mascota" },

            // GANADO
            { ObjectID.Cow, "Vaca" },
            { ObjectID.CowBaby, "Ternero" },
            { ObjectID.Goat, "Cabra" },
            { ObjectID.GoatBaby, "Cabrito" },
            { ObjectID.RolyPoly, "Bicho bola" },
            { ObjectID.RolyPolyBaby, "Bicho bola bebé" },
            { ObjectID.Turtle, "Tortuga" },
            { ObjectID.TurtleBaby, "Tortuga bebé" },
            { ObjectID.Dodo, "Dodo" },
            { ObjectID.DodoBaby, "Dodo bebé" },
            { ObjectID.Camel, "Camello" },
            { ObjectID.CamelBaby, "Camello bebé" },

            // COMIDA Y PRODUCTOS
            { ObjectID.Milk, "Leche" },
            { ObjectID.Meat, "Carne" },
            { ObjectID.Egg, "Huevo" },
            { ObjectID.LarvaMeat, "Carne de larva" },
            { ObjectID.GoldenLarvaMeat, "Carne de larva dorada" },

            // SEMILLAS Y PLANTAS
            { ObjectID.HeartBerrySeed, "Semilla de baya corazón" },
            { ObjectID.HeartBerryPlant, "Planta de baya corazón" },
            { ObjectID.HeartBerry, "Baya corazón" },
            { ObjectID.GlowingTulipSeed, "Semilla de tulipán brillante" },
            { ObjectID.GlowingTulipPlant, "Planta de tulipán brillante" },
            { ObjectID.GlowingTulipFlower, "Tulipán brillante" },
            { ObjectID.BombPepperSeed, "Semilla de pimienta bomba" },
            { ObjectID.BombPepperPlant, "Planta de pimienta bomba" },
            { ObjectID.BombPepper, "Pimienta bomba" },
            { ObjectID.CarrockSeed, "Semilla de zanahoria roca" },
            { ObjectID.CarrockPlant, "Planta de zanahoria roca" },
            { ObjectID.Carrock, "Zanahoria roca" },
            { ObjectID.PuffungiSeed, "Semilla de puffungi" },
            { ObjectID.PuffungiPlant, "Planta de puffungi" },
            { ObjectID.Puffungi, "Puffungi" },

            // RIELES
            { ObjectID.Rail, "Riel" },

            // SEÑALES
            { ObjectID.SignArrow, "Señal de flecha" },
            { ObjectID.SignSkull, "Señal de calavera" },
            { ObjectID.SignBrute, "Señal de bruto" },
            { ObjectID.SignText, "Señal de texto" },

            // ESTATUAS DE JEFES
            { ObjectID.LarvaBossStatue, "Estatua de Ghorm" },
            { ObjectID.SlimeBossStatue, "Estatua de Glurch" },
            { ObjectID.LarvaHiveBossStatue, "Estatua de la Reina Colmena" },

            // INVOCADORES DE JEFES
            { ObjectID.SlimeBossSummoningItem, "Invocador de Glurch" },
            { ObjectID.LarvaBossSummoningItem, "Invocador de Ghorm" },
            { ObjectID.HiveBossSummoningItem, "Invocador de la Reina Colmena" },
            { ObjectID.ShamanBossSummoningItem, "Invocador de Ivy" },
            { ObjectID.KingSlimeSummoningItem, "Invocador del Rey Slime" },
            { ObjectID.CoreBossSummoningItem, "Invocador del Comandante del Núcleo" },
            { ObjectID.WallBossSummoningItem, "Invocador del Muro Viviente" },
            { ObjectID.GiantCicadaBossSummoningItem, "Invocador de la Cigarra Gigante" },

            // CEBOS DE HIDRA
            { ObjectID.HydraBossNatureBait, "Cebo de Hidra de Naturaleza" },
            { ObjectID.HydraBossSeaBait, "Cebo de Hidra Marina" },
            { ObjectID.HydraBossDesertBait, "Cebo de Hidra del Desierto" }
        };

        /// <summary>
        /// Verifica si un ObjectID es una estación de crafteo.
        /// </summary>
        public static bool IsCraftingStation(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("workbench") ||
                   name.Contains("anvil") ||
                   name.Contains("furnace") ||
                   name.Contains("cookingpot") ||
                   name.Contains("alchemytable") ||
                   name.Contains("table") ||
                   name.Contains("forge") ||
                   name.Contains("carpenter") ||
                   name.Contains("smelter") ||
                   name.Contains("kiln") ||
                   name.Contains("station") ||
                   name.Contains("loom");
        }

        /// <summary>
        /// Verifica si un ObjectID es un enemigo.
        /// </summary>
        public static bool IsEnemy(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("slime") ||
                   name.Contains("larva") ||
                   name.Contains("caveling") ||
                   name.Contains("enemy") ||
                   name.Contains("boss") ||
                   name.Contains("tentacle") ||
                   name.Contains("crab") ||
                   name.Contains("scarab") ||
                   name.Contains("butterfly") ||
                   name.Contains("brute") ||
                   name.Contains("mimite") ||
                   name.Contains("turret") ||
                   name.Contains("worm") ||
                   name.Contains("cicada") ||
                   name.Contains("mushroom") ||
                   name.Contains("ghost") ||
                   name.Contains("snail") ||
                   name.Contains("hydra");
        }

        /// <summary>
        /// Verifica si un ObjectID es un cofre.
        /// </summary>
        public static bool IsChest(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("chest");
        }

        /// <summary>
        /// Verifica si un ObjectID es un mineral o recurso.
        /// </summary>
        public static bool IsOre(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("ore") || name.Contains("boulder") || name.Contains("bar");
        }

        /// <summary>
        /// Obtiene el nombre localizado de un ObjectID.
        /// </summary>
        public static string GetName(ObjectID objectID)
        {
            if (ObjectNames.TryGetValue(objectID, out var name))
            {
                return name;
            }
            return ObjectTypeHelper.GetLocalizedName(objectID);
        }

        /// <summary>
        /// Obtiene la categoría de un ObjectID.
        /// </summary>
        public static string GetCategory(ObjectID objectID)
        {
            if (IsEnemy(objectID)) return "Enemigo";
            if (IsChest(objectID)) return "Cofre";
            if (IsCraftingStation(objectID)) return "Estación de Crafteo";
            if (IsOre(objectID)) return "Mineral";

            var name = objectID.ToString().ToLower();
            if (name.Contains("tool") || name.Contains("pick") || name.Contains("shovel") ||
                name.Contains("hoe") || name.Contains("drill")) return "Herramienta";
            if (name.Contains("torch") || name.Contains("lamp") || name.Contains("lantern")) return "Iluminación";
            if (name.Contains("trap")) return "Trampa";
            if (name.Contains("door") || name.Contains("wall") || name.Contains("floor") ||
                name.Contains("bridge") || name.Contains("fence")) return "Construcción";
            if (name.Contains("seed") || name.Contains("plant")) return "Agricultura";
            if (name.Contains("pet") || name.Contains("cow") || name.Contains("goat") ||
                name.Contains("turtle") || name.Contains("dodo") || name.Contains("camel")) return "Animal";
            if (name.Contains("cart") || name.Contains("boat") || name.Contains("kart")) return "Vehículo";
            if (name.Contains("core")) return "Núcleo";
            if (name.Contains("merchant")) return "Mercader";

            return ObjectTypeHelper.GetCategory(objectID);
        }

        /// <summary>
        /// Verifica si un ObjectID es de almacenamiento (cofres, contenedores).
        /// </summary>
        public static bool IsStorage(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("chest") ||
                   name.Contains("storage") ||
                   name.Contains("container") ||
                   name.Contains("box") ||
                   name.Contains("barrel") ||
                   name.Contains("inventory") ||
                   name.Contains("craftingstation");
        }

        /// <summary>
        /// Verifica si un ObjectID es interactuable.
        /// </summary>
        public static bool IsInteractable(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("door") ||
                   name.Contains("switch") ||
                   name.Contains("lever") ||
                   name.Contains("button") ||
                   name.Contains("torch") ||
                   name.Contains("portal") ||
                   name.Contains("shrine") ||
                   name.Contains("altar") ||
                   name.Contains("crystal") ||
                   name.Contains("monument") ||
                   objectID == ObjectID.TheCore;
        }

        /// <summary>
        /// Verifica si un ObjectID es un enemigo REAL (no estatuas ni decoración).
        /// </summary>
        public static bool IsRealEnemy(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();

            // Excluir estatuas y trofeos
            if (name.Contains("statue") || name.Contains("trophy") || name.Contains("decoration"))
                return false;

            // Lista completa de enemigos reales
            return name.Contains("slime") ||
                   name.Contains("larva") ||
                   name.Contains("grub") ||
                   name.Contains("spider") ||
                   name.Contains("ghorm") ||
                   name.Contains("boss") ||
                   name.Contains("enemy") ||
                   name.Contains("mushroom") ||
                   name.Contains("scarab") ||
                   name.Contains("mold") ||
                   name.Contains("caveling") ||
                   name.Contains("hive") ||
                   name.Contains("blob") ||
                   name.Contains("worm") ||
                   name.Contains("fly") ||
                   name.Contains("wasp") ||
                   name.Contains("bee") ||
                   name.Contains("ant") ||
                   name.Contains("beetle") ||
                   name.Contains("crystal") && name.Contains("crab");
        }

        /// <summary>
        /// Verifica si un ObjectID es destructible.
        /// </summary>
        public static bool IsDestructible(ObjectID objectID)
        {
            var name = objectID.ToString().ToLower();
            return name.Contains("rock") ||
                   name.Contains("stone") ||
                   name.Contains("wall") ||
                   name.Contains("crystal") ||
                   name.Contains("ore") ||
                   name.Contains("vein") ||
                   name.Contains("root") ||
                   name.Contains("tree") ||
                   name.Contains("destructible") ||
                   name.Contains("breakable");
        }
    }
}