#define PUG_RGB_ENABLED
#define PUG_ACHIEVEMENTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using I2.Loc;
using Interaction;
using Inventory;
using PlayerCommand;
using PlayerEquipment;
using PlayerState;
using Pug.ECS.Hybrid;
using Pug.UnityExtensions;
using PugMod;
using PugProperties;
using PugTilemap;
using Rewired;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

public class PlayerController : EntityMonoBehaviour, IManagedUpdate
{
	[Serializable]
	[HideInInspector]
	public class PlayerSettings
	{
		[Tooltip("Dummy field to prevent Unity from getting rid of empty player settings")]
		public int dummy = -1337;
	}

	[Serializable]
	public class FishingRodColorGradient
	{
		public ObjectID fishingRodId;

		public Gradient gradient;
	}

	[Serializable]
	public class CarryableEffect
	{
		public ObjectID objectWithEffect;

		public GameObject effectObject;
	}

	public struct GhostIDWithIndex : IComparable<GhostIDWithIndex>
	{
		public int ghostID;

		public int index;

		public int CompareTo(GhostIDWithIndex other)
		{
			return ghostID.CompareTo(other.ghostID);
		}
	}

	public struct ObjectToDamageECSInfo
	{
		public Entity closestHitEntity;

		public float3 closestEntityPosition;

		public float closestDistance;

		public float3 hitPosition;

		public Entity closestHitGroundEntity;

		public float3 groundEntityPosition;

		public TileInfo groundEntityTileInfo;

		public float closestGroundDistance;

		public bool closestEntityIsTile;

		public bool closestEntityIsDestructible;

		public bool closestEntityIsIndestructible;

		public bool closestEntityIsImmune;

		public bool closestEntityIsTileCollider;

		public bool closestEntityIsGroundDecoration;

		public bool closestEntityHasSurfacePriority;

		public int closestEntitySurfacePrio;

		public static ObjectToDamageECSInfo Default
		{
			get
			{
				ObjectToDamageECSInfo result = default(ObjectToDamageECSInfo);
				result.closestHitEntity = Entity.Null;
				result.closestEntityPosition = float3.zero;
				result.closestDistance = 2.1474836E+09f;
				result.hitPosition = float3.zero;
				result.closestHitGroundEntity = Entity.Null;
				result.groundEntityPosition = float3.zero;
				result.groundEntityTileInfo = default(TileInfo);
				result.closestGroundDistance = 2.1474836E+09f;
				return result;
			}
		}
	}

	private struct CastResult
	{
		public DistanceHit distanceHit;

		public float3 entityPositionAtHit;
	}

	public GameObject fog;

	public Transform footStepTransform;

	public ExtraLootAtStartTable extraLootAtStartTable;

	public RolePerksTable rolePerksTable;

	public bool isLocal;

	public Biome currentBiome;

	public bool isInSafeZone;

	private bool lastLocalGodModeState;

	public ClientInput clientInput;

	[NonSerialized]
	public bool spawningFromCoreFinished;

	[HideInInspector]
	public bool mouseUIInputWasDone;

	[HideInInspector]
	public Entity currentSheetStandBeingPlayedAt;

	[HideInInspector]
	public Portal currentPortal;

	private string unfilteredPlayerName;

	private UIInputActionData lastPoppedUIInputAction;

	private List<UIInputActionData> queuedUIInputActions = new List<UIInputActionData>(16);

	public ClientSystem playerCommandSystem;

	public SendClientSubMapToPugMapSystem pugMapSystem;

	public PugQuerySystem querySystem;

	public UpdateGraphicalObjectTransformSystem updateGraphicalObjectTransformSystem;

	public EntityPrespawnSystem entityPrespawnSystem;

	public BlobAssetReference<PugDatabase.PugDatabaseBank> pugDatabase;

	public BlobAssetReference<LootTableBankBlob> lootTableBank;

	public bool hasDisabledInputBecauseLoading;

	public bool hasRemovedLoginImmunity;

	[Header("Physics settings:")]
	public PhysicsCategoryTags belongsToLayer;

	public PhysicsCategoryTags collidesWithLayer;

	public PhysicsCategoryTags damagesLayer;

	public const float collisionRadius = 0.2f;

	[Header("Movement settings:")]
	public float movementSpeed = 9.6f;

	public float noClipMovementSpeedMultipler = 12.5f;

	public ParticleSystem runDust;

	[NonSerialized]
	public Vector3 targetMovementVelocity = Vector3.zero;

	[NonSerialized]
	public Vector3 targetingDirection = Vector3.zero;

	[NonSerialized]
	public Direction lastEffectiveSteerDir = Direction.zero;

	[NonSerialized]
	public Direction facingDirection = Direction.back;

	[NonSerialized]
	public Vector3 aimDirection = Vector3.back;

	[Header("Inventory settings:")]
	public const int MAX_EQUIPMENT_SLOTS = 10;

	public const int MAX_POUCHES = 4;

	public const int MAX_POUCH_SLOTS = 10;

	[NonSerialized]
	public int equippedSlotIndex;

	private int lastUsedSlotIndex;

	[NonSerialized]
	public int hotbarStartIndex;

	[NonSerialized]
	public int hotbarEndIndex;

	[NonSerialized]
	public PetBase activePet;

	private List<ContainedObjectsBuffer> previousInventoryObjects;

	private ContainedObjectsBuffer previousMouseObject;

	[NonSerialized]
	public PlayInstrumentHandler instrumentHandler;

	[NonSerialized]
	public bool stopPlayingInstrument;

	private EquipmentSlotType visuallyEquippedSlotType;

	[Header("Carryable:")]
	public GameObject carryableHandle;

	public SpriteRenderer carryableSwingItemSprite;

	public SpriteSheetSkin carryableSwingItemSkinSkin;

	public SpriteRenderer carryableRangeItemSprite;

	public SpriteSheetSkin carryableRangeItemSkinSkin;

	public SpriteRenderer carryableShieldItemSprite;

	public SpriteSheetSkin carryableShieldItemSkinSkin;

	public SpriteRenderer instrumentSprite;

	public SpriteSheetSkin instrumentSkin;

	public SpriteRenderer carryablePlaceItemSprite;

	public ColorReplacer carryablePlaceItemColorReplacer;

	public SpriteRenderer carryableBigSpearItemSprite;

	public SpriteRenderer carryableBigSwingItemSprite;

	public SpriteRenderer carryableDrillToolSprite;

	public SpriteSheetSkin carryableBigSpearItemSkin;

	public SpriteSheetSkin carryableBigSwingItemSkin;

	public SpriteSheetSkin carryableDrillToolSkin;

	public GameObject carryableTorch;

	public SpriteRenderer carryableFishingRodSprite;

	public SpriteSheetSkin carryableFishingRodSkin;

	public LineRenderer fishingRodLine;

	public Gradient fishingRodLineDefaultGradient;

	public List<FishingRodColorGradient> fishingRodLineGradientOverrides;

	public SpriteRenderer fishingRodSink;

	public SpriteRenderer fishingRodLoot;

	public Vector2 animatedFishingRodSinkPosition;

	public bool animatedLineIsBeingPulled;

	public ElectricBeamFX beamFx;

	public ElectricBeamFX lightningFx;

	public ElectricBeamFX[] chainLightningFx;

	[ArrayElementTitle("objectWithEffect")]
	public List<CarryableEffect> carryableEffects;

	[Header("References:")]
	public List<SpriteRenderer> renderers;

	public SpriteRenderer skinRenderer;

	public SpriteRenderer eyesSpriteRenderer;

	[ColorUsage(true, true)]
	public Color defaultEyeEmissive;

	[ColorUsage(true, true)]
	public Color redGlowEyeEmissive;

	public UnityEngine.Material defaultmaterial;

	public UnityEngine.Material ghostMaterial;

	public Color ghostColor;

	public Flashable flashableComponent;

	public Flashable flashableEyesComponent;

	public GameObject srContainer;

	public Transform srOffset;

	public AimUI aimUI;

	[Header("Hit Area:")]
	public bool showHitArea;

	public AttackFX attackFX;

	public SpriteRenderer hitAreaBoxSR;

	public SpriteRenderer hitAreaSR;

	public Sprite hitAreaSpriteCone45;

	public Sprite hitAreaSpriteCone90;

	public Sprite hitAreaSpriteCone135;

	public Sprite hitAreaSpriteCone180;

	public Sprite hitAreaSpriteCone270;

	public Sprite hitAreaSpriteCircle;

	public Sprite hitAreaSpriteRectangle;

	[Header("Customization:")]
	public PlayerCustomizationTable customizationTable;

	public PlayerCustomization activeCustomization;

	private int activeCustomizationTriggerCount;

	public SpriteSheetSkin bodySkin;

	public ColorReplacer skinColorReplacer;

	public SpriteSheetSkin hairSkin;

	public ColorReplacer hairColorReplacer;

	public SpriteSheetSkin hairShadeSkin;

	public ColorReplacer hairShadeColorReplacer;

	public SpriteSheetSkin eyesSkin;

	public ColorReplacer eyesColorReplacer;

	public SpriteSheetSkin pantsSkin;

	public ColorReplacer pantsColorReplacer;

	public SpriteSheetSkin shirtSkin;

	public ColorReplacer shirtColorReplacer;

	public SpriteSheetSkin helmSkin;

	public SpriteRenderer helmSR;

	public ColorReplacer helmColorReplacer;

	public SpriteSheetSkin breastArmorSkin;

	public SpriteRenderer breastArmorSR;

	public ColorReplacer breastArmorColorReplacer;

	public SpriteSheetSkin pantsArmorSkin;

	public SpriteRenderer pantsArmorSR;

	public ColorReplacer pantsArmorColorReplacer;

	public PugText nameText;

	public PugText nameTextOutline;

	private string _prevName = "";

	private bool prevLookLikeGhost;

	[Header("Tile effects:")]
	private PoolableAudioSource acidLoop;

	private bool onSlime;

	private bool onOrangeSlime;

	private bool onAcid;

	private bool onPoisonSlime;

	private bool onSlipperySlime;

	private bool onWood;

	private bool onGrass;

	private bool onChrysalis;

	private bool onMoss;

	private bool onStone;

	private bool onFlesh;

	private bool onSand;

	private bool onOasis;

	private bool onBeach;

	private bool onClay;

	private bool onMold;

	private bool onDirt;

	private bool onMetal;

	private bool onRoses;

	private bool onGlass;

	private bool onMeadow;

	[Header("Effects:")]
	public ParticleSystem teleportEffect;

	public GameObject teleportSprites;

	private PoolableAudioSource castAudio;

	public ParticleSystem gainTalentEffect;

	public ParticleSystem coreWallParticles;

	public ParticleSystem dashEffect;

	private bool _anyInventoryOrMapWasActiveThisFrame;

	private bool _playerInputBlockedThisFrame;

	[HideInInspector]
	public Vector3 previousMouseScreenPosition;

	private int swappedTorchFromIndex = -1;

	private TimerSimple slotSwapWithMouseCooldownTimer = new TimerSimple(0.02f);

	private static readonly int GreatWallHeight = Shader.PropertyToID("GreatWallHeight");

	private static readonly int GreatWallEmissivity = Shader.PropertyToID("GreatWallEmissivity");

	private static readonly int GreatWallParticles = Shader.PropertyToID("GreatWallParticles");

	public AnimationCurve greatWallHeightCurve;

	public AnimationCurve greatWallEmissiveCurve;

	public AnimationCurve greatWallParticlesCurve;

	private bool greatWallIsShakingAndRumbling;

	private bool greatWallEmissiveSoundHasPlayed;

	private bool greatWallHasBeenLowered;

	private TimerSimple greatWallShakeTimer = new TimerSimple(0.15f);

	private float nearbyEmissiveAndLowerWallBlend;

	private const int distanceToStartNearbyWallEffect = 4;

	private TimerSimple playerNearbyTimer;

	private bool canDoHandEmote;

	private bool isPlayingInteractFlash;

	private const float UNDISCOVERED_ITEM_CHECK_INTERVAL = 0.5f;

	private TimerSimple checkUndiscoveredItemsTimer = new TimerSimple(0.5f);

	public AnimationCurve slackingFishingLineCurve;

	public AnimationCurve throwingFishingLineCurve;

	private float lineTensionSmoothTimer;

	private float sinkHeightFromFishHooking;

	private TimerSimple rippleTimer;

	private PoolableAudioSource reelAudio;

	private PoolableAudioSource struggleAudio;

	private const float DRILL_START_SOUND_DURATION = 0.48f;

	private static readonly int Drilling = Animator.StringToHash("drilling");

	private readonly List<AudioManager.RunningSfxReference> drillSoundAudioSources = new List<AudioManager.RunningSfxReference>();

	private TimerSimple drillStartSoundTimer = new TimerSimple(0.48f);

	private bool drillIsLooping;

	private List<AudioManager.RunningSfxReference> beamSoundAudioSources = new List<AudioManager.RunningSfxReference>();

	private List<AudioManager.RunningSfxReference> beamSoundHittingAudioSources = new List<AudioManager.RunningSfxReference>();

	private TimerSimple beamStartSoundTimer = new TimerSimple(0.48f);

	private bool beamIsLooping;

	private TimerSimple thisIsGoingToTakeAWhileEmoteTimer;

	private NetworkTick thisIsGoingToTakeAWhileLastDamageTick;

	private bool _windupSoundsPlaying;

	private readonly List<AudioManager.RunningSfxReference> _windupSoundEffects = new List<AudioManager.RunningSfxReference>();

	private Vector4[] tulipPositionsArray;

	private static readonly int TulipPositionsArray = Shader.PropertyToID("TulipPositionsArray");

	private const int MAX_TULIPS = 8;

	private List<ContainedObjectsBuffer> inventoryCache = new List<ContainedObjectsBuffer>();

	private int lastReceivedConditionsAmount;

	private TimerSimple conditionsSaveTimer = new TimerSimple(1f, unscaled: false, startTimer: false);

	private static readonly int Emissive = Shader.PropertyToID("_Emissive");

	private int lastReceivedLockedObjectsAmount;

	private TimerSimple lockedObjectsSaveTimer = new TimerSimple(1f, unscaled: false, startTimer: false);

	private const float MIN_TIME_BETWEEN_BLINK = 8f;

	private const float MAX_TIME_BETWEEN_BLINK = 12f;

	private TimerSimple blinkTimer = new TimerSimple(5f);

	private Vector3 srOffSetPos;

	private readonly Vector3 defaultSROffSetPos = new Vector3(0f, 0.6875f, -0.0585f);

	private Quaternion srOffSetRot;

	private static readonly int EmissiveTex = Shader.PropertyToID("_EmissiveTex");

	protected const string rareTerm = "Rarity/Rare";

	protected const string epicTerm = "Rarity/Epic";

	protected const string foodFormat = "foodFormat";

	protected const string rare = "Rare";

	protected const string epic = "Epic";

	protected const string foodAdjectives = "FoodAdjectives/";

	protected const string foodNouns = "FoodNouns/";

	protected const string items = "Items/";

	protected const string rareFormat = "rareItemFormat";

	protected const string male = "Male";

	protected const string female = "Female";

	private float _checkedManaTime;

	private bool _checkedShouldShowManaLastValue;

	private bool defaultHelmOffsetInitialized;

	private Vector3 defaultHelmOffset;

	private const string skillIncreasePrefix = "SkillIncrease/";

	private const string skillsPrefix = "Skills/";

	public LeashPoints leashPoints;

	[NonSerialized]
	public bool isDyingOrDead;

	private TimerSimple hungerEmoteTimer = new TimerSimple(30f);

	private int previousHunger;

	protected override bool hideDirectlyOnDeath => false;

	public Entity smoothFollowEntity { get; private set; }

	public bool IsHardcore => EntityUtility.GetComponentData<HardcoreCD>(base.entity, base.world).isHardcore;

	public bool initialized { get; private set; }

	public string playerName { get; private set; } = "";


	public int playerIndex { get; private set; }

	public string networkName { get; private set; }

	public PlatformUserID platformID { get; private set; }

	public Platform platform { get; private set; }

	public int adminPrivileges
	{
		get
		{
			if (!EntityUtility.HasComponentData<PlayerGhost>(base.entity, base.world))
			{
				return 0;
			}
			return EntityUtility.GetComponentData<PlayerGhost>(base.entity, base.world).adminPrivileges;
		}
	}

	public Vector3 SmoothWorldPosition => EntityUtility.GetComponentData<LocalToWorld>(smoothFollowEntity, base.world).Position;

	public float SmoothSpeed
	{
		set
		{
			EntityUtility.SetComponentData(smoothFollowEntity, base.world, new VisualSmoothFollowSpeedCD
			{
				Value = value
			});
		}
	}

	private PlayerInput _inputModule { get; set; }

	public virtual PlayerInput inputModule
	{
		get
		{
			if (!isLocal)
			{
				throw new InvalidOperationException("Trying to access input for non-local player");
			}
			if (_inputModule == null)
			{
				_inputModule = Manager.input.singleplayerInputModule;
			}
			return _inputModule;
		}
	}

	public bool reorientationBlocked
	{
		get
		{
			return EntityUtility.GetComponentData<PlayerOrientationCD>(base.entity, base.world).reorientationBlocked;
		}
		set
		{
			if (base.world != null && EntityUtility.HasComponentData<PlayerOrientationCD>(base.entity, base.world))
			{
				EntityUtility.SetComponentData(base.entity, base.world, new PlayerOrientationCD
				{
					reorientationBlocked = value
				});
			}
		}
	}

	public bool guestMode
	{
		get
		{
			if (querySystem.TryGetSingleton<WorldInfoCD>(out var value) && value.guestMode)
			{
				return adminPrivileges < 1;
			}
			return false;
		}
	}

	public bool pvpMode
	{
		get
		{
			if (querySystem.TryGetSingleton<WorldInfoCD>(out var value))
			{
				return value.pvpEnabled;
			}
			return false;
		}
	}

	public bool isInteractionBlocked
	{
		get
		{
			if (!Manager.ui.isAnyInventoryShowing && !Manager.ui.isShowingMap && !guestMode && !Manager.menu.IsAnyMenuActive() && EntityUtility.GetConditionEffectValue(ConditionEffect.Stunned, base.entity, base.world) <= 0 && !instrumentHandler.IsPlayingInstrument)
			{
				return EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.IgnoreAllInput);
			}
			return true;
		}
	}

	private bool isMovingBlocked
	{
		get
		{
			if (!Manager.ui.isAnyInventoryShowing && !Manager.menu.IsAnyMenuActive() && !EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.Fishing | PlayerStateEnum.IgnoreAllInput))
			{
				return instrumentHandler.IsPlayingInstrument;
			}
			return true;
		}
	}

	public bool isAimingBlocked => EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.IgnoreAllInput);

	private bool isUIShortCutsBlocked
	{
		get
		{
			if (!Manager.input.textInputWasActiveThisFrame && !Manager.menu.IsAnyMenuActive() && !instrumentHandler.IsPlayingInstrument && !EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.IgnoreAllInput))
			{
				return EntityUtility.GetComponentData<HealthCD>(base.entity, base.world).health <= 0;
			}
			return true;
		}
	}

	public bool isSteeringBlocked
	{
		get
		{
			if (!Manager.ui.isAnyInventoryShowing && !Manager.menu.IsAnyMenuActive())
			{
				return EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.Fishing | PlayerStateEnum.IgnoreAllInput);
			}
			return true;
		}
	}

	private int equippedVisualSlotIndex => equippedSlotIndex - hotbarStartIndex;

	private List<EquipmentSlot> equipmentSlots { get; } = new List<EquipmentSlot>();


	public InventoryHandler playerInventoryHandler => playerCraftingHandler?.inventoryHandler;

	public InventoryHandler mouseInventoryHandler => playerCraftingHandler?.outputInventoryHandler;

	public InventoryHandler activeInventoryHandler { get; private set; }

	public InventoryHandler activeBuyInventoryHandler { get; private set; }

	public Cattle activeCattle { get; private set; }

	public SignText activeSign { get; private set; }

	[HideInInspector]
	public CraftingHandler activeCraftingHandler { get; private set; }

	public CraftingHandler playerCraftingHandler { get; private set; }

	public SellSlotsHandler sellSlotsHandler { get; private set; }

	public EquipmentHandler equipmentHandler { get; private set; }

	public VanitySlotsHandler vanitySlotsHandler { get; private set; }

	public TrashCanHandler trashCanHandler { get; private set; }

	public UpgradeSlotHandler upgradeSlotHandler { get; private set; }

	public ContainedObjectsBuffer visuallyEquippedContainedObject { get; private set; }

	private bool slotSwapWithMouseIsOnCooldown
	{
		get
		{
			if (slotSwapWithMouseCooldownTimer.isRunning)
			{
				return !slotSwapWithMouseCooldownTimer.isTimerElapsed;
			}
			return false;
		}
	}

	public bool playerCanInteractWithGreatWall { get; private set; }

	public int activeEquipmentPreset { get; private set; }

	public HungerCD hungerComponent => EntityUtility.GetComponentData<HungerCD>(base.entity, base.world);

	public InteractableObject GetCurrentInteractableObject()
	{
		EntityUtility.TryGetComponentData<InteractableObjectReferenceCD>(EntityUtility.GetComponentData<InteractorCD>(base.entity, base.world).currentClosestInteractable, base.world, out var value);
		return value.Value.Value;
	}

	public bool TryPopUIInputActionData(out UIInputActionData uiInputActionData)
	{
		uiInputActionData = default(UIInputActionData);
		if (queuedUIInputActions.Count == 0)
		{
			lastPoppedUIInputAction.action = UIInputAction.None;
			return false;
		}
		lastPoppedUIInputAction = queuedUIInputActions[0];
		queuedUIInputActions.RemoveAt(0);
		uiInputActionData = lastPoppedUIInputAction;
		return true;
	}

	public UIInputActionData GetLastUIInputAction()
	{
		return lastPoppedUIInputAction;
	}

	public UIInputActionData GetNextUIInputAction()
	{
		if (queuedUIInputActions.Count == 0)
		{
			return default(UIInputActionData);
		}
		return queuedUIInputActions[0];
	}

	public void QueueInputAction(UIInputActionData inputActionData)
	{
		queuedUIInputActions.Add(inputActionData);
	}

	public void SetGodMode(bool enabled)
	{
		lastLocalGodModeState = enabled;
		playerCommandSystem.SetGodMode(base.entity, enabled);
		Manager.prefs.GodModeEnabled = enabled;
	}

	public bool GetLastLocalGodModeState()
	{
		return lastLocalGodModeState;
	}

	public Color GetPlayerColor(bool ignoreTeamColor = false)
	{
		if (playerIndex == 0)
		{
			return Color.white;
		}
		if (!ignoreTeamColor && pvpMode)
		{
			return GetTeamColor();
		}
		int colorIndex = playerIndex - 1;
		return GetPlayerColor(colorIndex);
	}

	public Color GetTeamColor()
	{
		if (!EntityUtility.HasComponentData<FactionCD>(base.entity, base.world))
		{
			return Color.white;
		}
		int pvpTeam = EntityUtility.GetComponentData<FactionCD>(base.entity, base.world).pvpTeam;
		return GetPlayerColor(pvpTeam);
	}

	public Color GetPlayerColor(int colorIndex)
	{
		if (colorIndex < 0)
		{
			return Color.white;
		}
		for (int i = Manager.ui.playerColors.Count; i <= colorIndex; i++)
		{
			Manager.ui.playerColors.Add(PugRandom.Color(i));
		}
		return Manager.ui.playerColors[colorIndex];
	}

	public Vector3 GetEntityPosition()
	{
		return EntityUtility.GetComponentData<LocalToWorld>(base.entity, base.world).Position;
	}

	public void ToggleNoClip()
	{
		bool noClipActive = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.NoClip);
		SetNoClipActive(noClipActive);
	}

	public void SetNoClipActive(bool value)
	{
		playerCommandSystem.SetPlayerState(base.entity, value ? PlayerStateEnum.Walk : PlayerStateEnum.NoClip);
	}

	public void ToggleManualCamera()
	{
		bool flag = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.IgnoreAllInput);
		playerCommandSystem.SetPlayerState(base.entity, flag ? PlayerStateEnum.Walk : PlayerStateEnum.IgnoreAllInput);
	}

	public bool StateAllowsAimUI()
	{
		if (EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.VehicleRiding))
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool StateAllowsAimUI(in PlayerStateCD playerStateCD)
	{
		return !playerStateCD.HasAnyState(PlayerStateEnum.VehicleRiding);
	}

	public bool CurrentStateAllowInteractions(bool isTryingToUseSecondInteract, bool isTryingToInteractWithObject = false)
	{
		querySystem.TryGetSingleton<WorldInfoCD>(out var value);
		PlayerGhost playerGhost = EntityUtility.GetComponentData<PlayerGhost>(base.entity, base.world);
		PlayerStateCD playerStateCD = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world);
		EquipmentSlotCD equippedSlot = EntityUtility.GetComponentData<EquipmentSlotCD>(base.entity, base.world);
		ClientInput componentData = EntityUtility.GetComponentData<ClientInput>(base.entity, base.world);
		return CurrentStateAllowInteractions(in value, in playerGhost, in playerStateCD, in equippedSlot, isTryingToUseSecondInteract, in componentData, isTryingToInteractWithObject);
	}

	public static bool CurrentStateAllowInteractions(in WorldInfoCD worldInfoCD, in PlayerGhost playerGhost, in PlayerStateCD playerStateCD, in EquipmentSlotCD equippedSlot, bool isTryingToUseSecondInteract, in ClientInput clientInput, bool isTryingToInteractWithObject = false)
	{
		if (IsGuestMode(in worldInfoCD, in playerGhost))
		{
			return false;
		}
		EquipmentSlotType slotType = equippedSlot.slotType;
		if (playerStateCD.HasAnyState(PlayerStateEnum.Sitting))
		{
			if (!isTryingToInteractWithObject)
			{
				if (isTryingToUseSecondInteract)
				{
					if (slotType != EquipmentSlotType.EatableSlot)
					{
						return slotType == EquipmentSlotType.InstrumentSlot;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		if (playerStateCD.HasAnyState(PlayerStateEnum.PlayingInstrument))
		{
			return clientInput.IsButtonStateSet(CommandInputButtonStateNames.StopPlayingInstrument_Pressed);
		}
		if (playerStateCD.HasAnyState(PlayerStateEnum.VehicleRiding))
		{
			if (isTryingToUseSecondInteract)
			{
				return slotType == EquipmentSlotType.EatableSlot;
			}
			return false;
		}
		if (playerStateCD.HasAnyState(PlayerStateEnum.SpawningFromCore | PlayerStateEnum.Release | PlayerStateEnum.Death | PlayerStateEnum.Sleep | PlayerStateEnum.Teleporting))
		{
			return false;
		}
		if (!isTryingToUseSecondInteract)
		{
			return true;
		}
		bool num = slotType == EquipmentSlotType.MeleeWeaponSlot || slotType == EquipmentSlotType.ShovelSlot || slotType == EquipmentSlotType.PlaceObjectSlot || slotType == EquipmentSlotType.EatableSlot || slotType == EquipmentSlotType.WaterCanSlot || slotType == EquipmentSlotType.RangeWeaponSlot || slotType == EquipmentSlotType.HoeSlot || slotType == EquipmentSlotType.PaintToolSlot || slotType == EquipmentSlotType.FishingRodSlot || slotType == EquipmentSlotType.BugNet || slotType == EquipmentSlotType.BucketSlot || slotType == EquipmentSlotType.RoofingToolSlot || slotType == EquipmentSlotType.SummoningWeaponSlot || slotType == EquipmentSlotType.EquipGearSlot || slotType == EquipmentSlotType.SeederSlot;
		bool flag = slotType == EquipmentSlotType.InstrumentSlot && clientInput.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_Pressed);
		if (!(num || flag))
		{
			return playerStateCD.HasNoneState(PlayerStateEnum.MinecartRiding | PlayerStateEnum.BoatRiding);
		}
		return true;
	}

	private static bool IsGuestMode(in WorldInfoCD worldInfoCD, in PlayerGhost playerGhost)
	{
		if (worldInfoCD.guestMode)
		{
			return playerGhost.adminPrivileges < 1;
		}
		return false;
	}

	public bool LocalPlayerIsTryingToAttack()
	{
		bool flag = clientInput.IsButtonStateSet(CommandInputButtonStateNames.Interact_HeldDown);
		bool flag2 = clientInput.IsButtonStateSet(CommandInputButtonStateNames.SecondInteract_HeldDown);
		querySystem.TryGetSingleton<WorldInfoCD>(out var value);
		PlayerGhost playerGhost = EntityUtility.GetComponentData<PlayerGhost>(base.entity, base.world);
		PlayerStateCD playerStateCD = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world);
		EquipmentSlotCD equippedSlot = EntityUtility.GetComponentData<EquipmentSlotCD>(base.entity, base.world);
		if (Time.timeScale != 0f && Manager.main.currentSceneHandler.isSceneHandlerReady && !Manager.load.IsLoadingAndScreenBlack() && (flag || flag2))
		{
			if (!playerStateCD.HasAnyState(PlayerStateEnum.Release))
			{
				return CurrentStateAllowInteractions(in value, in playerGhost, in playerStateCD, in equippedSlot, !flag, in clientInput);
			}
			return true;
		}
		return false;
	}

	public bool IsShielded()
	{
		PlayerStateCD componentData = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world);
		UseOffHandStateCD componentData2 = EntityUtility.GetComponentData<UseOffHandStateCD>(base.entity, base.world);
		return IsShielded(componentData, componentData2);
	}

	public static bool IsShielded(PlayerStateCD playerStateCD, UseOffHandStateCD useOffHandStateCD)
	{
		if (playerStateCD.HasAnyState(PlayerStateEnum.UseOffHand))
		{
			return useOffHandStateCD.shieldedAmount != 0f;
		}
		return false;
	}

	public void SetActiveInventoryHandler(InventoryHandler inventoryHandler)
	{
		activeInventoryHandler = inventoryHandler;
	}

	public void SetActiveBuyInventoryHandler(InventoryHandler inventoryHandler)
	{
		activeBuyInventoryHandler = inventoryHandler;
	}

	public void SetActiveCattle(Cattle cattle)
	{
		activeCattle = cattle;
	}

	public void SetActiveSign(SignText sign)
	{
		activeSign = sign;
	}

	public void SetActiveCraftingHandler(CraftingHandler craftingHandler)
	{
		if (activeCraftingHandler != null && craftingHandler != null)
		{
			Manager.ui.HideAllInventoryAndCraftingUI();
			SetActiveInventoryHandler(null);
			SetActiveBuyInventoryHandler(null);
		}
		activeCraftingHandler = craftingHandler;
	}

	protected override void Awake()
	{
		base.Awake();
		if (fog != null)
		{
			fog.SetActive(value: false);
		}
		facingDirection = Direction.back;
		reorientationBlocked = false;
	}

	public override void OnOccupied()
	{
		querySystem = base.world.GetExistingSystemManaged<PugQuerySystem>();
		base.OnOccupied();
		if (fog != null)
		{
			fog.SetActive(value: false);
		}
		if (acidLoop != null)
		{
			acidLoop.StopNow();
		}
		spawningFromCoreFinished = false;
		smoothFollowEntity = Entity.Null;
		playerIndex = 0;
		swappedTorchFromIndex = -1;
		playerCanInteractWithGreatWall = false;
		activeInventoryHandler = null;
		activeCraftingHandler = null;
		querySystem = base.world.GetExistingSystemManaged<PugQuerySystem>();
		updateGraphicalObjectTransformSystem = base.world.GetExistingSystemManaged<UpdateGraphicalObjectTransformSystem>();
		entityPrespawnSystem = base.world.GetExistingSystemManaged<EntityPrespawnSystem>();
		currentBiome = EntityUtility.GetComponentData<CurrentBiomeCD>(base.entity, base.world).biome;
		if (currentBiome == Biome.None)
		{
			currentBiome = Biome.Slime;
		}
		isLocal = EntityUtility.IsComponentEnabled<GhostOwnerIsLocal>(base.entity, base.world);
		Entity srcEntity = Entity.Null;
		using (EntityQuery entityQuery = base.world.EntityManager.CreateEntityQuery(typeof(PugPrefabBuffer)))
		{
			DynamicBuffer<PugPrefabBuffer> buffer = EntityUtility.GetBuffer<PugPrefabBuffer>(entityQuery.GetSingletonEntity(), base.world);
			for (int i = 0; i < buffer.Length; i++)
			{
				if (EntityUtility.HasComponentData<VisualSmoothFollowEntityCD>(buffer[i].Value, base.world))
				{
					srcEntity = buffer[i].Value;
				}
			}
		}
		GhostOwner ghostOwner = default(GhostOwner);
		if (EntityUtility.HasComponentData<GhostOwner>(base.entity, base.world))
		{
			ghostOwner = EntityUtility.GetComponentData<GhostOwner>(base.entity, base.world);
		}
		if (!querySystem.TryGetSingleton<NetworkId>(out var value))
		{
			Debug.LogError("Player create: No NetworkId");
			value.Value = -1;
		}
		if (ghostOwner.NetworkId != -1 && ghostOwner.NetworkId == value.Value)
		{
			if (fog != null)
			{
				fog.SetActive(value: true);
			}
			lastReceivedConditionsAmount = 0;
			lastReceivedLockedObjectsAmount = 0;
		}
		smoothFollowEntity = base.world.EntityManager.Instantiate(srcEntity);
		EntityUtility.SetComponentData(smoothFollowEntity, base.world, new VisualSmoothFollowEntityCD
		{
			Value = base.entity
		});
		if (isLocal)
		{
			clientInput = default(ClientInput);
			Manager.main.player = this;
		}
		PlayerInit();
		if (isLocal)
		{
			Manager.camera.SnapCameraToPlayer();
			Manager.ui.inventoryButton.HideLightUpHint();
			Manager.ui.mapButton.HideLightUpHint();
			Manager.achievements.CheckAndTriggerAchievementsOnInit(this);
			InitTulipPositionsArray();
			playerCommandSystem.SetSquashBugs(base.entity, Manager.prefs.squashBugs);
			PrefsManager prefs = Manager.prefs;
			prefs.OnSquashBugsChanged = (Action<bool>)Delegate.Combine(prefs.OnSquashBugsChanged, new Action<bool>(OnSquashBugsChanged));
			if (Manager.sceneHandler.isDev)
			{
				playerCommandSystem.SetPlayerState(base.entity, PlayerStateEnum.NoClip);
			}
		}
		else
		{
			List<PlayerController> nonLocalPlayers = Manager.main.nonLocalPlayers;
			if (!nonLocalPlayers.Contains(this))
			{
				nonLocalPlayers.Add(this);
			}
		}
		instrumentHandler = new PlayInstrumentHandler(this);
		PlayerGhost componentData = EntityUtility.GetComponentData<PlayerGhost>(base.entity, base.world);
		platformID = new PlatformUserID(componentData.onlineId);
		platform = (Platform)componentData.platform;
		if (!Manager.main.allPlayers.Contains(this))
		{
			Manager.main.allPlayers.Add(this);
		}
		Manager.update.AddToUpdate(this);
	}

	protected void PlayerInit()
	{
		initialized = true;
		visuallyEquippedContainedObject = default(ContainedObjectsBuffer);
		ShowCarryable();
		if (Manager.sceneHandler.isInGame)
		{
			_inputModule = null;
		}
		inventoryCache.Clear();
		hasDisabledInputBecauseLoading = false;
		hasRemovedLoginImmunity = false;
		animator.applyRootMotion = false;
		greatWallIsShakingAndRumbling = false;
		greatWallEmissiveSoundHasPlayed = false;
		greatWallHasBeenLowered = false;
		playerCommandSystem = base.world.GetExistingSystemManaged<ClientSystem>();
		pugMapSystem = base.world.GetExistingSystemManaged<SendClientSubMapToPugMapSystem>();
		using EntityQuery entityQuery = base.world.EntityManager.CreateEntityQuery(typeof(PugDatabase.DatabaseBankCD));
		pugDatabase = entityQuery.GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
		using EntityQuery entityQuery2 = base.world.EntityManager.CreateEntityQuery(typeof(LootTableBankCD));
		lootTableBank = entityQuery2.GetSingleton<LootTableBankCD>().Value;
		lastLocalGodModeState = Manager.saves.IsCreativeModeCharacter();
		if (lastLocalGodModeState)
		{
			SetGodMode(Manager.prefs.GodModeEnabled);
		}
		reorientationBlocked = false;
		currentPortal = null;
		isDyingOrDead = false;
		animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		activeCustomization = default(PlayerCustomization);
		HideAllEquippedSlotVisuals();
		InitCustomization();
		MakePlayerLookLikeGhost(value: false);
		InitInventoryCraftingAndEquipment();
		if (isLocal)
		{
			_prevName = "";
			nameText.Render("");
			SetInvincibility(Manager.prefs.playerInvincible);
		}
	}

	public override void Free()
	{
		if (!base.isPooled)
		{
			OnFree();
		}
		else
		{
			base.Free();
		}
	}

	public override void OnDestroy()
	{
		Dispose();
		base.OnDestroy();
	}

	private void Dispose()
	{
		PrefsManager prefs = Manager.prefs;
		prefs.OnSquashBugsChanged = (Action<bool>)Delegate.Remove(prefs.OnSquashBugsChanged, new Action<bool>(OnSquashBugsChanged));
	}

	public override void OnFree()
	{
		Debug.Log("OnFree called for " + playerName + " (" + base.name + ")");
		if (initialized)
		{
			FreeAllEquipmentSlots();
		}
		if (isLocal)
		{
			if (!Manager.ecs.IsClosing && querySystem.TryGetSingleton<ConnectionState>(out var value) && value.CurrentState != ConnectionState.State.Disconnected)
			{
				Debug.LogError("Main player despawned while still connected to server");
				if (querySystem.TryGetSingletonEntity<NetworkStreamConnection>(out var value2))
				{
					base.world.EntityManager.AddComponentData(value2, new NetworkStreamRequestDisconnect
					{
						Reason = NetworkStreamDisconnectReason.ConnectionClose
					});
				}
			}
			UpdateInventory();
			Manager.main.player = null;
			PrefsManager prefs = Manager.prefs;
			prefs.OnSquashBugsChanged = (Action<bool>)Delegate.Remove(prefs.OnSquashBugsChanged, new Action<bool>(OnSquashBugsChanged));
		}
		EntityUtility.DestroyEntity(smoothFollowEntity, base.world);
		StopDrillSounds();
		StopBeamSounds();
		if (!isLocal && Manager.main.nonLocalPlayers.Contains(this))
		{
			Manager.main.nonLocalPlayers.Remove(this);
		}
		if (Manager.main.allPlayers.Contains(this))
		{
			Manager.main.allPlayers.Remove(this);
		}
		initialized = false;
		Dispose();
		Manager.update.RemoveFromUpdate(this);
		updateGraphicalObjectTransformSystem.RemoveTransformOverride(base.transform);
		networkName = null;
		base.OnFree();
	}

	private void OnSquashBugsChanged(bool value)
	{
		playerCommandSystem?.SetSquashBugs(base.entity, value);
	}

	public override void ManagedLateUpdate()
	{
		base.ManagedLateUpdate();
		if (isLocal)
		{
			UpdateGreatWall();
		}
		UpdateAnyNewCustomization();
		UpdateName();
		UpdateGearCustomization();
		UpdateBlinking();
		UpdateAnimSROffset();
		UpdateAnimSROffsetRot();
		UpdateDrillAndBeamToolVisuals();
		UpdateEmotes();
		if (!isLocal)
		{
			nameText.transform.localScale = Manager.ui.CalcGameplayUITargetScaleMultiplier();
		}
	}

	public bool AnyInventoryOrMapWasActiveThisFrame()
	{
		return _anyInventoryOrMapWasActiveThisFrame;
	}

	public bool PlayerInputBlockedThisFrame()
	{
		return _playerInputBlockedThisFrame;
	}

	public void ManagedUpdate()
	{
		Biome biome = EntityUtility.GetComponentData<CurrentBiomeCD>(base.entity, base.world).biome;
		if (biome != 0)
		{
			currentBiome = biome;
		}
		if (playerIndex == 0)
		{
			playerIndex = EntityUtility.GetComponentData<PlayerGhost>(base.entity, base.world).playerIndex;
		}
		UpdateRenderersEnabled();
		UpdateConditionsVisuals();
		UpdateInventorySize();
		MelodyData.Update();
		if (!isLocal)
		{
			UpdateEquipmentPreset();
			UpdateEquippedSlotVisuals();
			UpdateOnTileEffects();
			UpdateRunDust();
			return;
		}
		clientInput.useFishingMiniGame = Manager.prefs.fishingMiniGameEnabled;
		if (querySystem.TryGetSingleton<ConnectionState>(out var value) && value.CurrentState == ConnectionState.State.Disconnected)
		{
			Free();
			return;
		}
		if (!Manager.sceneHandler.isSceneHandlerReady || Manager.load.IsScreenFadingOutOrBlack())
		{
			hasDisabledInputBecauseLoading = true;
			inputModule.DisableInputFor(0.2f);
		}
		else if (hasDisabledInputBecauseLoading)
		{
			hasDisabledInputBecauseLoading = false;
			inputModule.EnableInput();
		}
		else if (!hasRemovedLoginImmunity)
		{
			hasRemovedLoginImmunity = true;
			playerCommandSystem.RemoveLoginImmunity(base.entity);
		}
		UpdateInventoryCache();
		UpdateConditions();
		UpdateLockedObjects();
		UpdateHitArea();
		UpdateHungerEmote();
		if (!checkUndiscoveredItemsTimer.isRunning || checkUndiscoveredItemsTimer.isTimerElapsed)
		{
			UpdateDiscoveredItems();
			checkUndiscoveredItemsTimer.Start(0.5f);
		}
		_playerInputBlockedThisFrame = SendClientInputSystem.PlayerInputBlocked();
		_anyInventoryOrMapWasActiveThisFrame = Manager.ui.isAnyInventoryShowing || Manager.ui.isShowingMap;
		equipmentHandler.FixAnyIssues();
		UpdateAllEquipmentSlots();
		instrumentHandler.UpdateInput();
		if (Time.timeScale == 0f || EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).isStateLocked || !Manager.main.currentSceneHandler.isSceneHandlerReady)
		{
			return;
		}
		if (Manager.load.IsLoadingAndScreenBlack())
		{
			targetMovementVelocity = Vector2.zero;
			targetingDirection = Vector2.zero;
			return;
		}
		if (InventoryShortCutsButton.ShortcutsCanBeToggled() && !Manager.input.textInputWasActiveThisFrame && inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.TOGGLE_SHORTCUTS_WINDOW))
		{
			InventoryShortCutsButton.ToggleInventoryShortcuts();
		}
		bool flag = EntityUtility.GetComponentData<HealthCD>(base.entity, base.world).health <= 0;
		bool num = !isUIShortCutsBlocked;
		if (num && (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.TOGGLE_MAP) || (Manager.ui.isShowingMap && inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.CANCEL))))
		{
			Manager.ui.OnMapToggle();
		}
		if (num)
		{
			if (guestMode)
			{
				if (Manager.ui.isPlayerInventoryShowing)
				{
					CloseAnyOpenInventory();
				}
			}
			else if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.TOGGLE_INVENTORY))
			{
				if (Manager.ui.isPlayerInventoryShowing)
				{
					CloseAnyOpenInventory();
				}
				else
				{
					OpenPlayerInventory();
				}
			}
			else if (Manager.ui.isAnyInventoryShowing && inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.CANCEL))
			{
				CloseAnyOpenInventory();
			}
		}
		if (!isSteeringBlocked && !flag)
		{
			Vector2 vector = ProcessMovementInput(inputModule.GetInputAxisValue(PlayerInput.InputAxisType.CHARACTER_MOVEMENT_HORIZONTAL, PlayerInput.InputAxisType.CHARACTER_MOVEMENT_VERTICAL));
			targetMovementVelocity = new Vector3(0f, 0f, 0f);
			if (!isMovingBlocked)
			{
				targetMovementVelocity = new Vector3(vector.x, 0f, vector.y);
			}
			Direction direction = Direction.FromVector(targetMovementVelocity, 0.01f);
			if (EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasNoneState(PlayerStateEnum.VehicleRiding) && !reorientationBlocked && !Manager.ui.isShowingMap)
			{
				if (inputModule.PrefersKeyboardAndMouse())
				{
					Vector3 vector2 = Manager.camera.uiCamera.WorldToScreenPoint(Manager.ui.mouse.pointer.transform.position);
					targetingDirection = aimDirection;
					if (Manager.prefs.faceMouseDirection)
					{
						EquipmentSlot equippedSlot = GetEquippedSlot();
						Direction direction2;
						if (equippedSlot != null && equippedSlot.GetSlotType() == EquipmentSlotType.RangeWeaponSlot)
						{
							direction2 = Direction.FromVector(aimDirection);
						}
						else
						{
							Vector3 vector3 = Manager.camera.gameCamera.WorldToScreenPoint(base.RenderPosition + Vector3.up * 0.5f);
							Vector3 vector4 = vector2 - vector3;
							vector4 = new Vector3(vector4.x, 0f, vector4.y).normalized;
							direction2 = Direction.FromVector(vector4);
						}
						if (vector2 != previousMouseScreenPosition)
						{
							facingDirection = direction2;
							previousMouseScreenPosition = vector2;
						}
					}
					else
					{
						if (!direction.is0)
						{
							facingDirection = direction;
						}
						if (Vector3.Angle(facingDirection.vec3, aimDirection) > 90f)
						{
							targetingDirection = facingDirection.vec3;
						}
					}
				}
				else
				{
					Vector2 inputAxisValue = inputModule.GetInputAxisValue(PlayerInput.InputAxisType.CHARACTER_AIM_HORIZONTAL, PlayerInput.InputAxisType.CHARACTER_AIM_VERTICAL);
					vector = ProcessMovementInput(inputAxisValue);
					Direction direction3 = Direction.FromVector(new Vector3(vector.x, 0f, vector.y), 0.01f);
					if (targetMovementVelocity.sqrMagnitude > 0.1f)
					{
						targetingDirection = targetMovementVelocity.normalized;
					}
					if (!direction3.is0)
					{
						facingDirection = direction3;
						if (inputAxisValue.sqrMagnitude > 0.1f)
						{
							targetingDirection = new Vector3(inputAxisValue.x, 0f, inputAxisValue.y);
						}
					}
					else if (!direction.is0)
					{
						facingDirection = direction;
					}
				}
			}
			if (!direction.is0)
			{
				lastEffectiveSteerDir = direction;
			}
		}
		else
		{
			targetMovementVelocity = new Vector3(0f, 0f, 0f);
		}
		if (!flag)
		{
			UpdateInventoryStuff();
		}
		UpdateOnTileEffects();
		UpdateRunDust();
		UpdateGlowTulipShaderArray();
		Manager.achievements.CheckAndTriggerAchievements(this);
	}

	private void UpdateInventoryStuff()
	{
		if (!isInteractionBlocked)
		{
			HandleSlotSwapping();
			mouseUIInputWasDone = Manager.ui.mouse.UpdateMouseUIInput(out var _, out var _);
			if (!mouseUIInputWasDone && !isInteractionBlocked && !_anyInventoryOrMapWasActiveThisFrame)
			{
				PlayerStateCD componentData = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world);
				if (componentData.HasAnyState(PlayerStateEnum.MinecartRiding | PlayerStateEnum.BoatRiding | PlayerStateEnum.VehicleRiding) && inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.INTERACT_WITH_OBJECT))
				{
					MinecartRidingStateCD componentData2 = EntityUtility.GetComponentData<MinecartRidingStateCD>(base.entity, base.world);
					EffectiveVelocityCD componentData3 = EntityUtility.GetComponentData<EffectiveVelocityCD>(base.entity, base.world);
					if ((componentData.HasAnyState(PlayerStateEnum.MinecartRiding) && componentData2.IsMoving) || (componentData.HasAnyState(PlayerStateEnum.BoatRiding) && componentData3.IsBarelyMoving) || (componentData.HasAnyState(PlayerStateEnum.VehicleRiding) && componentData3.IsMoving))
					{
						Emote.SpawnEmoteText(center, Emote.EmoteType.CantLeaveWhileItsMoving);
					}
				}
			}
		}
		if (inputModule.PrefersKeyboardAndMouse() && inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.INTERACT_WITH_OBJECT) && ((activeCraftingHandler != null && activeCraftingHandler != playerCraftingHandler) || (activeInventoryHandler != null && activeInventoryHandler != playerInventoryHandler)))
		{
			CloseAnyOpenInventory();
		}
		if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.QUICK_STACK) && CanUseQuickStackShortcut())
		{
			if (activeInventoryHandler != null && activeInventoryHandler != playerInventoryHandler && activeInventoryHandler != sellSlotsHandler.sellSlotsInventoryHandler)
			{
				QueueInputAction(new UIInputActionData
				{
					action = UIInputAction.InventoryChange,
					inventoryChangeData = Create.QuickStack(playerInventoryHandler.inventoryEntity, activeInventoryHandler.inventoryEntity)
				});
			}
			else
			{
				QueueInputAction(new UIInputActionData
				{
					action = UIInputAction.InventoryChange,
					inventoryChangeData = Create.QuickStackToNearbyChests(playerInventoryHandler.inventoryEntity)
				});
			}
		}
		if (Manager.ui.isPlayerInventoryShowing)
		{
			if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.SORT))
			{
				QueueInputAction(new UIInputActionData
				{
					action = UIInputAction.InventoryChange,
					inventoryChangeData = Create.Sort(playerInventoryHandler.inventoryEntity, isPlayerInventory: true)
				});
			}
			if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.LOCKING_TOGGLE))
			{
				Manager.ui.mouse.ToggleLockingMouseMode();
			}
		}
		if (!Manager.menu.IsAnyMenuActive())
		{
			if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.EQUIP_PRESET_1))
			{
				SetActiveEquipmentPreset(0);
			}
			else if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.EQUIP_PRESET_2))
			{
				SetActiveEquipmentPreset(1);
			}
			else if (inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.EQUIP_PRESET_3))
			{
				SetActiveEquipmentPreset(2);
			}
		}
	}

	private void HandleSlotSwapping()
	{
		if (inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.QUICK_SWAP_TORCH))
		{
			return;
		}
		HandleHotBarSlotNavigation(out var swappedItem);
		if (!swappedItem)
		{
			for (int i = 0; i < 10; i++)
			{
				if (inputModule.WasSlotButtonPressedDownThisFrame(i))
				{
					swappedItem = true;
					EquipSlot(i + hotbarStartIndex);
					break;
				}
			}
		}
		if (swappedItem)
		{
			ClientInputHistoryCD componentData = EntityUtility.GetComponentData<ClientInputHistoryCD>(base.entity, base.world);
			componentData.secondInteractBlockedUntilRelease = true;
			clientInput.SetButtonState(CommandInputButtonStateNames.SecondInteract_HeldDown, val: false);
			EntityUtility.SetComponentData(base.entity, base.world, componentData);
			AudioManager.Sfx(SfxTableID.playerSwapItem, base.transform.position);
		}
	}

	private void HandleHotBarSlotNavigation(out bool swappedItem)
	{
		bool prefersMouse = inputModule.PrefersKeyboardAndMouse();
		swappedItem = false;
		if (equipmentSlots.Count <= 1 || (prefersMouse && slotSwapWithMouseIsOnCooldown))
		{
			return;
		}
		if (InputIsTriggered(PlayerInput.InputType.SWAP_NEXT_HOTBAR))
		{
			UpdateSelectedHotbar(moveToNext: true, prefersMouse, out swappedItem);
			if (swappedItem)
			{
				return;
			}
		}
		if (InputIsTriggered(PlayerInput.InputType.SWAP_PREVIOUS_HOTBAR))
		{
			UpdateSelectedHotbar(moveToNext: false, prefersMouse, out swappedItem);
			if (swappedItem)
			{
				return;
			}
		}
		if (InputIsTriggered(PlayerInput.InputType.NEXT_SLOT))
		{
			UpdateSelectedItem(moveToNext: true, prefersMouse, out swappedItem);
			if (swappedItem)
			{
				return;
			}
		}
		if (InputIsTriggered(PlayerInput.InputType.PREVIOUS_SLOT))
		{
			UpdateSelectedItem(moveToNext: false, prefersMouse, out swappedItem);
			_ = swappedItem;
		}
		bool InputIsTriggered(PlayerInput.InputType input)
		{
			bool flag = inputModule.rewiredPlayer.GetAxisCoordinateMode((int)input) == AxisCoordinateMode.Absolute;
			if (prefersMouse && !flag && inputModule.rewiredPlayer.GetAxis((int)input) > 0f)
			{
				return true;
			}
			return inputModule.WasButtonPressedDownThisFrame(input);
		}
	}

	private bool CanUseQuickStackShortcut()
	{
		EntityUtility.TryGetComponentData<PlayerStateCD>(base.entity, base.world, out var value);
		return value.HasNoneState(PlayerStateEnum.PlayingInstrument);
	}

	private void UpdateSelectedHotbar(bool moveToNext, bool prefersMouse, out bool swappedItem)
	{
		bool num = inputModule.rewiredPlayer.controllers.maps.GetFirstElementMapWithAction((!prefersMouse) ? ControllerType.Joystick : ControllerType.Keyboard, 225, skipDisabledMaps: true) == null || inputModule.IsButtonCurrentlyDown(PlayerInput.InputType.HOT_BAR_SWAP_MODIFIER);
		swappedItem = false;
		if (num)
		{
			if (moveToNext)
			{
				Manager.ui.itemSlotsBar.MoveToNextHotBar();
			}
			else
			{
				Manager.ui.itemSlotsBar.MoveToPreviousHotBar();
			}
			swappedItem = true;
			slotSwapWithMouseCooldownTimer.Start();
		}
	}

	private void UpdateSelectedItem(bool moveToNext, bool prefersMouse, out bool swappedItem)
	{
		swappedItem = false;
		bool flag = Manager.ui.currentSelectedUIElement == Manager.ui.itemSlotsBar.downArrow;
		bool flag2 = Manager.ui.currentSelectedUIElement == Manager.ui.itemSlotsBar.upArrow;
		bool flag3 = true;
		if (!prefersMouse && Manager.prefs.ShowHotbarArrows)
		{
			int num = hotbarEndIndex - hotbarStartIndex - 1;
			int num2 = (moveToNext ? num : 0);
			if (!flag && !flag2 && equippedVisualSlotIndex == num2)
			{
				if (moveToNext)
				{
					Manager.ui.itemSlotsBar.downArrow.Select();
				}
				else
				{
					Manager.ui.itemSlotsBar.upArrow.Select();
				}
				Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
				flag3 = false;
				swappedItem = true;
			}
			else if (flag2)
			{
				if (moveToNext)
				{
					Manager.ui.DeselectAnySelectedUIElement();
					EquipSlot(hotbarStartIndex);
				}
				else
				{
					Manager.ui.itemSlotsBar.downArrow.Select();
					Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
				}
				flag3 = false;
				swappedItem = true;
			}
			else if (flag)
			{
				if (moveToNext)
				{
					Manager.ui.itemSlotsBar.upArrow.Select();
					Manager.ui.mouse.PlaceMousePositionOnSelectedUIElementWhenControlledByJoystick();
				}
				else
				{
					Manager.ui.DeselectAnySelectedUIElement();
					EquipSlot(hotbarEndIndex - 1);
				}
				flag3 = false;
				swappedItem = true;
			}
		}
		if (!flag3)
		{
			return;
		}
		slotSwapWithMouseCooldownTimer.Start();
		int num3;
		if (moveToNext)
		{
			num3 = equippedSlotIndex + 1;
			if (num3 >= hotbarEndIndex)
			{
				num3 = hotbarStartIndex;
			}
		}
		else
		{
			num3 = equippedSlotIndex - 1;
			if (num3 < hotbarStartIndex)
			{
				num3 = hotbarEndIndex - 1;
			}
		}
		EquipSlot(num3);
		swappedItem = true;
	}

	public static float3 GetBeamStartPoint(in AnimationOrientationCD animationOrientationCD, in PlayerStateCD playerStateCD, in LocalTransform localTransform)
	{
		Direction direction = animationOrientationCD.facingDirection;
		float3 @float = float3.zero;
		if (direction.id == Direction.Id.forward)
		{
			@float = new float3(0.3f, 0.25f, 0f);
		}
		else if (direction.id == Direction.Id.back)
		{
			@float = new float3(-0.3f, 0.25f, -0.45f);
		}
		else if (direction.id == Direction.Id.left)
		{
			@float = new float3(-0.25f, 0.25f, -0.45f);
		}
		else if (direction.id == Direction.Id.right)
		{
			@float = new float3(0.25f, 0.25f, -0.45f);
		}
		if (playerStateCD.HasAnyState(PlayerStateEnum.BoatRiding))
		{
			@float += new float3(0f, 0f, -0.2f);
		}
		return localTransform.Position + direction.f3 * 0.2f + @float;
	}

	private static bool GetBeamPoints(in PlayerAimPositionCD playerAimPositionCD, in AnimationOrientationCD animationOrientationCD, in PlayerStateCD playerStateCD, in LocalTransform localTransform, out float3 fromWorldPos, out float3 toWorldPos)
	{
		float3 beamStartPoint = GetBeamStartPoint(in animationOrientationCD, in playerStateCD, in localTransform);
		fromWorldPos = beamStartPoint.X0Z() + new float3(0f, 0.25f, 0f);
		toWorldPos = playerAimPositionCD.position.X0Z() + new float3(0f, 0.25f, 0f);
		return playerAimPositionCD.isHittingSomething;
	}

	private static void GetChainPoints(in DynamicBuffer<PlayerChainTargetsBuffer> playerChainTargetsBuffer, int index, out float3 fromWorldPos, out float3 toWorldPos)
	{
		fromWorldPos = playerChainTargetsBuffer[index - 1].targetPosition.X0Z() + new float3(0f, 0.25f, 0f);
		toWorldPos = playerChainTargetsBuffer[index].targetPosition.X0Z() + new float3(0f, 0.25f, 0f);
	}

	public bool RotateInteractionIsConflicting()
	{
		if (EntityUtility.GetComponentData<InteractorCD>(base.entity, base.world).currentClosestInteractable != Entity.Null)
		{
			return inputModule.ActionsAreUsingTheSameInput(PlayerInput.InputType.ROTATE, PlayerInput.InputType.INTERACT_WITH_OBJECT);
		}
		return false;
	}

	public override void UpdatePosition(bool hasLocalToWorld, in LocalToWorld localToWorld)
	{
		using (new ProfilerMarker("PlayerUpdatePosition").Auto())
		{
			if (!isLocal)
			{
				base.UpdatePosition(hasLocalToWorld, in localToWorld);
				return;
			}
			updateGraphicalObjectTransformSystem.SetTransformOverride(base.transform, base.entity);
			base.WorldPosition = EntityUtility.GetComponentData<LocalToWorld>(base.entity, base.world).Position;
		}
	}

	protected void UpdateOnTileEffects()
	{
		bool flag = onAcid;
		CurrentTileCD componentData = EntityUtility.GetComponentData<CurrentTileCD>(base.entity, base.world);
		onSlime = componentData.TileType == TileType.groundSlime;
		onOrangeSlime = componentData.TileType == TileType.groundSlime && componentData.Tileset == Tileset.Dirt;
		onAcid = componentData.TileType == TileType.groundSlime && componentData.Tileset == Tileset.LarvaHive;
		onPoisonSlime = componentData.TileType == TileType.groundSlime && componentData.Tileset == Tileset.Nature;
		onSlipperySlime = componentData.TileType == TileType.groundSlime && componentData.Tileset == Tileset.Sea;
		onWood = componentData.TileType == TileType.floor && componentData.Tileset == Tileset.BaseBuildingWood;
		onWood = onWood || (componentData.TileType == TileType.bridge && componentData.Tileset == Tileset.BaseBuildingWood);
		onGrass = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Turf;
		onGrass = onGrass || (componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Nature);
		onChrysalis = componentData.TileType == TileType.chrysalis && componentData.Tileset == Tileset.Dirt;
		int num;
		if (componentData.TileType == TileType.chrysalis)
		{
			Tileset tileset = componentData.Tileset;
			num = ((tileset == Tileset.Stone || tileset == Tileset.Nature || tileset == Tileset.City || tileset == Tileset.Desert) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		onMoss = (byte)num != 0;
		onStone = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Stone;
		onStone = onStone || (componentData.TileType == TileType.floor && componentData.Tileset == Tileset.BaseBuildingStone);
		onStone = onStone || (componentData.TileType == TileType.bridge && componentData.Tileset == Tileset.BaseBuildingStone);
		onFlesh = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.LarvaHive;
		int num2;
		if (componentData.TileType == TileType.ground)
		{
			Tileset tileset = componentData.Tileset;
			num2 = ((tileset == Tileset.Sand || tileset == Tileset.Desert) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		onSand = (byte)num2 != 0;
		onOasis = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Oasis;
		onBeach = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Sea;
		onClay = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Clay;
		onMold = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Mold;
		onDirt = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Dirt;
		onMetal = componentData.TileType == TileType.bridge && componentData.Tileset == Tileset.BaseBuildingMetal;
		onRoses = componentData.TileType == TileType.looseFlooring && componentData.Tileset == Tileset.BaseBuildingValentine;
		TileType tileType = componentData.TileType;
		onGlass = (tileType == TileType.floor || tileType == TileType.bridge) && (componentData.Tileset == Tileset.Glass || (componentData.Tileset >= Tileset.GlassYellow && componentData.Tileset <= Tileset.GlassGrey));
		onMeadow = componentData.TileType == TileType.ground && componentData.Tileset == Tileset.Meadow;
		if (onAcid && !flag)
		{
			acidLoop = AudioManager.SfxFollowTransform(SfxID.AcidLoop, base.transform, 1f, 1f, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: true);
		}
		else if ((bool)acidLoop && !onAcid)
		{
			acidLoop.FadeOutAndStop(0.5f);
			acidLoop = null;
		}
	}

	protected void UpdateRunDust()
	{
		if (EntityUtility.HasComponentData<ControlledByOtherEntityCD>(base.entity, base.world) && !(EntityUtility.GetComponentData<ControlledByOtherEntityCD>(base.entity, base.world).controlledByEntity == Entity.Null))
		{
			float t = (EntityUtility.GetComponentData<AnimationSpeedCD>(base.entity, base.world).speed - 1f) / 1.5f;
			float num = Mathf.Lerp(0f, 10f, t);
			if (onSlime || onAcid)
			{
				num = 0f;
			}
			ParticleSystem.EmissionModule emission = runDust.emission;
			emission.rateOverTime = num;
			AnimationOrientationCD componentData = EntityUtility.GetComponentData<AnimationOrientationCD>(base.entity, base.world);
			if (!componentData.facingDirection.is0)
			{
				runDust.transform.rotation = Quaternion.LookRotation(componentData.facingDirection.vec3, Vector3.up);
			}
		}
	}

	protected void UpdateGreatWall()
	{
		SinglePugMap.TileLayerLookup tileLayerLookup = Manager.multiMap.GetTileLayerLookup();
		TheGreatWallAnimationSystem.TheGreatWallanimationBuffer value;
		bool flag = querySystem.TryGetSingleton<TheGreatWallAnimationSystem.TheGreatWallanimationBuffer>(out value);
		if (flag)
		{
			flag = value.animationTimer <= 10f;
		}
		if (greatWallIsShakingAndRumbling && !flag)
		{
			greatWallIsShakingAndRumbling = false;
			greatWallHasBeenLowered = true;
			greatWallShakeTimer.Stop();
		}
		float value2 = (greatWallHasBeenLowered ? (-4) : 0);
		float start = 0f;
		float value3 = 0f;
		if (flag)
		{
			if (!greatWallIsShakingAndRumbling && value.animationTimer > 2f)
			{
				greatWallShakeTimer.Start();
				Manager.camera.ShakeCameraNow(0.2f);
				greatWallIsShakingAndRumbling = true;
			}
			if (greatWallShakeTimer.isRunning && greatWallShakeTimer.isTimerElapsed)
			{
				float num = math.clamp(10f - value.animationTimer, 0f, 1f) * 1.5f;
				Manager.camera.ShakeCameraNow(0.2f, num, num);
				greatWallShakeTimer.Start();
			}
			if (!greatWallEmissiveSoundHasPlayed)
			{
				AudioManager.SfxFollowTransform(SfxID.lowering_the_Ancient_Wall, base.transform, 1f, 1f, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: false, 16f, 10f, muteVolumeWhilePaused: true, freeAudioSourceAfterItStoppedPlaying: true, playOnGamepad: true);
				greatWallEmissiveSoundHasPlayed = true;
			}
			float time = value.animationTimer / 10f;
			value2 = greatWallHeightCurve.Evaluate(time);
			start = greatWallEmissiveCurve.Evaluate(time);
			value3 = greatWallParticlesCurve.Evaluate(time);
		}
		bool hasUnlockedSouls = EntityUtility.GetComponentData<SoulsInfoCD>(base.entity, base.world).hasUnlockedSouls;
		bool flag2 = false;
		bool flag3 = false;
		if (querySystem.TryGetSingleton<WorldInfoCD>(out var value4))
		{
			flag2 = value4.greatWallHasBeenLowered;
			flag3 = value4.coreIsActivated;
		}
		float num2 = 4f;
		playerCanInteractWithGreatWall = false;
		if (!flag && hasUnlockedSouls && !flag2 && flag3)
		{
			int2 @int = base.WorldPosition.RoundToInt2();
			for (int i = -4; i <= 4; i++)
			{
				for (int j = -4; j <= 4; j++)
				{
					int2 worldPosition = @int + new int2(i, j);
					float num3 = math.length(base.WorldPosition - new Vector3(worldPosition.x, 0f, worldPosition.y));
					if (num3 < num2 && tileLayerLookup.GetTopTile(worldPosition).tileType == TileType.greatWall)
					{
						num2 = num3;
						if (num2 <= 1.5f)
						{
							playerCanInteractWithGreatWall = true;
						}
					}
				}
			}
			if (!playerNearbyTimer.isRunning && num2 < 4f)
			{
				playerNearbyTimer.Start(2.5f);
				canDoHandEmote = true;
			}
			else if (num2 >= 4f)
			{
				playerNearbyTimer.Stop();
			}
			if (playerNearbyTimer.isTimerElapsed && canDoHandEmote)
			{
				canDoHandEmote = false;
				if (num2 < 3f)
				{
					Emote.SpawnEmoteText(center, Emote.EmoteType.PlaceHandOnWall);
				}
			}
			if (!isInteractionBlocked && playerCanInteractWithGreatWall && inputModule.WasButtonPressedDownThisFrame(PlayerInput.InputType.INTERACT_WITH_OBJECT))
			{
				playerCommandSystem.LowerTheGreatWall(base.entity);
			}
		}
		else
		{
			playerNearbyTimer.Stop();
		}
		if (playerCanInteractWithGreatWall && !isPlayingInteractFlash)
		{
			isPlayingInteractFlash = true;
			Manager.ui.interactHintButton.ShowLightUpHint();
		}
		else if (!playerCanInteractWithGreatWall)
		{
			isPlayingInteractFlash = false;
			Manager.ui.interactHintButton.HideLightUpHint();
		}
		if (!flag)
		{
			nearbyEmissiveAndLowerWallBlend = math.clamp(nearbyEmissiveAndLowerWallBlend + Time.deltaTime, 0f, 1f);
		}
		else
		{
			nearbyEmissiveAndLowerWallBlend = math.clamp(nearbyEmissiveAndLowerWallBlend - Time.deltaTime, 0f, 1f);
		}
		float end = math.pow(math.clamp((4f - num2) / 4f, 0f, 1f), 1.8f) * 0.4f;
		start = math.lerp(start, end, nearbyEmissiveAndLowerWallBlend);
		ParticleSystem.EmissionModule emission = coreWallParticles.emission;
		float t = math.clamp((4f - num2) / 4f, 0f, 1f);
		emission.rateOverTime = math.lerp(0f, 15f, t);
		Shader.SetGlobalFloat(GreatWallHeight, value2);
		Shader.SetGlobalFloat(GreatWallEmissivity, start);
		Shader.SetGlobalFloat(GreatWallParticles, value3);
	}

	protected void UpdateAnimationSounds(int animationID)
	{
		if (animationID == -1518581387 && castAudio == null)
		{
			castAudio = AudioManager.Sfx(SfxID.teleportLoop, base.transform.position, 0.5f, 1f, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: true);
		}
		else if (animationID != -1518581387 && castAudio != null)
		{
			castAudio.FadeOutAndStop(0.25f);
			castAudio = null;
		}
	}

	public void StartFishingMiniGameSounds()
	{
		if (reelAudio == null)
		{
			reelAudio = AudioManager.Sfx(SfxID.fishingReel, base.transform.position, 0f, 1f, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: true);
		}
		if (struggleAudio == null)
		{
			struggleAudio = AudioManager.Sfx(SfxID.waterSplashLoop, base.transform.position, 0f, 1.2f, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: true);
		}
	}

	public void StopFishingMiniGameSounds()
	{
		if (reelAudio != null)
		{
			reelAudio.FadeOutAndStop();
			reelAudio = null;
		}
		if (struggleAudio != null)
		{
			struggleAudio.FadeOutAndStop();
			struggleAudio = null;
		}
	}

	public void UpdateFishingMiniGameSounds(in FishingMiniGameStateCD fishingMiniGameStateCD)
	{
		if (struggleAudio == null || reelAudio == null)
		{
			return;
		}
		if (fishingMiniGameStateCD.isInFishingMiniGame && !fishingMiniGameStateCD.miniGameOverTimer.isRunning)
		{
			reelAudio.SetVolume(fishingMiniGameStateCD.reelVolume);
			if (fishingMiniGameStateCD.fishIsStruggling)
			{
				struggleAudio.SetVolume(0.5f);
			}
			else
			{
				struggleAudio.SetVolume(Mathf.Clamp(struggleAudio.audioSource.volume - Time.deltaTime / fishingMiniGameStateCD.struggleAudioFadeOutTime, 0f, 1f));
			}
		}
		else
		{
			reelAudio.SetVolume(0f);
			struggleAudio.SetVolume(0f);
		}
	}

	public void UpdateFishingRodLine(FishingStateCD fishingStateCD)
	{
		if (!fishingRodLine.gameObject.activeInHierarchy)
		{
			return;
		}
		Vector3 vector = EntityMonoBehaviour.ToRenderFromWorld(fishingStateCD.targetSinkWorldPosition);
		fishingRodLine.colorGradient = fishingRodLineDefaultGradient;
		for (int i = 0; i < fishingRodLineGradientOverrides.Count; i++)
		{
			if (fishingRodLineGradientOverrides[i].fishingRodId == visuallyEquippedContainedObject.objectID)
			{
				fishingRodLine.colorGradient = fishingRodLineGradientOverrides[i].gradient;
				break;
			}
		}
		Vector3 position = fishingRodLine.transform.position;
		float3 @float = new float3(math.lerp(position.x, vector.x, animatedFishingRodSinkPosition.x), math.lerp(position.y, vector.y, animatedFishingRodSinkPosition.y), math.lerp(position.z, vector.z, animatedFishingRodSinkPosition.x));
		float num = math.sin(Time.time * (fishingStateCD.fishIsNibbling ? 5f : 2f));
		@float += new float3(0f, num * 0.05f, 0f);
		EntityUtility.TryGetComponentData<FishingMiniGameStateCD>(base.entity, base.world, out var value);
		if ((fishingStateCD.fishIsNibbling && animatedFishingRodSinkPosition.y >= 1f) || value.fishIsStruggling)
		{
			if (!rippleTimer.isRunning || rippleTimer.isTimerElapsed)
			{
				Manager.effects.PlayPuff(PuffID.WaterRipple, @float, 1);
				sinkHeightFromFishHooking = -0.2f;
				if (value.fishIsStruggling)
				{
					AudioManager.Sfx(SfxID.splash2, @float, 0.5f, 1f, 0.1f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: false, 16f, 10f, muteVolumeWhilePaused: true, freeAudioSourceAfterItStoppedPlaying: true, playOnGamepad: true);
					Manager.effects.PlayTempSprite(SpriteTempEffectID.WaterSplash, @float, 1f, 0.5f);
					rippleTimer.Start(1f);
				}
				else
				{
					AudioManager.Sfx(SfxID.bubble, @float, 1f, 1f, 0.2f);
					rippleTimer.Start(UnityEngine.Random.Range(0.5f, 1f));
				}
			}
		}
		else
		{
			rippleTimer.Stop();
		}
		sinkHeightFromFishHooking = math.clamp(sinkHeightFromFishHooking + Time.deltaTime, -0.2f, 0f);
		@float.y += sinkHeightFromFishHooking;
		fishingRodSink.transform.position = @float;
		@float += new float3(0f, 0.1f, 0f);
		for (int j = 0; j < fishingRodLine.positionCount; j++)
		{
			float num2 = (float)j / (float)(fishingRodLine.positionCount - 1);
			float3 float2 = Vector3.Lerp(position, @float, num2);
			float end = slackingFishingLineCurve.Evaluate(num2);
			float t = math.lerp(throwingFishingLineCurve.Evaluate(num2), end, lineTensionSmoothTimer);
			float2.y = math.lerp(position.y, @float.y, t);
			fishingRodLine.SetPosition(j, float2);
		}
		float3 x = new float3(vector.x, 0f, vector.z) - new float3(position.x, 0f, position.z);
		float widthMultiplier = math.lerp(0.048f, 0.0625f, math.abs(math.normalizesafe(x, float3.zero).z) + math.lerp(1f, 0f, math.clamp(math.length(x), 0f, 1f)));
		fishingRodLine.widthMultiplier = widthMultiplier;
		if (animatedLineIsBeingPulled)
		{
			lineTensionSmoothTimer = 0.5f;
		}
		else if (animatedFishingRodSinkPosition.y < 1f)
		{
			lineTensionSmoothTimer = 0f;
		}
		else
		{
			lineTensionSmoothTimer = Mathf.Clamp01(lineTensionSmoothTimer + Time.deltaTime);
		}
		if (fishingStateCD.fishingLootToSpawn != 0 && animatedFishingRodSinkPosition.y < 1f)
		{
			fishingRodLoot.sprite = PugDatabase.GetObjectInfo(fishingStateCD.fishingLootToSpawn)?.smallIcon;
		}
		else
		{
			fishingRodLoot.sprite = null;
		}
	}

	private void UpdateDrillAndBeamToolVisuals()
	{
		ObjectInfo objectInfo = PugDatabase.GetObjectInfo(visuallyEquippedContainedObject.objectID);
		EntityUtility.IsComponentEnabled<EntityDestroyedCD>(base.entity, base.world);
		ClientInput componentData = EntityUtility.GetComponentData<ClientInput>(base.entity, base.world);
		ClientInputNonPartialStateCD componentData2 = EntityUtility.GetComponentData<ClientInputNonPartialStateCD>(base.entity, base.world);
		componentData.SetButtonState(CommandInputButtonStateNames.Interact_HeldDown, componentData2.interactHeldDown);
		PlayerStateCD playerStateCD = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world);
		EquippedObjectCD equippedObjectCD = EntityUtility.GetComponentData<EquippedObjectCD>(base.entity, base.world);
		EquipmentSlotCD equipmentSlotCD = EntityUtility.GetComponentData<EquipmentSlotCD>(base.entity, base.world);
		PlayerGhost playerGhost = EntityUtility.GetComponentData<PlayerGhost>(base.entity, base.world);
		querySystem.TryGetSingleton<WorldInfoCD>(out var value);
		bool flag = playerStateCD.HasAnyState(PlayerStateEnum.SpawningFromCore | PlayerStateEnum.Death | PlayerStateEnum.VehicleRiding | PlayerStateEnum.Sitting);
		bool interactHeldDown = componentData2.interactHeldDown;
		bool flag2 = objectInfo != null && objectInfo.objectType == ObjectType.DrillTool;
		bool flag3 = flag2 && visuallyEquippedContainedObject.amount > 0 && interactHeldDown && !flag;
		UpdateDrillTool(flag3, flag2);
		ObjectType num = objectInfo?.objectType ?? ObjectType.NonUsable;
		bool beamToolEquipped = num == ObjectType.BeamWeapon;
		bool flag4 = BeamTargetUpdateSystem.BeamTargetUpdateJob.IsBeamActive(num, in equippedObjectCD, interactHeldDown, in playerStateCD, in value, in playerGhost, in equipmentSlotCD, in componentData);
		UpdateBeamWeaponVisuals(flag4, beamToolEquipped);
		animator.SetBool(Drilling, flag3 || flag4);
	}

	public bool IsUsingBeamOrDrillTool()
	{
		ObjectInfo objectInfo = PugDatabase.GetObjectInfo(visuallyEquippedContainedObject.objectID);
		ClientInput componentData = EntityUtility.GetComponentData<ClientInput>(base.entity, base.world);
		if (objectInfo != null && (objectInfo.objectType == ObjectType.DrillTool || objectInfo.objectType == ObjectType.BeamWeapon) && visuallyEquippedContainedObject.amount > 0)
		{
			return componentData.IsButtonStateSet(CommandInputButtonStateNames.Interact_HeldDown);
		}
		return false;
	}

	public bool IsTryingToUseBeamOrDrillTool()
	{
		ClientInput componentData = EntityUtility.GetComponentData<ClientInput>(base.entity, base.world);
		ObjectDataCD heldObject = GetHeldObject();
		ObjectType heldObjectType = GetHeldObjectType();
		if (heldObject.amount > 0 && (heldObjectType == ObjectType.DrillTool || heldObjectType == ObjectType.BeamWeapon) && visuallyEquippedContainedObject.amount > 0)
		{
			return componentData.IsButtonStateSet(CommandInputButtonStateNames.Interact_HeldDown);
		}
		return false;
	}

	public static bool IsTryingToUseBeamOrDrillTool(in ClientInput clientInput, in EquippedObjectCD equippedObjectCD, PugDatabase.DatabaseBankCD databaseBankCD)
	{
		ObjectType objectType = PugDatabase.GetEntityObjectInfo(equippedObjectCD.containedObject.objectID, databaseBankCD.databaseBankBlob, equippedObjectCD.containedObject.variation).objectType;
		if (equippedObjectCD.containedObject.objectData.amount > 0 && (objectType == ObjectType.DrillTool || objectType == ObjectType.BeamWeapon) && equippedObjectCD.containedObject.amount > 0)
		{
			return clientInput.IsButtonStateSet(CommandInputButtonStateNames.Interact_HeldDown);
		}
		return false;
	}

	private void UpdateDrillTool(bool drillIsActive, bool drillToolEquipped)
	{
		if (drillIsActive)
		{
			if (drillIsLooping)
			{
				return;
			}
			ObjectInfo objectInfo = PugDatabase.GetObjectInfo(visuallyEquippedContainedObject.objectID);
			if (!drillStartSoundTimer.isRunning)
			{
				if (objectInfo == null || objectInfo.objectID != ObjectID.DrillToolScarlet)
				{
					AudioManager.SfxFollowTransform(SfxTableID.drillToolStart, base.transform, 1f, 1f, loop: false, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, result: drillSoundAudioSources, playOnGamepad: isLocal);
				}
				else
				{
					AudioManager.SfxFollowTransform(SfxTableID.scarletDrillToolStart, base.transform, 1f, 1f, loop: false, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, result: drillSoundAudioSources, playOnGamepad: isLocal);
				}
				drillStartSoundTimer.Start();
				ApplyGamepadRumble(0.3f, 0.48f, Manager.input.LinearAscendingAnimationCurve);
			}
			else if (drillStartSoundTimer.isRunning && drillStartSoundTimer.isTimerElapsed)
			{
				if (objectInfo.objectID != ObjectID.DrillToolScarlet)
				{
					AudioManager.SfxFollowTransform(SfxTableID.drillToolLoop, base.transform, 1f, 1f, loop: true, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, result: drillSoundAudioSources, playOnGamepad: isLocal);
				}
				else
				{
					AudioManager.SfxFollowTransform(SfxTableID.scarletDrillToolLoop, base.transform, 1f, 1f, loop: true, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, result: drillSoundAudioSources, playOnGamepad: isLocal);
				}
				drillStartSoundTimer.Stop();
				drillIsLooping = true;
				ApplyGamepadRumble(0.3f, float.PositiveInfinity, Manager.input.ConstantFullAnimationCurve, PlayerInput.RumbleInstanceId.DrillTools);
			}
		}
		else
		{
			if (drillSoundAudioSources.Count <= 0)
			{
				return;
			}
			ObjectInfo objectInfo2 = PugDatabase.GetObjectInfo(visuallyEquippedContainedObject.objectID);
			if (objectInfo2 != null && objectInfo2.objectID == ObjectID.DrillToolScarlet)
			{
				AudioManager.SfxFollowTransform(SfxTableID.scarletDrillToolJam, base.transform);
				if (drillToolEquipped)
				{
					AudioManager.SfxFollowTransform(SfxTableID.scarletDrillToolEnd, base.transform);
				}
			}
			else
			{
				AudioManager.SfxFollowTransform(SfxTableID.drillToolJam, base.transform);
				if (drillToolEquipped)
				{
					AudioManager.SfxFollowTransform(SfxTableID.drillToolEnd, base.transform);
				}
			}
			StopDrillSounds();
			StopGamepadRumble();
			ApplyGamepadRumble(0.3f, 0.24f, Manager.input.LinearDescendingAnimationCurve);
		}
	}

	private void ApplyGamepadRumble(float intensity, float duration, AnimationCurve curve = null, PlayerInput.RumbleInstanceId instanceId = PlayerInput.RumbleInstanceId.None)
	{
		if (isLocal)
		{
			Manager.input.singleplayerInputModule.RumbleNow(duration, intensity, curve, instanceId);
		}
	}

	private void StopGamepadRumble()
	{
		Manager.input.singleplayerInputModule.RemoveRumbleInstance(PlayerInput.RumbleInstanceId.DrillTools);
	}

	private void StopDrillSounds()
	{
		drillStartSoundTimer.Stop();
		drillIsLooping = false;
		foreach (AudioManager.RunningSfxReference drillSoundAudioSource in drillSoundAudioSources)
		{
			drillSoundAudioSource.FadeOutAndStop(0f);
		}
		drillSoundAudioSources.Clear();
	}

	private void UpdateBeamWeaponVisuals(bool beamIsActive, bool beamToolEquipped)
	{
		lightningFx.isOn = false;
		beamFx.isOn = false;
		DynamicBuffer<PlayerChainTargetsBuffer> playerChainTargetsBuffer = EntityUtility.GetBuffer<PlayerChainTargetsBuffer>(base.entity, base.world);
		ElectricBeamFX electricBeamFX = ((visuallyEquippedContainedObject.objectID == ObjectID.LightningGun) ? lightningFx : beamFx);
		if (beamIsActive)
		{
			electricBeamFX.isOn = true;
			PlayerAimPositionCD playerAimPositionCD = EntityUtility.GetComponentData<PlayerAimPositionCD>(base.entity, base.world);
			AnimationOrientationCD animationOrientationCD = EntityUtility.GetComponentData<AnimationOrientationCD>(base.entity, base.world);
			PlayerStateCD playerStateCD = EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world);
			LocalTransform localTransform = EntityUtility.GetComponentData<LocalTransform>(base.entity, base.world);
			electricBeamFX.isConnected = playerAimPositionCD.isHittingSomething;
			GetBeamPoints(in playerAimPositionCD, in animationOrientationCD, in playerStateCD, in localTransform, out var fromWorldPos, out var toWorldPos);
			electricBeamFX.originPoint = EntityMonoBehaviour.ToRenderFromWorld(fromWorldPos);
			electricBeamFX.endPoint = EntityMonoBehaviour.ToRenderFromWorld(toWorldPos);
			electricBeamFX.UpdatePosition();
			if (!beamIsLooping)
			{
				if (!beamStartSoundTimer.isRunning)
				{
					AudioManager.SfxFollowTransform(SfxTableID.laserDrillToolStart, base.transform, 1f, 1f, loop: false, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, playOnGamepad: false, beamSoundAudioSources);
					beamStartSoundTimer.Start();
				}
				else if (beamStartSoundTimer.isRunning && beamStartSoundTimer.isTimerElapsed)
				{
					AudioManager.SfxFollowTransform(SfxTableID.laserDrillToolLoop, base.transform, 1f, 1f, loop: true, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, playOnGamepad: true, beamSoundAudioSources);
					beamStartSoundTimer.Stop();
					beamIsLooping = true;
				}
			}
			if (playerAimPositionCD.isHittingSomething && beamSoundHittingAudioSources.Count == 0)
			{
				AudioManager.SfxFollowTransform(SfxTableID.laserDrillToolImpactLoop, base.transform, 1f, 1f, loop: true, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, playOnGamepad: true, beamSoundHittingAudioSources);
			}
			else if (!playerAimPositionCD.isHittingSomething && beamSoundHittingAudioSources.Count != 0)
			{
				StopBeamHittingSounds();
			}
		}
		else
		{
			lightningFx.isOn = false;
			beamFx.isOn = false;
			if (beamSoundAudioSources.Count > 0)
			{
				AudioManager.SfxFollowTransform(SfxTableID.laserDrillToolJam, base.transform);
				if (beamToolEquipped)
				{
					AudioManager.SfxFollowTransform(SfxTableID.laserDrillToolEnd, base.transform);
				}
			}
			StopBeamSounds();
		}
		ElectricBeamFX[] array = chainLightningFx;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].isOn = false;
		}
		if (playerChainTargetsBuffer.Length > 1)
		{
			for (int j = 1; j < playerChainTargetsBuffer.Length; j++)
			{
				chainLightningFx[j].isOn = true;
				chainLightningFx[j].isConnected = true;
				GetChainPoints(in playerChainTargetsBuffer, j, out var fromWorldPos2, out var toWorldPos2);
				chainLightningFx[j].originPoint = EntityMonoBehaviour.ToRenderFromWorld(fromWorldPos2);
				chainLightningFx[j].endPoint = EntityMonoBehaviour.ToRenderFromWorld(toWorldPos2);
				chainLightningFx[j].UpdatePosition();
			}
		}
		else
		{
			array = chainLightningFx;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].isOn = false;
			}
		}
	}

	private void StopBeamSounds()
	{
		beamStartSoundTimer.Stop();
		beamIsLooping = false;
		foreach (AudioManager.RunningSfxReference beamSoundAudioSource in beamSoundAudioSources)
		{
			beamSoundAudioSource.FadeOutAndStop(0f);
		}
		beamSoundAudioSources.Clear();
		StopBeamHittingSounds();
	}

	private void StopBeamHittingSounds()
	{
		foreach (AudioManager.RunningSfxReference beamSoundHittingAudioSource in beamSoundHittingAudioSources)
		{
			beamSoundHittingAudioSource.FadeOutAndStop(0f);
		}
		beamSoundHittingAudioSources.Clear();
	}

	private void UpdateEmotes()
	{
		PlayerEmoteEffectsCD componentData = EntityUtility.GetComponentData<PlayerEmoteEffectsCD>(base.entity, base.world);
		if (componentData.thisIsGoingToTakeAWhileLastDamageTick.IsValid)
		{
			if (!thisIsGoingToTakeAWhileLastDamageTick.IsValid || componentData.thisIsGoingToTakeAWhileLastDamageTick.IsNewerThan(thisIsGoingToTakeAWhileLastDamageTick))
			{
				thisIsGoingToTakeAWhileLastDamageTick = componentData.thisIsGoingToTakeAWhileLastDamageTick;
				if (thisIsGoingToTakeAWhileEmoteTimer.isRunning && thisIsGoingToTakeAWhileEmoteTimer.isTimerElapsed && thisIsGoingToTakeAWhileEmoteTimer.elapsedTime < 17f)
				{
					Emote.SpawnEmoteText(center, ((float)componentData.thisIsGoingToTakeAWhileHitsNeededToDestroy >= 30f) ? Emote.EmoteType.ThisWillTakeForever : Emote.EmoteType.ThisWillTakeAWhile);
					thisIsGoingToTakeAWhileEmoteTimer.Start(15f);
				}
				else if (!thisIsGoingToTakeAWhileEmoteTimer.isRunning || thisIsGoingToTakeAWhileEmoteTimer.elapsedTime > 17f)
				{
					thisIsGoingToTakeAWhileEmoteTimer.Start(2f);
				}
			}
		}
		else
		{
			thisIsGoingToTakeAWhileEmoteTimer.Stop();
		}
	}

	public void UpdateWindupSounds(in EquipmentSlotCD equipmentSlotCD, in EquippedObjectCD equippedObjectCD, ComponentLookup<CustomAttackSoundCD> customAttackSoundLookup)
	{
		if (equipmentSlotCD.windupTimer.isRunning)
		{
			if (!_windupSoundsPlaying)
			{
				int sfxTableID = 0;
				if (customAttackSoundLookup.TryGetComponent(equippedObjectCD.equipmentPrefab, out var componentData) && componentData.windupSoundId != -1)
				{
					sfxTableID = componentData.windupSoundId;
				}
				AudioManager.Sfx(sfxTableID, base.transform.position, 1f, 1f, loop: false, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, playOnGamepad: true, _windupSoundEffects);
				_windupSoundsPlaying = true;
			}
		}
		else
		{
			if (!_windupSoundsPlaying)
			{
				return;
			}
			foreach (AudioManager.RunningSfxReference windupSoundEffect in _windupSoundEffects)
			{
				windupSoundEffect.FadeOutAndStop(0.2f);
			}
			_windupSoundEffects.Clear();
			_windupSoundsPlaying = false;
		}
	}

	private void InitTulipPositionsArray()
	{
		tulipPositionsArray = new Vector4[8];
		for (int i = 0; i < 8; i++)
		{
			tulipPositionsArray[i] = new Vector4(0f, 0f, 0f, 0f);
		}
	}

	private void UpdateGlowTulipShaderArray()
	{
		List<PlayerController> allPlayers = Manager.main.allPlayers;
		NativeList<Vector4> nativeList = new NativeList<Vector4>(Allocator.Temp);
		foreach (PlayerController item in allPlayers)
		{
			Vector3 position = item.transform.position;
			if (item.visuallyEquippedContainedObject.objectID == ObjectID.GlowingTulipFlower && math.distancesq(position, base.transform.position) < 400f)
			{
				Vector4 value = new Vector4(position.x, 0f, position.z, 1f);
				nativeList.Add(in value);
			}
		}
		for (int i = 0; i < tulipPositionsArray.Length; i++)
		{
			if (nativeList.Length > i)
			{
				tulipPositionsArray[i] = nativeList[i];
			}
			else
			{
				tulipPositionsArray[i] = Vector4.zero;
			}
		}
		nativeList.Dispose();
		Shader.SetGlobalVectorArray(TulipPositionsArray, tulipPositionsArray);
	}

	public void UpdateAim()
	{
		float3 @float = aimDirection;
		UpdateAim(ref @float, base.RenderPosition, isAimingBlocked, inputModule, aimUI);
		aimDirection = @float;
	}

	public static void UpdateAim(ref float3 aimDirection, float3 position, bool isAimingBlocked, PlayerInput inputModule, AimUI aimUI)
	{
		if (isAimingBlocked)
		{
			return;
		}
		if (inputModule.PrefersKeyboardAndMouse())
		{
			Vector3 mouseGameViewPosition = Manager.ui.mouse.GetMouseGameViewPosition();
			UnityEngine.Ray ray = Manager.camera.gameCamera.ViewportPointToRay(Manager.camera.gameCamera.WorldToViewportPoint(mouseGameViewPosition));
			new UnityEngine.Plane(Vector3.up, 0f).Raycast(ray, out var enter);
			float3 @float = ray.origin + ray.direction * enter;
			@float -= new float3(0f, 0f, 1f) * 0.5f;
			aimDirection = @float - position;
			aimDirection.y = 0f;
			aimDirection = math.normalizesafe(aimDirection);
		}
		else
		{
			Vector2 inputAxisValue = inputModule.GetInputAxisValue(PlayerInput.InputAxisType.CHARACTER_AIM_HORIZONTAL, PlayerInput.InputAxisType.CHARACTER_AIM_VERTICAL);
			Vector2 vector = ProcessMovementInput(inputAxisValue);
			if (!Direction.FromVector(new Vector3(vector.x, 0f, vector.y), 0.01f).is0)
			{
				aimDirection = math.normalizesafe(new float3(inputAxisValue.x, 0f, inputAxisValue.y));
				aimDirection = new Vector3(aimDirection.x, 0f, aimDirection.z);
			}
			else if (math.all(aimDirection == float3.zero))
			{
				aimDirection = new float3(0f, 0f, -1f);
			}
		}
		aimUI.UpdateAimPosition();
	}

	public void SetPlayerPosition(float3 position)
	{
		EntityUtility.SetComponentData(base.entity, base.world, LocalTransform.FromPosition(position));
		targetMovementVelocity = Vector3.zero;
	}

	public void DebugSetPlayerPosition(float3 position)
	{
		playerCommandSystem.SetPlayerPosition(base.entity, position);
	}

	public static Vector2 ProcessMovementInput(Vector2 movementInput)
	{
		float magnitude = movementInput.magnitude;
		if (magnitude < 0.25f)
		{
			return Vector2.zero;
		}
		float num = Mathf.Repeat(Mathf.Atan2(movementInput.y, movementInput.x), MathF.PI * 2f);
		Vector2[] kAnalogInputSnapTable = Constants.kAnalogInputSnapTable;
		int num2 = Mathf.FloorToInt(num / (MathF.PI * 2f) * (float)kAnalogInputSnapTable.Length);
		Vector2 vector = kAnalogInputSnapTable[num2];
		if (vector == Vector2.zero)
		{
			vector = movementInput.normalized;
		}
		if (magnitude < 0.75f)
		{
			return 0.4f * vector;
		}
		return vector;
	}

	private void UpdateInventoryCache()
	{
		DynamicBuffer<ContainedObjectsBuffer> buffer = EntityUtility.GetBuffer<ContainedObjectsBuffer>(base.entity, base.world);
		inventoryCache.Clear();
		for (int i = 0; i < buffer.Length; i++)
		{
			inventoryCache.Add(buffer[i]);
		}
	}

	private void UpdateInventorySize()
	{
		playerInventoryHandler.UpdateSize(0);
		for (int i = 0; i < 4; i++)
		{
			equipmentHandler.pouchInventorySlotsHandlers[i].UpdateSize(i + 1);
		}
	}

	public void UpdateInventory()
	{
		if (inventoryCache.Count == 0)
		{
			Debug.LogWarning("Inventory not set when trying to update");
			return;
		}
		if (base.world == null)
		{
			Debug.LogWarning("World null when trying to update inventory");
			return;
		}
		using EntityQuery entityQuery = base.world.EntityManager.CreateEntityQuery(typeof(InventoryAuxDataSystemDataCD));
		if (!entityQuery.TryGetSingleton<InventoryAuxDataSystemDataCD>(out var value))
		{
			Debug.LogError("failed to get aux system data on player update inventory");
		}
		Manager.saves.SetInventory(inventoryCache, value, base.world);
		Manager.saves.SetCoinAmount(EntityUtility.GetComponentData<CoinAmountCD>(base.entity, base.world).Value);
	}

	private void UpdateConditions()
	{
		DynamicBuffer<ConditionsBuffer> conditions = EntityUtility.GetConditions(base.entity, base.world);
		if ((!conditionsSaveTimer.isRunning || conditionsSaveTimer.isTimerElapsed) && (lastReceivedConditionsAmount != 0 || conditions.Length != 0))
		{
			NetworkTick serverTick = querySystem.GetSingleton<NetworkTime>().ServerTick;
			uint simulationTickRateForPlatform = (uint)NetworkingManager.GetSimulationTickRateForPlatform();
			Manager.saves.SetConditions(conditions, serverTick, simulationTickRateForPlatform);
			lastReceivedConditionsAmount = conditions.Length;
			conditionsSaveTimer.Start();
		}
		clientInput.collectedAndEnabledSoulsMask = Manager.saves.GetCollectedAndActiveSoulsMask();
	}

	private void UpdateLockedObjects()
	{
		DynamicBuffer<LockedObjectsBuffer> buffer = EntityUtility.GetBuffer<LockedObjectsBuffer>(base.entity, base.world);
		if ((!lockedObjectsSaveTimer.isRunning || lockedObjectsSaveTimer.isTimerElapsed) && (lastReceivedLockedObjectsAmount != 0 || buffer.Length != 0))
		{
			Manager.saves.SetLockedObjects(buffer);
			lastReceivedLockedObjectsAmount = buffer.Length;
			lockedObjectsSaveTimer.Start();
		}
		clientInput.collectedAndEnabledSoulsMask = Manager.saves.GetCollectedAndActiveSoulsMask();
	}

	private void UpdateRenderersEnabled()
	{
		if (EntityUtility.GetConditionValues(base.entity, base.world)[81].value > 0)
		{
			DynamicBuffer<ConditionsBuffer> buffer = EntityUtility.GetBuffer<ConditionsBuffer>(base.entity, base.world);
			float num = ((EntityUtility.GetFirstOccurrenceOfCondition(ConditionID.ImmuneToDamageAfterRespawn, buffer).condition.conditionData.duration != 0f) ? 0.5f : 1f);
			bool flag = (float)Mathf.RoundToInt(Time.time * 60f) % (24f * num) < 16f * num;
			{
				foreach (SpriteRenderer renderer in renderers)
				{
					renderer.enabled = flag;
				}
				return;
			}
		}
		if (EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.NoClip) || renderers[0].enabled)
		{
			return;
		}
		foreach (SpriteRenderer renderer2 in renderers)
		{
			renderer2.enabled = true;
		}
	}

	private void UpdateConditionsVisuals()
	{
		DynamicBuffer<SummarizedConditionsBuffer> conditionValues = EntityUtility.GetConditionValues(base.entity, base.world);
		Color value = ((conditionValues[189].value > 0) ? redGlowEyeEmissive : defaultEyeEmissive);
		eyesSpriteRenderer.material.SetColor(Emissive, value);
		bool flag = conditionValues[190].value > 0;
		if (flag != prevLookLikeGhost)
		{
			MakePlayerLookLikeGhost(flag);
		}
	}

	public int GetMaxMana()
	{
		return EntityUtility.GetComponentData<ManaCD>(base.entity, base.world).maxMana + EntityUtility.GetConditionEffectValue(ConditionEffect.MaxMana, base.entity, base.world);
	}

	public int GetMana()
	{
		return EntityUtility.GetComponentData<ManaCD>(base.entity, base.world).mana;
	}

	private void MakePlayerLookLikeGhost(bool value)
	{
		foreach (SpriteRenderer renderer in renderers)
		{
			if (renderer != eyesSpriteRenderer)
			{
				if (value)
				{
					renderer.material = ghostMaterial;
					renderer.color = ghostColor;
				}
				else
				{
					renderer.material = defaultmaterial;
					renderer.color = Color.white;
				}
			}
		}
		skinRenderer.SetAlpha(value ? 0.5f : 1f);
		prevLookLikeGhost = value;
		RefreshCustomization();
	}

	private void UpdateHitArea()
	{
		EquipmentSlot equippedSlot = GetEquippedSlot();
		if (equippedSlot == null || equippedSlot.slotOwner == null)
		{
			return;
		}
		EquippedObjectCD componentData = EntityUtility.GetComponentData<EquippedObjectCD>(base.entity, base.world);
		EquipmentSlotCD equipmentSlotCD = EntityUtility.GetComponentData<EquipmentSlotCD>(base.entity, base.world);
		AnimationOrientationCD animationOrientationCD = EntityUtility.GetComponentData<AnimationOrientationCD>(base.entity, base.world);
		ComponentLookup<MeleeWeaponCD> meleeWeaponLookup = querySystem.GetComponentLookup<MeleeWeaponCD>(isReadOnly: true);
		bool isBroken = componentData.isBroken;
		float currentWindupMultiplier = equipmentSlotCD.currentWindupMultiplier;
		float3 centerOfHitCollider = EquipmentSlot.GetCenterOfHitCollider(componentData.equipmentPrefab, in equipmentSlotCD, in animationOrientationCD, in meleeWeaponLookup, isBroken, currentWindupMultiplier);
		float3 sizeOfHitCollider = EquipmentSlot.GetSizeOfHitCollider(componentData.equipmentPrefab, in equipmentSlotCD, in animationOrientationCD, in meleeWeaponLookup, isBroken, currentWindupMultiplier);
		hitAreaBoxSR.enabled = showHitArea && equippedSlot != null && equippedSlot.GetSlotType() == EquipmentSlotType.MeleeWeaponSlot;
		hitAreaSR.enabled = showHitArea && equippedSlot != null && equippedSlot.GetSlotType() == EquipmentSlotType.MeleeWeaponSlot;
		if (hitAreaBoxSR.enabled)
		{
			float y = hitAreaBoxSR.transform.localPosition.y;
			hitAreaBoxSR.transform.localPosition = new Vector3(centerOfHitCollider.x, y, centerOfHitCollider.z);
			hitAreaBoxSR.size = new Vector2(sizeOfHitCollider.x, sizeOfHitCollider.z);
		}
		if (!hitAreaSR.enabled || !PugDatabase.HasComponent<MeleeWeaponCD>(GetEquippedSlot().objectData))
		{
			return;
		}
		MeleeWeaponCD component = PugDatabase.GetComponent<MeleeWeaponCD>(GetEquippedSlot().objectData);
		float y2 = hitAreaBoxSR.transform.localPosition.y;
		Vector3 localPosition = ((component.colliderCenteredOnWindup && currentWindupMultiplier > 1f) ? new Vector3(0f, y2, 0f) : new Vector3(centerOfHitCollider.x, y2, centerOfHitCollider.z));
		float y3 = 0f;
		Vector3 vec = facingDirection.vec3;
		float angle = facingDirection.angle;
		float num = 0.25f;
		Vector3 vector = new Vector3(sizeOfHitCollider.x, sizeOfHitCollider.z, 1f) * num;
		switch (component.attackFXType)
		{
		case AttackFXType.Arc:
			switch (component.arcAngle)
			{
			case ArcAngle.arc45:
				hitAreaSR.sprite = hitAreaSpriteCone45;
				y3 = -45f;
				break;
			case ArcAngle.arc90:
				hitAreaSR.sprite = hitAreaSpriteCone90;
				y3 = -45f;
				break;
			case ArcAngle.arc135:
				hitAreaSR.sprite = hitAreaSpriteCone135;
				break;
			case ArcAngle.arc180:
				hitAreaSR.sprite = hitAreaSpriteCone180;
				break;
			case ArcAngle.arc270:
				hitAreaSR.sprite = hitAreaSpriteCone270;
				y3 = -45f;
				break;
			case ArcAngle.arc360:
				hitAreaSR.sprite = hitAreaSpriteCircle;
				break;
			default:
				hitAreaSR.sprite = hitAreaSpriteCone90;
				break;
			}
			num = 0.5f;
			vector = ((angle / 90f % 2f != 0f) ? (new Vector3(sizeOfHitCollider.x * 0.5f, sizeOfHitCollider.z, 1f) * num) : (new Vector3(sizeOfHitCollider.z * 0.5f, sizeOfHitCollider.x, 1f) * num));
			localPosition = new Vector3(0f, y2, 0f);
			break;
		case AttackFXType.Line:
			hitAreaSR.sprite = hitAreaSpriteRectangle;
			num = 0.5f;
			vector = ((angle / 90f % 2f != 0f) ? (new Vector3(sizeOfHitCollider.x, sizeOfHitCollider.z, 1f) * num) : (new Vector3(sizeOfHitCollider.z, sizeOfHitCollider.x, 1f) * num));
			localPosition = new Vector3(centerOfHitCollider.x, y2, centerOfHitCollider.z);
			break;
		case AttackFXType.Shockwave:
			hitAreaSR.sprite = hitAreaSpriteCircle;
			vector = new Vector3(sizeOfHitCollider.x, sizeOfHitCollider.z, 1f) * num;
			break;
		default:
			hitAreaSR.sprite = hitAreaSpriteRectangle;
			num = 0.5f;
			vector = new Vector3(sizeOfHitCollider.x, sizeOfHitCollider.z, 1f) * num;
			break;
		}
		hitAreaSR.transform.localScale = vector;
		hitAreaSR.transform.localPosition = localPosition;
		hitAreaSR.transform.localRotation = Quaternion.LookRotation(vec, Vector3.up) * Quaternion.Euler(90f, y3, 0f);
	}

	public static float GetConditionsMovementSpeedMultiplier(in DynamicBuffer<SummarizedConditionEffectsBuffer> summarizedConditionEffectsBuffer)
	{
		int value = summarizedConditionEffectsBuffer[2].value;
		float y = 1f + (float)value / 1000f;
		return math.max(0.1f, y);
	}

	public static float3 GetLeashForce(Entity entity, in LeashingCD leashingCD, ComponentLookup<LocalTransform> translationLookup)
	{
		if (leashingCD.leashedEntity == Entity.Null || !translationLookup.TryGetComponent(leashingCD.leashedEntity, out var componentData))
		{
			return float3.zero;
		}
		return CalculateLeashForce(translationLookup[entity].Position, componentData.Position);
	}

	private static float3 CalculateLeashForce(float3 ourPos, float3 leashedPos)
	{
		float3 x = leashedPos - ourPos;
		float3 @float = math.normalizesafe(leashedPos - ourPos);
		float num = math.length(x) - 5f;
		if (num < 0f)
		{
			num = 0f;
		}
		return @float * num * 0.3f;
	}

	public static float3 GetCornerSmoothingFromVectorInternal(float3 worldPosition, float3 velocityVector, in CollisionWorld collisionWorld, in CornerSmoothingCD cornerSmoothingCD, in PlayerStateCD playerStateCD)
	{
		float3 @float = velocityVector;
		Direction dir = Direction.FromVector_Strict_ZLeaning(@float);
		bool flag = Mathf.Abs(dir.isH ? @float.z : @float.x) > 0.05f;
		Direction escapeDir = Direction.zero;
		if (!CornerSmoothingUtility.TestMovement(in collisionWorld, in cornerSmoothingCD, in playerStateCD, worldPosition + new float3(0f, 0.5f, 0f), dir, out escapeDir))
		{
			if (!flag)
			{
				if (escapeDir != Direction.zero)
				{
					float blendFactor = cornerSmoothingCD.smoothingData.Value.cornerMovementBlend * cornerSmoothingCD.cornerMovementBlendMultiplier;
					return @float.Blend((float3)escapeDir.vec3, blendFactor);
				}
			}
			else if (cornerSmoothingCD.smoothingData.Value.experimentalWallSmoothingEnabled)
			{
				return @float.Blend((float3)dir.ExcludeAxis(@float), cornerSmoothingCD.smoothingData.Value.wallMovementBlend);
			}
		}
		else if (flag && escapeDir != Direction.zero)
		{
			Vector3 vec = escapeDir.FilterAxis(@float);
			if (escapeDir != Direction.FromVector(vec, 0f))
			{
				return @float.Blend((float3)dir.FilterAxis(@float), cornerSmoothingCD.smoothingData.Value.wallMovementBlend);
			}
		}
		return @float;
	}

	public static void Pushback(Entity playerEntity, float3 force, in PlayerStateCD playerStateCD, ComponentLookup<ReceivedPushbackCD> receivedPushbackLookup, NetworkTick currentTick, uint tickRate)
	{
		if (!playerStateCD.HasAnyState(PlayerStateEnum.MinecartRiding) && !playerStateCD.HasAnyState(PlayerStateEnum.BoatRiding))
		{
			receivedPushbackLookup.GetRefRW(playerEntity).ValueRW.AddPushback(force.ToFloat2(), currentTick, tickRate);
		}
	}

	public override void UpdateAnimatorSpeedAndOrientation()
	{
		AnimationSpeedCD componentData = EntityUtility.GetComponentData<AnimationSpeedCD>(base.entity, base.world);
		animator.SetFloat(-1801483167, componentData.movementX);
		animator.SetFloat(-476529417, componentData.movementY);
		UpdateWalkSpeed(componentData.speed);
		AnimationOrientationCD componentData2 = EntityUtility.GetComponentData<AnimationOrientationCD>(base.entity, base.world);
		animator.SetFloat(898384463, componentData2.facingDirection.vec2.x);
		animator.SetFloat(1116435161, componentData2.facingDirection.vec2.y);
	}

	public static void PlayAnimationTrigger(int animID, NetworkTick currentTick, DynamicBuffer<AnimationBuffer> animationBuffer, ref AnimationBufferPointer animationBufferPointer)
	{
		if (animationBuffer.GetLastAddedElement(in animationBufferPointer).animID == -414722770 && animID != 1352515405)
		{
			Debug.LogWarning("ignoring animation that isn't reset after death");
		}
		else
		{
			AnimationUtilities.TriggerAnimation(animID, currentTick, animationBuffer, ref animationBufferPointer);
		}
	}

	protected override bool ShouldPlayAnimTrigger(int animID)
	{
		bool flag = true;
		if (animID == 1975517117 && lastAnim == 577303787)
		{
			flag = false;
		}
		return base.ShouldPlayAnimTrigger(animID) && flag;
	}

	protected override void HandleAnimationTrigger(int animID)
	{
		if (animID == -414722770)
		{
			reorientationBlocked = false;
			facingDirection = Direction.back;
			UpdateAnimatorSpeedAndOrientation();
			reorientationBlocked = true;
			Manager.effects.PlayPuff(PuffID.BloodSpurt, base.RenderPosition, 50);
			if (acidLoop != null)
			{
				acidLoop.FadeOutAndStop(0.5f);
			}
		}
		else if (!isLocal && acidLoop != null)
		{
			acidLoop.StopNow();
		}
		switch (animID)
		{
		case -2099238391:
		case -1014102059:
		case -731100560:
		case -34540245:
		case 577303787:
		case 1203776827:
		case 1763897515:
		case 2061642980:
		{
			ObjectID objectID = visuallyEquippedContainedObject.objectID;
			if (isLocal)
			{
				objectID = ((!HeldItemIsBroken()) ? GetHeldObject().objectID : ObjectID.None);
			}
			if (PugDatabase.HasComponent<CustomAttackSoundCD>(objectID))
			{
				CustomAttackSoundCD component = PugDatabase.GetComponent<CustomAttackSoundCD>(objectID);
				if (component.attackSoundId != 0)
				{
					AudioManager.SfxFollowTransform(component.attackSoundId, base.transform, 1f, 1f, loop: false, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, playOnGamepad: true);
				}
				else
				{
					AudioManager.SfxFollowTransform(SfxID.whip, base.transform, 0.5f, 1f, 0.2f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: false, 16f, 10f, muteVolumeWhilePaused: true, freeAudioSourceAfterItStoppedPlaying: true, playOnGamepad: true);
				}
			}
			else if (animID != -1014102059 && !PugDatabase.HasComponent<CritterCD>(objectID))
			{
				AudioManager.SfxFollowTransform(SfxID.whip, base.transform, 0.5f, 1f, 0.2f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: false, 16f, 10f, muteVolumeWhilePaused: true, freeAudioSourceAfterItStoppedPlaying: true, playOnGamepad: true);
			}
			break;
		}
		case 1984257893:
		{
			ObjectInfo objectInfo = PugDatabase.GetObjectInfo(visuallyEquippedContainedObject.objectID);
			if (objectInfo != null && objectInfo.objectType == ObjectType.RoofingTool)
			{
				AudioManager.SfxFollowTransform(SfxTableID.roofingTool, base.transform, 1f, 1f, loop: false, freeAudioSourceAfterItStoppedPlaying: true, AudioManager.MixerGroupEnum.EFFECTS, reuseSfxs: false, playOnGamepad: true);
			}
			break;
		}
		}
		UpdateAnimationSounds(animID);
		base.HandleAnimationTrigger(animID);
	}

	protected virtual void UpdateWalkSpeed(float speed)
	{
		animator.SetFloat(-1985230220, speed);
	}

	private void UpdateBlinking()
	{
		if (!blinkTimer.isRunning)
		{
			blinkTimer.Start(UnityEngine.Random.Range(8f, 12f));
		}
		else if (blinkTimer.isTimerElapsed)
		{
			blinkTimer.Start(UnityEngine.Random.Range(8f, 12f));
			if (EntityUtility.GetComponentData<PlayerStateCD>(base.entity, base.world).HasAnyState(PlayerStateEnum.Walk))
			{
				animator.SetTrigger(842569181);
			}
		}
	}

	public void GetSmallGlowSourceObjects(ref NativeList<ObjectID> smallGlowSourceObjects)
	{
		equipmentHandler.GetActiveVisibleArmorObjectIDs(ref smallGlowSourceObjects, this);
		ObjectDataCD heldObject = GetHeldObject();
		if (heldObject.amount > 0)
		{
			smallGlowSourceObjects.Add(in heldObject.objectID);
		}
		if (IsShielded())
		{
			ObjectID value = GetOffHand().objectID;
			smallGlowSourceObjects.Add(in value);
		}
	}

	public void SetAnimSROffset(Vector3 _srOffSetPos)
	{
		srOffSetPos = _srOffSetPos;
		srOffset.localPosition = srOffSetPos;
	}

	public void ResetAnimSROffset()
	{
		srOffSetPos = defaultSROffSetPos;
		srOffset.localPosition = srOffSetPos;
	}

	public void UpdateAnimSROffset()
	{
		srOffset.localPosition = srOffSetPos;
		srOffSetPos = defaultSROffSetPos;
	}

	public void SetAnimSROffsetRot(Quaternion _srOffSetRot)
	{
		srOffSetRot = _srOffSetRot;
		srOffset.localRotation = srOffSetRot;
	}

	public void ResetAnimSROffsetRot()
	{
		srOffSetRot = Quaternion.identity;
		srOffset.localRotation = srOffSetRot;
	}

	public void UpdateAnimSROffsetRot()
	{
		srOffset.localRotation = srOffSetRot;
		srOffSetRot = Quaternion.identity;
	}

	public void SpawnDashEffect(float3 direction)
	{
		dashEffect.transform.localRotation = quaternion.LookRotation(direction, Vector3.up);
		dashEffect.Play();
	}

	public bool IsEquippedSlotButtonDown()
	{
		return inputModule.IsSlotButtonCurrentlyDown(equippedVisualSlotIndex);
	}

	public bool IsAnySlotEquipped()
	{
		if (equipmentSlots.Count > 0)
		{
			return equipmentSlots[equippedVisualSlotIndex] != null;
		}
		return false;
	}

	public EquipmentSlot GetEquippedSlot()
	{
		if (!IsAnySlotEquipped())
		{
			return null;
		}
		return equipmentSlots[equippedVisualSlotIndex];
	}

	public ObjectDataCD GetHeldObject()
	{
		EquippedObjectCD componentData = EntityUtility.GetComponentData<EquippedObjectCD>(base.entity, base.world);
		int index = (isLocal ? equippedSlotIndex : componentData.equippedSlotIndex);
		return GetInventorySlot(index).objectData;
	}

	public ObjectType GetHeldObjectType()
	{
		return PugDatabase.GetObjectInfo(GetHeldObject().objectID)?.objectType ?? ObjectType.NonUsable;
	}

	public ContainedObjectsBuffer GetOffHand()
	{
		return equipmentHandler.offHandInventoryHandler.GetContainedObjectData(0);
	}

	public EquipmentSlot UnequipEquippedSlot()
	{
		EquipmentSlot equippedSlot = GetEquippedSlot();
		if (equippedSlot != null)
		{
			equippedSlot.OnUnequip(this);
		}
		return equippedSlot;
	}

	public bool EquipSlot(int slotIndex)
	{
		if (IsAnySlotEquipped())
		{
			UnequipEquippedSlot();
		}
		lastUsedSlotIndex = slotIndex;
		equippedSlotIndex = slotIndex;
		equipmentSlots[equippedVisualSlotIndex].OnEquip(this);
		Manager.ui.OnEquipmentSlotActivated(equippedVisualSlotIndex);
		UpdateEquippedSlotVisuals();
		return true;
	}

	private void HideAllEquippedSlotVisuals()
	{
		carryableSwingItemSprite.gameObject.SetActive(value: false);
		carryableRangeItemSprite.gameObject.SetActive(value: false);
		carryableShieldItemSprite.gameObject.SetActive(value: false);
		carryableBigSpearItemSprite.gameObject.SetActive(value: false);
		carryableBigSwingItemSprite.gameObject.SetActive(value: false);
		carryableDrillToolSprite.gameObject.SetActive(value: false);
		carryableTorch.SetActive(value: false);
		carryablePlaceItemSprite.gameObject.SetActive(value: false);
		carryableFishingRodSprite.gameObject.SetActive(value: false);
		instrumentSprite.gameObject.SetActive(value: false);
	}

	public void HideCarryable()
	{
		carryableHandle.SetActive(value: false);
	}

	public void ShowCarryable()
	{
		carryableHandle.SetActive(value: true);
	}

	public void UpdateEquippedSlotVisuals()
	{
		EquipmentSlotType equipmentSlotType = EquipmentSlotType.NonUsableSlot;
		ObjectID objectID = ObjectID.None;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		if (base.entityExist)
		{
			EquippedObjectCD componentData = EntityUtility.GetComponentData<EquippedObjectCD>(base.entity, base.world);
			int index = (isLocal ? equippedSlotIndex : componentData.equippedSlotIndex);
			ContainedObjectsBuffer containedObject = GetInventorySlot(index);
			if (EntityUtility.GetComponentData<PlayerRoutineCD>(base.entity, base.world).activeRoutine == PlayerRoutines.Shielding && EntityUtility.GetComponentData<UseOffHandStateCD>(base.entity, base.world).shieldedAmount > 0f)
			{
				containedObject = GetOffHand();
				objectID = containedObject.objectID;
				num = containedObject.variation;
				num2 = containedObject.amount;
				num3 = containedObject.auxDataIndex;
				equipmentSlotType = EquipmentSlotType.Shield;
			}
			else
			{
				if (PugDatabase.HasComponent<PetCD>(containedObject.objectID) && !InventoryHandler.TryGetExtraInventoryData<PetSkinCD>(containedObject, out var _))
				{
					containedObject = default(ContainedObjectsBuffer);
				}
				objectID = ((!HeldItemIsBroken()) ? containedObject.objectID : ObjectID.None);
				num = containedObject.variation;
				num2 = containedObject.amount;
				num3 = containedObject.auxDataIndex;
				ObjectInfo objectInfo = PugDatabase.GetObjectInfo(objectID);
				ComponentLookup<CattleCD> componentLookup = querySystem.GetComponentLookup<CattleCD>(isReadOnly: true);
				if (querySystem.TryGetSingleton<WorldInfoCD>(out var value))
				{
					equipmentSlotType = ((objectInfo != null) ? GetEquippedSlotTypeForObjectType(objectInfo.objectType, componentData.equipmentPrefab, componentLookup, value) : EquipmentSlotType.NonUsableSlot);
				}
			}
			if (isLocal && (clientInput.equippedSlotIndex != equippedSlotIndex || visuallyEquippedContainedObject.objectID != objectID || visuallyEquippedContainedObject.variation != num || visuallyEquippedContainedObject.amount != num2 || visuallyEquippedContainedObject.auxDataIndex != num3))
			{
				clientInput.equippedSlotIndex = (byte)equippedSlotIndex;
				equipmentSlots[equippedVisualSlotIndex].OnEquip(this);
			}
		}
		bool flag = visuallyEquippedContainedObject.amount != num2;
		bool flag2 = visuallyEquippedContainedObject.objectID != objectID || visuallyEquippedContainedObject.variation != num || visuallyEquippedSlotType != equipmentSlotType || visuallyEquippedContainedObject.auxDataIndex != num3;
		if (flag || flag2)
		{
			bool num4 = flag && !flag2;
			visuallyEquippedContainedObject = new ContainedObjectsBuffer
			{
				objectData = new ObjectDataCD
				{
					objectID = objectID,
					variation = num,
					amount = num2
				},
				auxDataIndex = num3
			};
			visuallyEquippedSlotType = equipmentSlotType;
			if (num4 && objectID != ObjectID.Bucket)
			{
				return;
			}
			HideAllEquippedSlotVisuals();
			ObjectInfo objectInfo2 = PugDatabase.GetObjectInfo(visuallyEquippedContainedObject.objectID, visuallyEquippedContainedObject.variation);
			switch (equipmentSlotType)
			{
			case EquipmentSlotType.MeleeWeaponSlot:
			case EquipmentSlotType.ShovelSlot:
			case EquipmentSlotType.HoeSlot:
			case EquipmentSlotType.BugNet:
			case EquipmentSlotType.SeederSlot:
				if (PugDatabase.HasComponent<MeleeWeaponCD>(visuallyEquippedContainedObject.objectID) && PugDatabase.GetComponent<MeleeWeaponCD>(visuallyEquippedContainedObject.objectID).isBigSpearWeapon)
				{
					ActivateCarryableItemSpriteAndSkin(carryableBigSpearItemSprite, carryableBigSpearItemSkin, objectInfo2, visuallyEquippedContainedObject);
				}
				else if (PugDatabase.HasComponent<MeleeWeaponCD>(visuallyEquippedContainedObject.objectID) && PugDatabase.GetComponent<MeleeWeaponCD>(visuallyEquippedContainedObject.objectID).isBigSwingWeapon)
				{
					ActivateCarryableItemSpriteAndSkin(carryableBigSwingItemSprite, carryableBigSwingItemSkin, objectInfo2, visuallyEquippedContainedObject);
				}
				else if (PugDatabase.HasComponent<MeleeWeaponCD>(visuallyEquippedContainedObject.objectID) && PugDatabase.GetComponent<MeleeWeaponCD>(visuallyEquippedContainedObject.objectID).moveFreely)
				{
					ActivateCarryableItemSpriteAndSkin(carryableDrillToolSprite, carryableDrillToolSkin, objectInfo2, visuallyEquippedContainedObject);
				}
				else
				{
					ActivateCarryableItemSpriteAndSkin(carryableSwingItemSprite, carryableSwingItemSkinSkin, objectInfo2, visuallyEquippedContainedObject);
				}
				break;
			case EquipmentSlotType.RangeWeaponSlot:
				if (objectInfo2.objectType == ObjectType.ThrowingWeapon)
				{
					ActivatePlaceItemSprite(objectInfo2);
				}
				else
				{
					ActivateCarryableItemSpriteAndSkin(carryableRangeItemSprite, carryableRangeItemSkinSkin, objectInfo2, visuallyEquippedContainedObject);
				}
				break;
			case EquipmentSlotType.Shield:
				ActivateCarryableItemSpriteAndSkin(carryableShieldItemSprite, carryableShieldItemSkinSkin, objectInfo2, visuallyEquippedContainedObject);
				break;
			case EquipmentSlotType.InstrumentSlot:
				ActivateCarryableItemSpriteAndSkin(instrumentSprite, instrumentSkin, objectInfo2, visuallyEquippedContainedObject);
				break;
			case EquipmentSlotType.FishingRodSlot:
				carryableFishingRodSprite.gameObject.SetActive(value: true);
				carryableFishingRodSprite.sprite = objectInfo2?.icon;
				carryableFishingRodSkin.SetSkin((carryableFishingRodSprite.sprite != null) ? carryableFishingRodSprite.sprite.texture : null);
				break;
			case EquipmentSlotType.NonUsableSlot:
			case EquipmentSlotType.PlaceObjectSlot:
			case EquipmentSlotType.EatableSlot:
			case EquipmentSlotType.WaterCanSlot:
			case EquipmentSlotType.CastingSlot:
			case EquipmentSlotType.PaintToolSlot:
			case EquipmentSlotType.BucketSlot:
			case EquipmentSlotType.RoofingToolSlot:
			case EquipmentSlotType.SummoningWeaponSlot:
			case EquipmentSlotType.EquipGearSlot:
				ActivatePlaceItemSprite(objectInfo2);
				break;
			}
			UpdateCarryableEffects();
			animator.Update(0f);
		}
		if (carryablePlaceItemSprite.gameObject.activeInHierarchy)
		{
			Manager.ui.ApplyAnyIconGradientMap(visuallyEquippedContainedObject, carryablePlaceItemSprite);
		}
	}

	private void ActivatePlaceItemSprite(ObjectInfo entityInfo)
	{
		if (entityInfo != null && entityInfo.objectID == ObjectID.Torch)
		{
			carryableTorch.SetActive(value: true);
		}
		else
		{
			carryablePlaceItemSprite.gameObject.SetActive(value: true);
			Sprite iconOverride = Manager.ui.itemOverridesTable.GetIconOverride(visuallyEquippedContainedObject.objectData, getSmallIcon: true);
			carryablePlaceItemSprite.sprite = ((iconOverride != null) ? iconOverride : entityInfo?.smallIcon);
		}
		int index = (isLocal ? equippedSlotIndex : EntityUtility.GetComponentData<EquippedObjectCD>(base.entity, base.world).equippedSlotIndex);
		carryablePlaceItemColorReplacer.UpdateColorReplacerFromObjectData(GetInventorySlot(index));
	}

	private void ActivateCarryableItemSpriteAndSkin(SpriteRenderer carryableSprite, SpriteSheetSkin skin, ObjectInfo entityInfo, ContainedObjectsBuffer visuallyEquipped)
	{
		carryableSprite.gameObject.SetActive(value: true);
		Sprite iconOverride = Manager.ui.itemOverridesTable.GetIconOverride(base.objectData, getSmallIcon: false);
		Sprite sprite = ((iconOverride != null) ? iconOverride : ((entityInfo == null) ? null : ((entityInfo.additionalSprites.Count > 0) ? entityInfo.additionalSprites[0] : entityInfo.icon)));
		carryableSprite.sprite = sprite;
		skin.SetSkin((carryableSprite.sprite != null) ? carryableSprite.sprite.texture : null);
		SetSpriteRendererEmissive(carryableSprite, (entityInfo != null && entityInfo.additionalSprites.Count > 1) ? entityInfo.additionalSprites[1].texture : null);
		Manager.ui.ApplyAnyIconGradientMap(visuallyEquipped, carryableSprite);
	}

	private void SetSpriteRendererEmissive(SpriteRenderer spriteRenderer, Texture2D emissiveTexture)
	{
		spriteRenderer.material.SetTexture(EmissiveTex, emissiveTexture);
	}

	private void UpdateCarryableEffects()
	{
		foreach (CarryableEffect carryableEffect in carryableEffects)
		{
			if (carryableEffect.objectWithEffect == visuallyEquippedContainedObject.objectID)
			{
				carryableEffect.effectObject.SetActive(value: true);
			}
			else
			{
				carryableEffect.effectObject.SetActive(value: false);
			}
		}
	}

	public ContainedObjectsBuffer GetInventorySlot(int index)
	{
		if (playerCraftingHandler == null)
		{
			Debug.LogError("try access inventory handler too late");
			return default(ContainedObjectsBuffer);
		}
		if (index >= 0 && index < playerInventoryHandler.size)
		{
			return playerInventoryHandler.GetContainedObjectData(index);
		}
		for (int i = 0; i < 4; i++)
		{
			InventoryHandler inventoryHandler = equipmentHandler.pouchInventorySlotsHandlers[i];
			int index2 = index - inventoryHandler.startPosInBuffer;
			if (index >= inventoryHandler.startPosInBuffer && index < inventoryHandler.startPosInBuffer + inventoryHandler.size)
			{
				return inventoryHandler.GetContainedObjectData(index2);
			}
		}
		return default(ContainedObjectsBuffer);
	}

	public void CloseAnyOpenInventory()
	{
		Manager.ui.HideAllInventoryAndCraftingUI();
		SetActiveInventoryHandler(null);
	}

	public void OpenPlayerInventory()
	{
		SetActiveCraftingHandler(playerCraftingHandler);
		Manager.ui.OnPlayerInventoryOpen();
	}

	public void UpdateAllEquipmentSlots()
	{
		for (int i = hotbarStartIndex; i < hotbarStartIndex + 10; i++)
		{
			UpdateEquipmentSlot(i);
		}
	}

	public void UpdateEquipmentSlot(int index)
	{
		int index2 = index - hotbarStartIndex;
		EquipmentSlot equipmentSlot = equipmentSlots[index2];
		ObjectDataCD objectDataCD = GetInventorySlot(index).objectData;
		ObjectType objectType = PugDatabase.GetObjectInfo(objectDataCD.objectID, objectDataCD.variation)?.objectType ?? ObjectType.NonUsable;
		if (equipmentSlot != null)
		{
			equipmentSlot.inventoryIndexReference = index;
			if (equipmentSlot.GetType() == GetSlotTypeForObjectType(objectType, objectDataCD))
			{
				if (index == equippedSlotIndex)
				{
					UpdateEquippedSlotVisuals();
				}
				return;
			}
			if (equippedSlotIndex == index)
			{
				UnequipEquippedSlot();
			}
			equipmentSlots[index2].Free();
			equipmentSlots[index2] = null;
		}
		else
		{
			Debug.LogError($"got null from equipment slot {index}");
		}
		EquipmentSlot value = ((objectDataCD.objectID != 0) ? CreateEquipmentSlotToBeUsedForObject(objectType, index, this) : CreateEquipmentSlotToBeUsedForObject(ObjectType.NonUsable, index, this));
		equipmentSlots[index2] = value;
		if (equippedSlotIndex == index)
		{
			EquipSlot(index);
		}
		Manager.ui.OnEquipmentSlotUpdated(index2);
	}

	public bool CanConsumeEntityInSlot(EquipmentSlot equipmentSlot, int consumedAmount)
	{
		int inventoryIndexReference = equipmentSlot.inventoryIndexReference;
		if (!playerInventoryHandler.HasObject(inventoryIndexReference))
		{
			return false;
		}
		ContainedObjectsBuffer inventorySlot = GetInventorySlot(inventoryIndexReference);
		if (inventorySlot.amount < consumedAmount && !PugDatabase.HasComponent<CattleCD>(inventorySlot.objectID))
		{
			return false;
		}
		return true;
	}

	public static bool CanConsumeEntityInSlot(Entity containedObjectPrefab, ObjectData objectData, int consumedAmount, ComponentLookup<CattleCD> cattleLookup)
	{
		if (objectData.objectID != 0)
		{
			if (objectData.amount < consumedAmount)
			{
				return cattleLookup.HasComponent(containedObjectPrefab);
			}
			return true;
		}
		return false;
	}

	public void InitInventoryCraftingAndEquipment()
	{
		equippedSlotIndex = 0;
		hotbarStartIndex = 0;
		hotbarEndIndex = 10;
		playerCraftingHandler = new CraftingHandler(this, base.world, treatAsAllInventoriesForTransfer: true);
		equipmentHandler = new EquipmentHandler(this, base.world);
		SetActiveEquipmentPreset(EntityUtility.GetComponentData<ActiveEquipmentPresetCD>(base.entity, base.world).Value);
		vanitySlotsHandler = new VanitySlotsHandler(this, base.world);
		sellSlotsHandler = new SellSlotsHandler(this, base.world);
		trashCanHandler = new TrashCanHandler(this, base.world);
		upgradeSlotHandler = new UpgradeSlotHandler(this, base.world);
		activeCraftingHandler = null;
		if (!isLocal)
		{
			return;
		}
		Manager.ui.itemSlotsBar.activeHotBarRow = 0;
		previousInventoryObjects = new List<ContainedObjectsBuffer>(playerInventoryHandler.maxSize);
		for (int i = 0; i < playerInventoryHandler.maxSize; i++)
		{
			previousInventoryObjects.Add(default(ContainedObjectsBuffer));
		}
		if (Manager.saves.IsFirstTimePlayingOnAnyServer())
		{
			if (Manager.saves.IsCreativeModeCharacter())
			{
				QueueInputAction(new UIInputActionData
				{
					action = UIInputAction.InventoryChange,
					inventoryChangeData = Create.CreateItem(base.entity, equipmentHandler.lanternInventoryHandler.startPosInBuffer, ObjectID.OrbLantern, 1, base.WorldPosition, 0)
				});
			}
			int num = 1;
			if (rolePerksTable != null)
			{
				Vector3 renderPosition = base.RenderPosition;
				byte role = activeCustomization.role;
				RolePerksTable.Perks perks = rolePerksTable.GetPerks((CharacterRole)activeCustomization.role);
				if (role != 6)
				{
					SetSkillLevel(perks.starterSkill, 3);
				}
				foreach (ObjectData starterItem in perks.starterItems)
				{
					playerCommandSystem.CreateItem(base.entity, num, starterItem.objectID, starterItem.amount, renderPosition, starterItem.variation);
					num++;
				}
			}
			if (extraLootAtStartTable != null)
			{
				Debug.Log("Adding start items from dlc's");
				Vector3 renderPosition2 = base.RenderPosition;
				foreach (ExtraLootAtStartTable.Loot item in extraLootAtStartTable.loot)
				{
					if ((item.needsApp != 0 && !Manager.platform.HasApp(item.needsApp)) || (item.needsDlc != 0 && !Manager.platform.HasDlc(item.needsDlc)))
					{
						continue;
					}
					foreach (ObjectData @object in item.objects)
					{
						playerCommandSystem.CreateItem(base.entity, num, @object.objectID, @object.amount, renderPosition2, @object.variation);
						num++;
					}
				}
			}
			PlayerCustomization characterCustomization = Manager.saves.GetCharacterCustomization();
			ref FixedString32Bytes reference = ref characterCustomization.name;
			FixedString32Bytes b = "Greggy";
			if (reference == b)
			{
				playerCommandSystem.CreateItem(base.entity, num, ObjectID.WallDirtBlock, 10, base.RenderPosition);
				num++;
			}
		}
		equipmentSlots.Clear();
		for (int j = 0; j < 10; j++)
		{
			ContainedObjectsBuffer inventorySlot = GetInventorySlot(j);
			ObjectType objectType = ObjectType.NonUsable;
			if (inventorySlot.objectID != 0 && PugDatabase.GetObjectInfo(inventorySlot.objectID, inventorySlot.variation) != null)
			{
				objectType = PugDatabase.GetObjectInfo(inventorySlot.objectID, inventorySlot.variation).objectType;
			}
			equipmentSlots.Add(CreateEquipmentSlotToBeUsedForObject(objectType, j, this));
			Manager.ui.OnEquipmentSlotUpdated(j);
		}
		EquipSlot(hotbarStartIndex);
		Manager.saves.ClearLastServerConnectedTo();
		Manager.saves.AddServerConnectCount();
	}

	public void SetActiveEquipmentPreset(int presetIndex)
	{
		activeEquipmentPreset = presetIndex;
		equipmentHandler.UpdateEquipmentPreset(activeEquipmentPreset);
		clientInput.equipmentPresetIndex = (byte)activeEquipmentPreset;
	}

	private void UpdateEquipmentPreset()
	{
		int value = EntityUtility.GetComponentData<ActiveEquipmentPresetCD>(base.entity, base.world).Value;
		if (value != activeEquipmentPreset)
		{
			activeEquipmentPreset = value;
			equipmentHandler.UpdateEquipmentPreset(activeEquipmentPreset);
		}
	}

	private EquipmentSlot CreateEquipmentSlotToBeUsedForObject(ObjectType objectType, int inventoryIndex, PlayerController owner)
	{
		ObjectDataCD objectDataCD = GetInventorySlot(inventoryIndex).objectData;
		EquipmentSlot equipmentSlot = Manager.memory.GetFreeComponent(GetSlotTypeForObjectType(objectType, objectDataCD), deferOnOccupied: true) as EquipmentSlot;
		if (equipmentSlot != null)
		{
			equipmentSlot.inventoryIndexReference = inventoryIndex;
			equipmentSlot.OnPickUp(owner, fireSceneEvent: false);
			equipmentSlot.OnOccupied();
		}
		else
		{
			Debug.LogError("could not allocate equipment slot");
		}
		return equipmentSlot;
	}

	private Type GetSlotTypeForObjectType(ObjectType objectType, ObjectDataCD objectData)
	{
		if ((Manager.networking.serverWorldMode & WorldMode.Creative) != 0 && (objectType == ObjectType.NonObtainable || objectType == ObjectType.Creature))
		{
			return typeof(PlaceObjectSlot);
		}
		switch (objectType)
		{
		case ObjectType.Helm:
		case ObjectType.BreastArmor:
		case ObjectType.PantsArmor:
		case ObjectType.Necklace:
		case ObjectType.Ring:
		case ObjectType.Offhand:
		case ObjectType.Bag:
		case ObjectType.Lantern:
		case ObjectType.Pouch:
		case ObjectType.Pet:
			return typeof(EquipGearSlot);
		case ObjectType.MeleeWeapon:
		case ObjectType.MiningPick:
		case ObjectType.Sledge:
		case ObjectType.DrillTool:
		case ObjectType.BeamWeapon:
			return typeof(MeleeWeaponSlot);
		case ObjectType.RangeWeapon:
		case ObjectType.ThrowingWeapon:
			return typeof(RangeWeaponSlot);
		case ObjectType.SummoningWeapon:
			return typeof(SummoningWeaponSlot);
		case ObjectType.Shovel:
			return typeof(ShovelSlot);
		case ObjectType.Hoe:
			return typeof(HoeSlot);
		case ObjectType.PlaceablePrefab:
		case ObjectType.Critter:
			return typeof(PlaceObjectSlot);
		case ObjectType.Eatable:
			return typeof(EatableSlot);
		case ObjectType.WaterCan:
			return typeof(WaterCanSlot);
		case ObjectType.Bucket:
			return typeof(BucketSlot);
		case ObjectType.Seeder:
			return typeof(SeederSlot);
		case ObjectType.CastingItem:
			return typeof(CastingItemSlot);
		case ObjectType.PaintTool:
			return typeof(PaintToolSlot);
		case ObjectType.FishingRod:
			return typeof(FishingRodSlot);
		case ObjectType.BugNet:
			return typeof(BugNetSlot);
		case ObjectType.Instrument:
			return typeof(InstrumentSlot);
		case ObjectType.RoofingTool:
			return typeof(RoofingToolSlot);
		case ObjectType.Creature:
			if (PugDatabase.HasComponent<CattleCD>(objectData))
			{
				return typeof(PlaceObjectSlot);
			}
			return typeof(NonUsableSlot);
		default:
			return typeof(NonUsableSlot);
		}
	}

	public static void GetCooldownTypeFromCooldownIdentifier(SharedCooldownIdentifier sharedCooldownIdentifier, ObjectType objectType, ObjectID objectID, out bool isSynced, out SyncedSharedCooldownType syncedSharedCooldownType, out LocalSharedCooldownType localSharedCooldownType)
	{
		switch (sharedCooldownIdentifier)
		{
		case SharedCooldownIdentifier.SlotType:
			GetCooldownTypeFromSlotType(objectType, out isSynced, out syncedSharedCooldownType, out localSharedCooldownType);
			break;
		case SharedCooldownIdentifier.ObjectID:
			GetCooldownTypeFromObjectID(objectID, out isSynced, out syncedSharedCooldownType, out localSharedCooldownType);
			break;
		case SharedCooldownIdentifier.HealingPotion:
			isSynced = true;
			syncedSharedCooldownType = SyncedSharedCooldownType.HealingPotion;
			localSharedCooldownType = LocalSharedCooldownType.Invalid;
			break;
		default:
			throw new ArgumentOutOfRangeException("sharedCooldownIdentifier", sharedCooldownIdentifier, null);
		}
	}

	public static void GetCooldownTypeFromSlotType(ObjectType objectType, out bool isSynced, out SyncedSharedCooldownType syncedSharedCooldownType, out LocalSharedCooldownType localSharedCooldownType)
	{
		localSharedCooldownType = LocalSharedCooldownType.Invalid;
		isSynced = true;
		switch (objectType)
		{
		case ObjectType.Helm:
		case ObjectType.BreastArmor:
		case ObjectType.PantsArmor:
		case ObjectType.Necklace:
		case ObjectType.Ring:
		case ObjectType.Offhand:
		case ObjectType.Bag:
		case ObjectType.Lantern:
		case ObjectType.Pouch:
		case ObjectType.Pet:
			syncedSharedCooldownType = SyncedSharedCooldownType.EquipGearSlot;
			break;
		case ObjectType.MeleeWeapon:
		case ObjectType.MiningPick:
		case ObjectType.Sledge:
		case ObjectType.DrillTool:
		case ObjectType.BeamWeapon:
			syncedSharedCooldownType = SyncedSharedCooldownType.MeleeWeaponSlot;
			break;
		case ObjectType.RangeWeapon:
		case ObjectType.ThrowingWeapon:
			syncedSharedCooldownType = SyncedSharedCooldownType.RangeWeaponSlot;
			break;
		case ObjectType.SummoningWeapon:
			syncedSharedCooldownType = SyncedSharedCooldownType.SummoningWeaponSlot;
			break;
		case ObjectType.Shovel:
			syncedSharedCooldownType = SyncedSharedCooldownType.ShovelSlot;
			break;
		case ObjectType.Hoe:
			syncedSharedCooldownType = SyncedSharedCooldownType.HoeSlot;
			break;
		case ObjectType.PlaceablePrefab:
		case ObjectType.Critter:
			syncedSharedCooldownType = SyncedSharedCooldownType.PlaceObjectSlot;
			break;
		case ObjectType.Eatable:
			syncedSharedCooldownType = SyncedSharedCooldownType.EatableSlot;
			break;
		case ObjectType.WaterCan:
			syncedSharedCooldownType = SyncedSharedCooldownType.WaterCanSlot;
			break;
		case ObjectType.Bucket:
			syncedSharedCooldownType = SyncedSharedCooldownType.BucketSlot;
			break;
		case ObjectType.CastingItem:
			syncedSharedCooldownType = SyncedSharedCooldownType.CastingSlot;
			break;
		case ObjectType.PaintTool:
			syncedSharedCooldownType = SyncedSharedCooldownType.PaintToolSlot;
			break;
		case ObjectType.FishingRod:
			syncedSharedCooldownType = SyncedSharedCooldownType.FishingRodSlot;
			break;
		case ObjectType.BugNet:
			syncedSharedCooldownType = SyncedSharedCooldownType.BugNet;
			break;
		case ObjectType.Instrument:
			syncedSharedCooldownType = SyncedSharedCooldownType.InstrumentSlot;
			break;
		case ObjectType.RoofingTool:
			syncedSharedCooldownType = SyncedSharedCooldownType.RoofingToolSlot;
			break;
		default:
			syncedSharedCooldownType = SyncedSharedCooldownType.NonUsableSlot;
			break;
		}
	}

	private static void GetCooldownTypeFromObjectID(ObjectID objectID, out bool isSynced, out SyncedSharedCooldownType syncedSharedCooldownType, out LocalSharedCooldownType localSharedCooldownType)
	{
		if (objectID == ObjectID.CupidBow)
		{
			isSynced = true;
			syncedSharedCooldownType = SyncedSharedCooldownType.CupidBow;
			localSharedCooldownType = LocalSharedCooldownType.Invalid;
			return;
		}
		throw new Exception($"Not shared cooldown support for objectID {objectID}");
	}

	public static EquipmentSlotType GetEquippedSlotTypeForObjectType(ObjectType objectType, Entity objectPrefab, ComponentLookup<CattleCD> cattleLookup, WorldInfoCD worldInfo)
	{
		if (worldInfo.IsWorldModeEnabled(WorldMode.Creative) && (objectType == ObjectType.NonObtainable || objectType == ObjectType.Creature))
		{
			return EquipmentSlotType.PlaceObjectSlot;
		}
		switch (objectType)
		{
		case ObjectType.Helm:
		case ObjectType.BreastArmor:
		case ObjectType.PantsArmor:
		case ObjectType.Necklace:
		case ObjectType.Ring:
		case ObjectType.Offhand:
		case ObjectType.Bag:
		case ObjectType.Lantern:
		case ObjectType.Pouch:
		case ObjectType.Pet:
			return EquipmentSlotType.EquipGearSlot;
		case ObjectType.MeleeWeapon:
		case ObjectType.MiningPick:
		case ObjectType.Sledge:
		case ObjectType.DrillTool:
		case ObjectType.BeamWeapon:
			return EquipmentSlotType.MeleeWeaponSlot;
		case ObjectType.RangeWeapon:
		case ObjectType.ThrowingWeapon:
			return EquipmentSlotType.RangeWeaponSlot;
		case ObjectType.SummoningWeapon:
			return EquipmentSlotType.SummoningWeaponSlot;
		case ObjectType.Shovel:
			return EquipmentSlotType.ShovelSlot;
		case ObjectType.Hoe:
			return EquipmentSlotType.HoeSlot;
		case ObjectType.Seeder:
			return EquipmentSlotType.SeederSlot;
		case ObjectType.PlaceablePrefab:
		case ObjectType.Critter:
			return EquipmentSlotType.PlaceObjectSlot;
		case ObjectType.NonUsable:
		case ObjectType.Valuable:
		case ObjectType.UniqueCraftingComponent:
		case ObjectType.KeyItem:
			return EquipmentSlotType.NonUsableSlot;
		case ObjectType.Eatable:
			return EquipmentSlotType.EatableSlot;
		case ObjectType.WaterCan:
			return EquipmentSlotType.WaterCanSlot;
		case ObjectType.CastingItem:
			return EquipmentSlotType.CastingSlot;
		case ObjectType.PaintTool:
			return EquipmentSlotType.PaintToolSlot;
		case ObjectType.FishingRod:
			return EquipmentSlotType.FishingRodSlot;
		case ObjectType.Instrument:
			return EquipmentSlotType.InstrumentSlot;
		case ObjectType.BugNet:
			return EquipmentSlotType.BugNet;
		case ObjectType.Bucket:
			return EquipmentSlotType.BucketSlot;
		case ObjectType.RoofingTool:
			return EquipmentSlotType.RoofingToolSlot;
		case ObjectType.Creature:
			if (cattleLookup.HasComponent(objectPrefab))
			{
				return EquipmentSlotType.PlaceObjectSlot;
			}
			return EquipmentSlotType.NonUsableSlot;
		default:
			return EquipmentSlotType.NonUsableSlot;
		}
	}

	public void FreeAllEquipmentSlots()
	{
		if (IsAnySlotEquipped())
		{
			UnequipEquippedSlot();
		}
		for (int num = equipmentSlots.Count - 1; num >= 0; num--)
		{
			if (equipmentSlots[num] != null)
			{
				if (equipmentSlots[num].slotOwner == this)
				{
					equipmentSlots[num].Free();
				}
				equipmentSlots[num] = null;
			}
		}
		lastUsedSlotIndex = 0;
	}

	public void UpdateDiscoveredItems()
	{
		if (!isLocal)
		{
			return;
		}
		DetectUndiscoveredObjectsInInventory(playerInventoryHandler);
		if (activeInventoryHandler != null && activeInventoryHandler != playerInventoryHandler)
		{
			DetectUndiscoveredObjectsInInventory(activeInventoryHandler);
		}
		if (activeCraftingHandler != null && activeCraftingHandler.inventoryHandler != playerInventoryHandler)
		{
			DetectUndiscoveredObjectsInInventory(activeCraftingHandler.inventoryHandler);
			if (activeCraftingHandler.outputInventoryHandler != null)
			{
				DetectUndiscoveredObjectsInInventory(activeCraftingHandler.outputInventoryHandler);
			}
		}
		for (int i = 0; i < equipmentHandler.pouchInventorySlotsHandlers.Length; i++)
		{
			DetectUndiscoveredObjectsInInventory(equipmentHandler.pouchInventorySlotsHandlers[i]);
		}
		ContainedObjectsBuffer containedObjectData = mouseInventoryHandler.GetContainedObjectData(0);
		if (containedObjectData.objectID != 0 && !previousMouseObject.Equals(containedObjectData))
		{
			Manager.saves.SetObjectAsDiscovered(containedObjectData.objectData);
			previousMouseObject = containedObjectData;
		}
	}

	private void DetectUndiscoveredObjectsInInventory(InventoryHandler inventoryHandler)
	{
		for (int i = 0; i < inventoryHandler.size; i++)
		{
			ContainedObjectsBuffer containedObjectData = inventoryHandler.GetContainedObjectData(i);
			if (containedObjectData.objectID == ObjectID.None || previousInventoryObjects[i].Equals(containedObjectData))
			{
				continue;
			}
			if (Manager.saves.SetObjectAsDiscovered(containedObjectData.objectData) && !Manager.ui.isPlayerInventoryShowing && Manager.sceneHandler.isSceneHandlerReady && !Manager.sceneHandler.cutsceneIsPlaying)
			{
				ObjectInfo objectInfo = PugDatabase.GetObjectInfo(containedObjectData.objectID);
				if (objectInfo != null)
				{
					string[] formatFields = new string[1] { GetObjectName(containedObjectData, localize: true).text };
					Rarity rarity = objectInfo.rarity;
					Manager.ui.chatWindow.AddInfoText(formatFields, rarity, ChatWindow.MessageTextType.NewItem);
					switch (rarity)
					{
					case Rarity.Epic:
						Manager.rgb.TriggerEvent(RGBManager.Event.FindNewEpicItem);
						break;
					case Rarity.Legendary:
						Manager.rgb.TriggerEvent(RGBManager.Event.FindNewLegendaryItem);
						break;
					}
				}
			}
			previousInventoryObjects[i] = containedObjectData;
		}
	}

	public static TextAndFormatFields GetObjectName(ContainedObjectsBuffer containedObject, bool localize)
	{
		NameCD data;
		string text = (InventoryHandler.TryGetExtraInventoryData<NameCD>(containedObject, out data) ? data.Value.ToString() : null);
		ObjectID anyObjectIDReplaceForNameAndDesc = GetAnyObjectIDReplaceForNameAndDesc(containedObject.objectID);
		TextAndFormatFields textAndFormatFields = null;
		if (!string.IsNullOrEmpty(text))
		{
			textAndFormatFields = new TextAndFormatFields
			{
				text = text,
				dontLocalize = true,
				profanityFilter = true
			};
		}
		else if (anyObjectIDReplaceForNameAndDesc != 0)
		{
			bool flag = false;
			string text2 = "";
			string text3 = anyObjectIDReplaceForNameAndDesc.ToString();
			if (API.Authoring.ObjectProperties.TryGetPropertyString(anyObjectIDReplaceForNameAndDesc, "name", out var value))
			{
				text3 = value;
			}
			string nameTermOverride = Manager.ui.itemOverridesTable.GetNameTermOverride(new ObjectDataCD
			{
				objectID = anyObjectIDReplaceForNameAndDesc,
				variation = containedObject.variation
			});
			if (nameTermOverride != null)
			{
				text3 = nameTermOverride;
			}
			textAndFormatFields = new TextAndFormatFields();
			if (PugDatabase.HasComponent<CookedFoodCD>(anyObjectIDReplaceForNameAndDesc))
			{
				string[] array = new string[3];
				int num = 0;
				ObjectInfo obj = PugDatabase.GetObjectInfo(containedObject.objectID);
				switch (obj.rarity)
				{
				case Rarity.Rare:
					flag = true;
					text2 = "Rarity/Rare";
					break;
				case Rarity.Epic:
					flag = true;
					text2 = "Rarity/Epic";
					break;
				}
				string languageFromCode = LocalizationManager.GetLanguageFromCode(Manager.prefs.language, exactMatch: false);
				Gender languageGender = obj.GetLanguageGender(languageFromCode);
				string text4 = "";
				if (languageGender == Gender.Female)
				{
					text4 = "Female";
				}
				if (languageGender == Gender.Male)
				{
					text4 = "Male";
				}
				text2 += text4;
				ObjectID primaryIngredientFromVariation = CookedFoodCD.GetPrimaryIngredientFromVariation(containedObject.variation);
				primaryIngredientFromVariation = GetAnyObjectIDReplaceForNameAndDesc(primaryIngredientFromVariation);
				if (!API.Authoring.ObjectProperties.TryGetPropertyString(primaryIngredientFromVariation, "name", out var value2))
				{
					value2 = primaryIngredientFromVariation.ToString();
				}
				ObjectID secondaryIngredientFromVariation = CookedFoodCD.GetSecondaryIngredientFromVariation(containedObject.variation);
				secondaryIngredientFromVariation = GetAnyObjectIDReplaceForNameAndDesc(secondaryIngredientFromVariation);
				if (!API.Authoring.ObjectProperties.TryGetPropertyString(secondaryIngredientFromVariation, "name", out var value3))
				{
					value3 = secondaryIngredientFromVariation.ToString();
				}
				if (value2.EndsWith("Rare"))
				{
					value2 = value2.Remove(value2.Length - 4, 4);
				}
				if (value3.EndsWith("Rare"))
				{
					value3 = value3.Remove(value3.Length - 4, 4);
				}
				if (text3.EndsWith("Rare") || text3.EndsWith("Epic"))
				{
					text3 = text3.Remove(text3.Length - 4, 4);
				}
				array[num] = LocalizationManager.GetTranslation("FoodAdjectives/" + value3 + text4);
				num++;
				array[num] = LocalizationManager.GetTranslation("FoodNouns/" + value2);
				num++;
				array[num] = LocalizationManager.GetTranslation("Items/" + text3);
				textAndFormatFields.text = "foodFormat";
				textAndFormatFields.formatFields = array;
				if (localize)
				{
					textAndFormatFields.text = PugText.ProcessText("foodFormat", array, shouldLocalize: true, shouldLocalizeFormatFields: false);
					textAndFormatFields.formatFields = null;
				}
			}
			else
			{
				textAndFormatFields.text = (localize ? LocalizationManager.GetTranslation("Items/" + text3) : ("Items/" + text3));
			}
			if (flag)
			{
				if (!localize)
				{
					textAndFormatFields.text = PugText.ProcessText(textAndFormatFields.text, textAndFormatFields.formatFields, shouldLocalize: true, shouldLocalizeFormatFields: false);
				}
				textAndFormatFields.formatFields = new string[2]
				{
					LocalizationManager.GetTranslation(text2),
					textAndFormatFields.text
				};
				textAndFormatFields.text = "rareItemFormat";
				if (localize)
				{
					textAndFormatFields.text = PugText.ProcessText(textAndFormatFields.text, textAndFormatFields.formatFields, shouldLocalize: true, shouldLocalizeFormatFields: false);
					textAndFormatFields.formatFields = null;
				}
			}
		}
		return textAndFormatFields;
	}

	public bool IsAtRequiredObject(ObjectID objectID)
	{
		bool flag = false;
		CollisionWorld collisionWorld = PhysicsManager.GetCollisionWorld();
		NativeList<DistanceHit> outHits = new NativeList<DistanceHit>(8, Allocator.Temp);
		if (collisionWorld.OverlapSphere(base.WorldPosition, 1f, ref outHits, PlayerControllerBurstableStatics.requiredObjectFilter))
		{
			foreach (DistanceHit item in outHits)
			{
				Entity entity = item.Entity;
				if (EntityUtility.HasComponentData<ObjectDataCD>(entity, base.world) && EntityUtility.GetComponentData<ObjectDataCD>(entity, base.world).objectID == objectID)
				{
					flag = true;
					break;
				}
			}
		}
		outHits.Dispose();
		if (flag)
		{
			return true;
		}
		return false;
	}

	public static bool IsAtRequiredObject(ObjectID objectID, in LocalTransform localTransform, in CollisionWorld collisionWorld, ComponentLookup<ObjectDataCD> objectDataLookup)
	{
		NativeList<DistanceHit> outHits = new NativeList<DistanceHit>(8, Allocator.Temp);
		if (!collisionWorld.OverlapSphere(localTransform.Position, 1f, ref outHits, PlayerControllerBurstableStatics.requiredObjectFilter))
		{
			outHits.Dispose();
			return false;
		}
		bool result = false;
		foreach (DistanceHit item in outHits)
		{
			Entity entity = item.Entity;
			if (objectDataLookup.TryGetComponent(entity, out var componentData) && componentData.objectID == objectID)
			{
				result = true;
				break;
			}
		}
		outHits.Dispose();
		return result;
	}

	public static ObjectID GetAnyObjectIDReplaceForNameAndDesc(ObjectID objectID)
	{
		return objectID switch
		{
			ObjectID.GiantMushroom2 => ObjectID.GiantMushroom, 
			ObjectID.AmberLarva2 => ObjectID.AmberLarva, 
			_ => objectID, 
		};
	}

	public bool ShouldShowManaBar()
	{
		if (_checkedManaTime >= Time.time)
		{
			return _checkedShouldShowManaLastValue;
		}
		_checkedManaTime = Time.time;
		_checkedShouldShowManaLastValue = PugDatabase.HasComponent<ConsumesManaCD>(GetEquippedSlot().objectData.objectID) || GetMaxMana() != 100 || EntityUtility.GetConditionValue(ConditionID.IncreasedManaRegen, base.entity, base.world) != 0 || EntityUtility.GetConditionValue(ConditionID.IncreasedManaRegenEffectiveness, base.entity, base.world) != 0;
		return _checkedShouldShowManaLastValue;
	}

	private void UpdateGearCustomization()
	{
		bool flag = false;
		ObjectDataCD itemObjectData = vanitySlotsHandler.helmVanitySlotInventoryHandler.GetObjectData(0);
		bool flag2 = itemObjectData.amount < 0 && itemObjectData.objectID == ObjectID.None;
		if (itemObjectData.objectID == ObjectID.None)
		{
			itemObjectData = equipmentHandler.helmInventoryHandler.GetObjectData(0);
		}
		byte b = 0;
		if (!flag2 && PugDatabase.HasComponent<EquipmentSkinCD>(itemObjectData))
		{
			b = (byte)((!ItemIsBroken(itemObjectData)) ? PugDatabase.GetComponent<EquipmentSkinCD>(itemObjectData).skin : 0);
		}
		else if (flag2)
		{
			b = 0;
		}
		if (activeCustomization.helm != b)
		{
			SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.HELM, b);
			flag = true;
		}
		ObjectDataCD itemObjectData2 = vanitySlotsHandler.breastVanitySlotInventoryHandler.GetObjectData(0);
		bool flag3 = itemObjectData2.amount < 0 && itemObjectData2.objectID == ObjectID.None;
		if (itemObjectData2.objectID == ObjectID.None)
		{
			itemObjectData2 = equipmentHandler.breastInventoryHandler.GetObjectData(0);
		}
		byte b2 = 0;
		if (!flag3 && PugDatabase.HasComponent<EquipmentSkinCD>(itemObjectData2))
		{
			b2 = (byte)((!ItemIsBroken(itemObjectData2)) ? PugDatabase.GetComponent<EquipmentSkinCD>(itemObjectData2).skin : 0);
		}
		else if (flag3)
		{
			b2 = 0;
		}
		if (activeCustomization.breastArmor != b2)
		{
			SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.BREAST_ARMOR, b2);
			flag = true;
		}
		ObjectDataCD itemObjectData3 = vanitySlotsHandler.pantsVanitySlotInventoryHandler.GetObjectData(0);
		bool flag4 = itemObjectData3.amount < 0 && itemObjectData3.objectID == ObjectID.None;
		if (itemObjectData3.objectID == ObjectID.None)
		{
			itemObjectData3 = equipmentHandler.pantsInventoryHandler.GetObjectData(0);
		}
		byte b3 = 0;
		if (!flag4 && PugDatabase.HasComponent<EquipmentSkinCD>(itemObjectData3))
		{
			b3 = (byte)((!ItemIsBroken(itemObjectData3)) ? PugDatabase.GetComponent<EquipmentSkinCD>(itemObjectData3).skin : 0);
		}
		else if (flag4)
		{
			b3 = 0;
		}
		if (activeCustomization.pantsArmor != b3)
		{
			SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_ARMOR, b3);
			flag = true;
		}
		if (isLocal && flag)
		{
			Manager.saves.SetCharacterCustomization(activeCustomization);
			Manager.ui.characterWindow.UpdateCharacterCustomization();
			Manager.ui.vanityUI.UpdateCharacterCustomization();
		}
	}

	public void UpdateAndSendNewCustomization()
	{
		PlayerCustomization characterCustomization = Manager.saves.GetCharacterCustomization();
		activeCustomization = characterCustomization;
		RefreshCustomization();
		activeCustomizationTriggerCount++;
		playerCommandSystem.UpdatePlayerCustomization(base.entity, activeCustomization);
	}

	public void UpdateAnyNewCustomization()
	{
		PlayerCustomizationCD componentData = EntityUtility.GetComponentData<PlayerCustomizationCD>(base.entity, base.world);
		if (componentData.triggerCount != activeCustomizationTriggerCount)
		{
			activeCustomization = componentData.customization;
			activeCustomizationTriggerCount = componentData.triggerCount;
			RefreshCustomization();
		}
	}

	private void InitCustomization()
	{
		activeCustomizationTriggerCount = 0;
		PlayerCustomization customization = EntityUtility.GetComponentData<PlayerCustomizationCD>(base.entity, base.world).customization;
		activeCustomization = customization;
		RefreshCustomization();
	}

	public void SetRandomCustomization()
	{
		activeCustomization.gender = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.GENDER, 0));
		activeCustomization.skinColor = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.SKIN_COLOR, 0));
		activeCustomization.hair = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.HAIR, 0));
		activeCustomization.hairColor = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.HAIR_COLOR, 0));
		activeCustomization.eyes = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.EYES, 0));
		activeCustomization.eyesColor = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.EYES_COLOR, 0));
		activeCustomization.shirtSkin = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT, 0));
		activeCustomization.shirtColor = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR, 0));
		activeCustomization.pantsSkin = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.PANTS, 0));
		activeCustomization.pantsColor = (byte)UnityEngine.Random.Range(0, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_COLOR, activeCustomization.pantsSkin));
		RefreshCustomization();
	}

	public void RefreshCustomization()
	{
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.GENDER, activeCustomization.gender);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.SKIN_COLOR, activeCustomization.skinColor);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.HAIR, activeCustomization.hair);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.HAIR_COLOR, activeCustomization.hairColor);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.EYES, activeCustomization.eyes);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.EYES_COLOR, activeCustomization.eyesColor);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT, activeCustomization.shirtSkin);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR, activeCustomization.shirtColor);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.PANTS, activeCustomization.pantsSkin);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_COLOR, activeCustomization.pantsColor);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.HELM, activeCustomization.helm);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.BREAST_ARMOR, activeCustomization.breastArmor);
		SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_ARMOR, activeCustomization.pantsArmor);
	}

	private void UpdateName()
	{
		if (unfilteredPlayerName == null || !activeCustomization.name.Equals(unfilteredPlayerName))
		{
			unfilteredPlayerName = activeCustomization.name.Value;
			playerName = "";
			Manager.platform.parentalControlManager.RestrictInput(unfilteredPlayerName, delegate(string filteredName)
			{
				playerName = filteredName;
			});
		}
		bool flag = !isLocal && Manager.prefs.showCharacterNames;
		if (flag && pvpMode)
		{
			flag = IsPlayersOfSamePvPTeam(Manager.main.player);
		}
		if (flag)
		{
			nameText.gameObject.SetActive(value: true);
			if (_prevName != playerName)
			{
				_prevName = playerName;
				nameText.Render(playerName);
			}
		}
		else
		{
			nameText.gameObject.SetActive(value: false);
		}
	}

	public void SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType bodyPart, byte index)
	{
		int skinId = bodyPart switch
		{
			PlayerCustomizationTable.CustomizableBodyPartType.PANTS_COLOR => activeCustomization.pantsSkin, 
			PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR => activeCustomization.shirtSkin, 
			_ => 0, 
		};
		int y = customizationTable.GetMaxVariations(bodyPart, skinId) - 1;
		index = (byte)math.min(index, y);
		switch (bodyPart)
		{
		case PlayerCustomizationTable.CustomizableBodyPartType.GENDER:
		{
			activeCustomization.gender = index;
			byte b = (byte)math.min(activeCustomization.shirtSkin, customizationTable.GetMaxVariations(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT, 0) - 1);
			shirtSkin.SetSkin(customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT, b, HelmHairType.FullyShow, index));
			shirtColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR, b));
			activeCustomization.eyes = index;
			eyesSkin.SetSkin(customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.EYES, index));
			bodySkin.SetSkin(customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.BODY, index));
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.SKIN_COLOR:
			skinColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.SKIN_COLOR, index));
			activeCustomization.skinColor = index;
			skinColorReplacer.SetActiveColorReplacement(index);
			hairShadeColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.HAIR_SHADE_COLOR, index));
			hairShadeColorReplacer.SetActiveColorReplacement(index);
			break;
		case PlayerCustomizationTable.CustomizableBodyPartType.HAIR:
		{
			activeCustomization.hair = index;
			HelmHairType helmHairType = customizationTable.GetHelmHairType(activeCustomization.helm);
			Texture2D skinTexture4 = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.HAIR, index, helmHairType);
			hairSkin.gameObject.SetActive(skinTexture4 != null);
			hairShadeSkin.gameObject.SetActive(skinTexture4 != null);
			if (skinTexture4 != null)
			{
				hairSkin.SetSkin(skinTexture4);
				Texture2D skinTexture5 = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.HAIR_SHADE, index);
				hairShadeSkin.gameObject.SetActive(skinTexture5 != null);
				if (skinTexture5 != null)
				{
					hairShadeSkin.SetSkin(skinTexture5);
				}
			}
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.HAIR_COLOR:
			hairColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.HAIR_COLOR, index));
			activeCustomization.hairColor = index;
			hairColorReplacer.SetActiveColorReplacement(index);
			break;
		case PlayerCustomizationTable.CustomizableBodyPartType.EYES:
			activeCustomization.eyes = index;
			eyesSkin.SetSkin(customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.EYES, index));
			break;
		case PlayerCustomizationTable.CustomizableBodyPartType.EYES_COLOR:
			eyesColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.EYES_COLOR, index));
			activeCustomization.eyesColor = index;
			eyesColorReplacer.SetActiveColorReplacement(index);
			break;
		case PlayerCustomizationTable.CustomizableBodyPartType.SHIRT:
		{
			activeCustomization.shirtSkin = index;
			ShirtVisibility shirtVisibility = customizationTable.GetShirtVisibility(activeCustomization.breastArmor);
			Texture2D skinTexture7 = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT, index, HelmHairType.FullyShow, activeCustomization.gender, shirtVisibility);
			shirtSkin.SetSkin(skinTexture7);
			shirtSkin.gameObject.SetActive(skinTexture7 != null);
			if (skinTexture7 != null)
			{
				shirtSkin.SetSkin(skinTexture7);
			}
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR:
			activeCustomization.shirtColor = index;
			shirtColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR, activeCustomization.shirtSkin));
			shirtColorReplacer.SetActiveColorReplacement(index);
			break;
		case PlayerCustomizationTable.CustomizableBodyPartType.PANTS:
		{
			activeCustomization.pantsSkin = index;
			PantsVisibility pantsVisibility = customizationTable.GetPantsVisibility(activeCustomization.pantsArmor);
			Texture2D skinTexture6 = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.PANTS, index, HelmHairType.FullyShow, 0, ShirtVisibility.FullyShow, pantsVisibility);
			pantsSkin.SetSkin(skinTexture6);
			pantsSkin.gameObject.SetActive(skinTexture6 != null);
			if (skinTexture6 != null)
			{
				pantsSkin.SetSkin(skinTexture6);
			}
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.PANTS_COLOR:
			activeCustomization.pantsColor = index;
			pantsColorReplacer.SetNewColorReplacementData(customizationTable.GetColorReplacementData(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_COLOR, activeCustomization.pantsSkin));
			pantsColorReplacer.SetActiveColorReplacement(index);
			break;
		case PlayerCustomizationTable.CustomizableBodyPartType.HELM:
		{
			activeCustomization.helm = index;
			Texture2D skinTexture3 = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.HELM, index);
			helmSkin.SetSkin(skinTexture3);
			Texture2D emissiveTexture3 = customizationTable.GetEmissiveTexture(PlayerCustomizationTable.CustomizableBodyPartType.HELM, index);
			SetSpriteRendererEmissive(helmSR, emissiveTexture3);
			helmSkin.gameObject.SetActive(skinTexture3 != null);
			if (!defaultHelmOffsetInitialized)
			{
				defaultHelmOffset = helmSR.transform.localPosition;
				defaultHelmOffsetInitialized = true;
			}
			helmSR.transform.localPosition = defaultHelmOffset + customizationTable.GetPixelOffset(PlayerCustomizationTable.CustomizableBodyPartType.HELM, index);
			SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.HAIR, activeCustomization.hair);
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.BREAST_ARMOR:
		{
			activeCustomization.breastArmor = index;
			Texture2D skinTexture2 = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.BREAST_ARMOR, index);
			breastArmorSkin.SetSkin(skinTexture2);
			Texture2D emissiveTexture2 = customizationTable.GetEmissiveTexture(PlayerCustomizationTable.CustomizableBodyPartType.BREAST_ARMOR, index);
			SetSpriteRendererEmissive(breastArmorSR, emissiveTexture2);
			breastArmorSkin.gameObject.SetActive(skinTexture2 != null);
			SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.SHIRT, activeCustomization.shirtSkin);
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.PANTS_ARMOR:
		{
			activeCustomization.pantsArmor = index;
			Texture2D skinTexture = customizationTable.GetSkinTexture(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_ARMOR, index);
			pantsArmorSkin.SetSkin(skinTexture);
			Texture2D emissiveTexture = customizationTable.GetEmissiveTexture(PlayerCustomizationTable.CustomizableBodyPartType.PANTS_ARMOR, index);
			SetSpriteRendererEmissive(pantsArmorSR, emissiveTexture);
			pantsArmorSkin.gameObject.SetActive(skinTexture != null);
			SetCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType.PANTS, activeCustomization.pantsSkin);
			break;
		}
		case PlayerCustomizationTable.CustomizableBodyPartType.HAIR_SHADE:
		case PlayerCustomizationTable.CustomizableBodyPartType.HAIR_SHADE_COLOR:
			break;
		}
	}

	public int GetActiveCustomizableBodypart(PlayerCustomizationTable.CustomizableBodyPartType bodyPart)
	{
		return bodyPart switch
		{
			PlayerCustomizationTable.CustomizableBodyPartType.GENDER => activeCustomization.gender, 
			PlayerCustomizationTable.CustomizableBodyPartType.SKIN_COLOR => activeCustomization.skinColor, 
			PlayerCustomizationTable.CustomizableBodyPartType.HAIR => activeCustomization.hair, 
			PlayerCustomizationTable.CustomizableBodyPartType.HAIR_COLOR => activeCustomization.hairColor, 
			PlayerCustomizationTable.CustomizableBodyPartType.EYES => activeCustomization.eyes, 
			PlayerCustomizationTable.CustomizableBodyPartType.EYES_COLOR => activeCustomization.eyesColor, 
			PlayerCustomizationTable.CustomizableBodyPartType.SHIRT => activeCustomization.shirtSkin, 
			PlayerCustomizationTable.CustomizableBodyPartType.SHIRT_COLOR => activeCustomization.shirtColor, 
			PlayerCustomizationTable.CustomizableBodyPartType.PANTS => activeCustomization.pantsSkin, 
			PlayerCustomizationTable.CustomizableBodyPartType.PANTS_COLOR => activeCustomization.pantsColor, 
			PlayerCustomizationTable.CustomizableBodyPartType.HELM => activeCustomization.helm, 
			PlayerCustomizationTable.CustomizableBodyPartType.BREAST_ARMOR => activeCustomization.breastArmor, 
			PlayerCustomizationTable.CustomizableBodyPartType.PANTS_ARMOR => activeCustomization.pantsArmor, 
			_ => 0, 
		};
	}

	public void SpawnSkillIncreasePopup(SkillID skillID, bool playAudio)
	{
		Vector3 position = (Manager.ui.isAnyInventoryShowing ? (base.RenderPosition + Vector3.down * 1.15f + Vector3.back * 0.5f) : (base.RenderPosition + Vector3.up * 0.7f));
		CombatText.SpawnCombatText("SkillIncrease/" + skillID, CombatText.NumberColor.White, position, isDamageNumber: false, isCrit: false, localize: true, new string[1] { "1" });
		if (playAudio)
		{
			AudioManager.Sfx(SfxID.skillPointHighTwinkle1, center, 0.4f, 1f, 0.1f);
		}
	}

	public void SpawnNewSkillPopup(SkillID skillID)
	{
		Manager.ui.chatWindow.AddInfoText(new string[1] { LocalizationManager.GetTranslation("Skills/" + skillID) }, ChatWindow.MessageTextType.NewTalentPointAvailable);
		gainTalentEffect.Play(withChildren: true);
		AudioManager.Sfx(SfxID.successTone, center, 0.4f, 1f, 0.1f);
	}

	public static void AddSkill(Entity entity, SkillID skillID, int amount, EntityCommandBuffer ecb, bool isServer)
	{
		if (isServer)
		{
			Entity e = ecb.CreateEntity();
			ecb.AddComponent(e, new AddSkillValueCD
			{
				entity = entity,
				skillID = skillID,
				amount = amount
			});
		}
	}

	public static void IncreasePetXp(Entity playerEntity, int xpIncrease, in PetOwnerCD petOwnerCD, in PlayerGhost playerGhost, in DynamicBuffer<ContainedObjectsBuffer> containedObjectsBuffer, BufferLookup<InventoryChangeBuffer> inventoryChangeBufferLookup, Entity inventoryChangeBufferEntity)
	{
		if (petOwnerCD.SlotIndex < containedObjectsBuffer.Length)
		{
			ContainedObjectsBuffer containedObjectsBuffer2 = containedObjectsBuffer[petOwnerCD.SlotIndex];
			if (containedObjectsBuffer2.objectID != 0 && !PetExtensions.IsAtMaxLevel(containedObjectsBuffer2.amount))
			{
				inventoryChangeBufferLookup[inventoryChangeBufferEntity].Add(new InventoryChangeBuffer
				{
					inventoryChangeData = Create.AddAmount(playerEntity, petOwnerCD.SlotIndex, containedObjectsBuffer2.objectID, xpIncrease),
					playerEntity = playerEntity
				});
			}
		}
	}

	public void SetSkillLevel(SkillID skillID, int level)
	{
		int skillFromLevel = SkillExtensions.GetSkillFromLevel(skillID, level);
		playerCommandSystem.SetSkillValue(base.entity, skillID, skillFromLevel);
	}

	public void MaxOutAllSkills()
	{
		for (int i = 0; i < 12; i++)
		{
			SkillID skillID = (SkillID)i;
			int maxSkillLevel = SkillExtensions.GetMaxSkillLevel(skillID);
			SetSkillLevel(skillID, maxSkillLevel);
		}
	}

	public void SetAllSkills(int level)
	{
		for (int i = 0; i < 12; i++)
		{
			SkillID skillID = (SkillID)i;
			SetSkillLevel(skillID, level);
		}
	}

	public void ResetAllSkills()
	{
		SetAllSkills(0);
	}

	public void CollectSoul(SoulID soulID)
	{
		if (!Manager.saves.HasCollectedSoul(soulID))
		{
			Manager.saves.CollectSoul(soulID);
			playerCommandSystem.CollectSoul(base.entity, soulID);
		}
	}

	public void UnlockSouls()
	{
		if (!Manager.saves.HasUnlockedSouls())
		{
			Manager.saves.UnlockSouls();
			playerCommandSystem.UnlockSouls(base.entity);
		}
	}

	public Vector3 GetLeashPoint()
	{
		Vector3 vector = facingDirection.vec3;
		if (!isLocal)
		{
			vector = GetAnimOrientationVec3();
		}
		return leashPoints.GetLeashPoint(vector);
	}

	public void TriggerOutroForAllPlayers()
	{
		PlayOutroClientSystem existingSystemManaged = Manager.ecs.ClientWorld.GetExistingSystemManaged<PlayOutroClientSystem>();
		if (existingSystemManaged == null)
		{
			Debug.LogError("No PlayOutroClientSystem system in client world!");
		}
		else
		{
			existingSystemManaged.TriggerOutroForAllPlayers();
		}
	}

	public void PlayOutro()
	{
		FadeOutAndLockPlayer();
		Manager.load.QueueScene("Outro", 1f, 0.5f, FadePresets.blackToBlack, setFadeValueTo1: false, 1);
	}

	private void FadeOutAndLockPlayer()
	{
		Manager.ui.HideAllInventoryAndCraftingUI();
		Manager.saves.PlayedOutro();
		Manager.menu.PopAllMenus();
		Manager.music.FadeOutVolume(1.4f);
		SetInvincibility(value: true);
		inputModule.DisableInputFor();
	}

	public bool IsPlayersOfSamePvPTeam(PlayerController otherPlayer)
	{
		if (otherPlayer == null)
		{
			return false;
		}
		if (EntityUtility.TryGetComponentData<FactionCD>(base.entity, base.world, out var value) && EntityUtility.TryGetComponentData<FactionCD>(otherPlayer.entity, otherPlayer.world, out var value2))
		{
			return value.pvpTeam == value2.pvpTeam;
		}
		return false;
	}

	public void StartSpawningFromCoreRoutine()
	{
		StartCoroutine(SpawningFromCoreCoroutine());
	}

	private IEnumerator SpawningFromCoreCoroutine()
	{
		XScaler.gameObject.SetActive(value: false);
		shadow.gameObject.SetActive(value: false);
		facingDirection = Direction.Id.back;
		yield return new WaitForSeconds(2f);
		AudioManager.SfxFollowTransform(SfxID.Bell, base.transform, 0.8f);
		Manager.camera.ShakeCameraNow(1f);
		AudioManager.SfxFollowTransform(SfxID.EarthquakeSpawn, base.transform, 0.5f);
		Manager.rgb.StartState(RGBManager.State.SpawnFromCore_Rumble);
		yield return new WaitForSeconds(0.9f);
		int i = 0;
		while (i < 3)
		{
			Manager.camera.ShakeCameraNow(0.5f, 2f, 2f);
			yield return new WaitForSeconds(0.4f);
			int num = i + 1;
			i = num;
		}
		i = 0;
		while (i < 20)
		{
			Manager.camera.ShakeCameraNow(0.25f, 3f, 3f);
			yield return new WaitForSeconds(0.2f);
			int num = i + 1;
			i = num;
		}
		i = 0;
		while (i < 5)
		{
			Manager.camera.ShakeCameraNow(0.25f, 2f, 2f);
			yield return new WaitForSeconds(0.25f);
			int num = i + 1;
			i = num;
		}
		Manager.rgb.EndState(RGBManager.State.SpawnFromCore_Rumble);
		Manager.rgb.TriggerEvent(RGBManager.Event.SpawnFromCore_Spawn);
		SpawnEffect freeComponent = Manager.memory.GetFreeComponent<SpawnEffect>(deferOnOccupied: true);
		if (freeComponent != null)
		{
			freeComponent.transform.position = base.transform.position + new Vector3(0f, 5f, -5f);
			freeComponent.OnOccupied();
		}
		else
		{
			Debug.LogError("failed to instantiate player spawn effect");
		}
		AudioManager.SfxFollowTransform(SfxID.darkgleam, base.transform);
		XScaler.gameObject.SetActive(value: true);
		shadow.gameObject.SetActive(value: true);
		flashableComponent.FlashLinearNoCurve(1f);
		yield return new WaitForSeconds(1.7f);
		facingDirection = Direction.Id.right;
		yield return new WaitForSeconds(0.6f);
		facingDirection = Direction.Id.left;
		yield return new WaitForSeconds(0.8f);
		facingDirection = Direction.Id.back;
		yield return new WaitForSeconds(0.5f);
		Emote.SpawnEmoteText(base.transform.position + new Vector3(0f, 1f, 0f), Emote.EmoteType.QuestionMark, randomizePosition: false, shortBounce: true);
		AudioManager.SfxUI(SfxID.twitch, 0.8f, reuse: true, 0.1f);
		spawningFromCoreFinished = true;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawSphere(base.transform.TransformPoint(leashPoints.behind), 0.05f);
		Gizmos.DrawSphere(base.transform.TransformPoint(leashPoints.infront), 0.05f);
		Gizmos.DrawSphere(base.transform.TransformPoint(leashPoints.left), 0.05f);
		Gizmos.DrawSphere(base.transform.TransformPoint(leashPoints.right), 0.05f);
	}

	public static bool IsDyingOrDead(PlayerStateCD playerStateCD)
	{
		return playerStateCD.HasAnyState(PlayerStateEnum.Death | PlayerStateEnum.Teleporting);
	}

	public void SetInvincibility(bool value)
	{
		playerCommandSystem.SetPlayerImmuneToDamage(base.entity, value);
	}

	private static bool TryDealSledgeDamage(bool heldItemIsBroken, in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, float2 hitPos, float2 hitSize, int damage, int tileDamage, bool isWoundup, float windupMult, bool shouldKnockback = false)
	{
		int2 @int = (int2)math.round(hitSize / 2f);
		int2 int2 = (int2)math.round(playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position.ToFloat2() + hitPos);
		playerAttackLookups.summarizeConiditionsLookup.TryGetBuffer(playerAttackAspect.entity, out var bufferData);
		playerAttackLookups.summarizeConiditionsEffectsLookup.TryGetBuffer(playerAttackAspect.entity, out var bufferData2);
		using NativeArray<SummarizedConditionsBuffer> conditionsAtHit = bufferData.ToNativeArray(Allocator.Temp);
		using NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit = bufferData2.ToNativeArray(Allocator.Temp);
		Entity equipmentPrefab = playerAttackAspect.equippedObjectCD.ValueRO.equipmentPrefab;
		ref readonly EquipmentSlotCD valueRO = ref playerAttackAspect.equipmentSlotCD.ValueRO;
		AnimationOrientationCD animationOrientationCD = playerAttackLookups.animationOrientationLookup[playerAttackAspect.entity];
		PhysicsCollider hitCollider = EquipmentSlot.GetHitCollider(equipmentPrefab, in valueRO, in animationOrientationCD, in playerAttackShared.colliderCache, in playerAttackLookups.meleeWeaponLookup, in playerAttackShared.worldInfo, heldItemIsBroken, windupMult);
		bool hitAnyEnemy;
		bool hitAnyWall;
		bool flag = TryDealDamageToEntities(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, heldItemIsBroken, conditionsAtHit, conditionEffectsAtHit, hitCollider, damage, isRanged: false, isMagic: false, canOnlyDamageDestructibles: false, out hitAnyEnemy, out hitAnyWall, isWoundup, windupMult, shouldKnockback);
		CollisionFilter collisionFilter = default(CollisionFilter);
		collisionFilter.BelongsTo = uint.MaxValue;
		collisionFilter.CollidesWith = 131905u;
		CollisionFilter filter = collisionFilter;
		bool flag2 = false;
		bool flag3 = false;
		int num = 0;
		int num2 = 4;
		Entity equipmentPrefab2 = playerAttackAspect.equippedObjectCD.ValueRO.equipmentPrefab;
		for (int i = -num2; i <= num2; i++)
		{
			for (int j = -num2; j <= num2; j++)
			{
				int2 int3 = new int2(i, j) + int2;
				if (math.abs(int3.x - int2.x) > @int.x || math.abs(int3.y - int2.y) > @int.y)
				{
					continue;
				}
				PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
				playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
				NativeList<ColliderCastHit> outHits = new NativeList<ColliderCastHit>(Allocator.Temp);
				collWorld.SphereCastAll(int3.ToFloat3(), 0.1f, float3.zero, 0f, ref outHits, filter);
				NativeArray<TileCD> nativeArray = playerAttackShared.tileAccessor.Get(int3, Allocator.Temp);
				for (int k = 0; k < nativeArray.Length; k++)
				{
					TileCD tileCD = nativeArray[k];
					if (tileCD.tileType == TileType.ground || !tileCD.tileType.IsDamageableTile())
					{
						continue;
					}
					Entity tileEntity = PugDatabase.GetPrimaryPrefabEntity(PugDatabase.GetObjectID(tileCD.tileset, tileCD.tileType, playerAttackShared.databaseBank.databaseBankBlob), playerAttackShared.databaseBank.databaseBankBlob);
					for (int l = 0; l < outHits.Length; l++)
					{
						Entity entity = outHits[l].Entity;
						if (playerAttackLookups.tileLookup.TryGetComponent(entity, out var componentData) && playerAttackLookups.healthLookup.HasComponent(entity) && componentData.tileType == tileCD.tileType && componentData.tileset == tileCD.tileset)
						{
							tileEntity = entity;
							break;
						}
					}
					GetTileDamageValues(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, tileEntity, int3, conditionsAtHit, conditionEffectsAtHit, tileDamage, out var normHealth, out var damageDone, out var damageDoneBeforeReduction);
					DynamicBuffer<TileDamageBuffer> tileDamageBuffer = playerAttackLookups.tileDamageBufferLookup[playerAttackShared.tileDamageBufferSingleton];
					ClientSystem.CreateTileDamage(playerAttackAspect.entity, tileDamageBuffer, int3, damageDoneBeforeReduction, in playerAttackShared.worldInfo, playerAttackAspect.entity, canDamageGround: false, pullAnyLootToPlayer: true);
					DoImmediateTileDamageEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, tileCD.tileset, tileCD.tileType, int3.ToInt3(), normHealth, damageDone, tileEntity, isRanged: false, isBeam: false, isExplosive: false, skipPushingPlayer: true);
					if (tileCD.tileset != 2 && tileCD.tileType != TileType.greatWall && tileCD.tileType == TileType.wall)
					{
						if ((float)damageDone >= 12f)
						{
							flag2 = true;
						}
						hitAnyWall = true;
						if (damageDone > 0)
						{
							flag3 = true;
						}
						if (normHealth <= 0f)
						{
							DestroyAnyObjectsHangingAtWallPosition(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, int3.ToFloat3());
						}
					}
					if (ShouldReceiveDurabilityFromDestroyingOre(normHealth, in playerAttackShared, int3))
					{
						num++;
					}
					flag = true;
					break;
				}
				nativeArray.Dispose();
				outHits.Dispose();
			}
		}
		if (flag2)
		{
			AddSkill(playerAttackAspect.entity, SkillID.Mining, 1, playerAttackShared.ecb, playerAttackShared.isServer);
		}
		if (flag3)
		{
			OnMiningWallBlock(in playerAttackAspect, in playerAttackShared, in playerAttackLookups);
		}
		if (num > 0)
		{
			playerAttackLookups.increaseDurabilityOfEquippedLookup.SetComponentEnabled(playerAttackAspect.entity, value: true);
			playerAttackLookups.increaseDurabilityOfEquippedLookup.GetRefRW(playerAttackAspect.entity).ValueRW.triggerCounter += num;
		}
		if (playerAttackLookups.durabilityLookup.HasComponent(equipmentPrefab2) && (hitAnyWall || flag))
		{
			playerAttackLookups.reduceDurabilityOfEquippedLookup.SetComponentEnabled(playerAttackAspect.entity, value: true);
			playerAttackLookups.reduceDurabilityOfEquippedLookup.GetRefRW(playerAttackAspect.entity).ValueRW.triggerCounter++;
		}
		return flag;
	}

	public static bool TryMeleeAttack(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, bool heldItemIsBroken, int damage, bool isWoundup, float windupMult)
	{
		ref readonly EquippedObjectCD valueRO = ref playerAttackAspect.equippedObjectCD.ValueRO;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool shouldKnockback = false;
		bool isMagic = false;
		Entity equipmentPrefab = playerAttackAspect.equippedObjectCD.ValueRO.equipmentPrefab;
		if (!heldItemIsBroken)
		{
			if (valueRO.containedObject.objectID != 0)
			{
				ref PugDatabase.EntityObjectInfo entityObjectInfo = ref PugDatabase.GetEntityObjectInfo(valueRO.containedObject.objectID, playerAttackShared.databaseBank.databaseBankBlob, valueRO.containedObject.variation);
				flag = entityObjectInfo.objectType == ObjectType.MiningPick;
				flag2 = entityObjectInfo.objectType == ObjectType.Sledge;
				flag3 = entityObjectInfo.objectType == ObjectType.DrillTool;
				flag4 = playerAttackLookups.hasWeaponDamageLookup.HasComponent(equipmentPrefab);
				isMagic = playerAttackLookups.hasWeaponDamageLookup.TryGetComponent(equipmentPrefab, out var componentData) && componentData.isMagic;
			}
			if (playerAttackLookups.windupLookup.TryGetComponent(equipmentPrefab, out var componentData2) && windupMult > 1.2f)
			{
				shouldKnockback = componentData2.knockback;
			}
		}
		AnimationOrientationCD animationOrientationCD = playerAttackLookups.animationOrientationLookup[playerAttackAspect.entity];
		if (flag2)
		{
			float2 hitPos = EquipmentSlot.GetCenterOfHitCollider(equipmentPrefab, in playerAttackAspect.equipmentSlotCD.ValueRO, in animationOrientationCD, in playerAttackLookups.meleeWeaponLookup, heldItemIsBroken, windupMult).ToFloat2();
			float2 hitSize = EquipmentSlot.GetSizeOfHitCollider(equipmentPrefab, in playerAttackAspect.equipmentSlotCD.ValueRO, in animationOrientationCD, in playerAttackLookups.meleeWeaponLookup, heldItemIsBroken, windupMult).ToFloat2();
			return TryDealSledgeDamage(heldItemIsBroken, in playerAttackAspect, in playerAttackShared, in playerAttackLookups, hitPos, hitSize, damage, damage, isWoundup, windupMult, shouldKnockback);
		}
		playerAttackLookups.summarizeConiditionsLookup.TryGetBuffer(playerAttackAspect.entity, out var bufferData);
		using NativeArray<SummarizedConditionsBuffer> conditionsAtHit = bufferData.ToNativeArray(Allocator.Temp);
		playerAttackLookups.summarizeConiditionsEffectsLookup.TryGetBuffer(playerAttackAspect.entity, out var bufferData2);
		using NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit = bufferData2.ToNativeArray(Allocator.Temp);
		PhysicsCollider hitCollider = EquipmentSlot.GetHitCollider(equipmentPrefab, in playerAttackAspect.equipmentSlotCD.ValueRO, in animationOrientationCD, in playerAttackShared.colliderCache, in playerAttackLookups.meleeWeaponLookup, in playerAttackShared.worldInfo, heldItemIsBroken, windupMult);
		bool hitAnyEnemy;
		bool hitAnyWall;
		bool result = TryDealDamageToEntities(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, heldItemIsBroken, conditionsAtHit, conditionEffectsAtHit, hitCollider, damage, isRanged: false, isMagic, canOnlyDamageDestructibles: false, out hitAnyEnemy, out hitAnyWall, isWoundup, windupMult, shouldKnockback);
		if (playerAttackLookups.durabilityLookup.HasComponent(equipmentPrefab) && ((hitAnyEnemy && flag4) || (hitAnyWall && (flag || flag3))))
		{
			playerAttackLookups.reduceDurabilityOfEquippedLookup.SetComponentEnabled(playerAttackAspect.entity, value: true);
			playerAttackLookups.reduceDurabilityOfEquippedLookup.GetRefRW(playerAttackAspect.entity).ValueRW.triggerCounter++;
		}
		return result;
	}

	public static bool TryRayCastAttack(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, bool heldItemIsBroken, int damage)
	{
		bool flag = false;
		ref readonly PlayerAimPositionCD valueRO = ref playerAttackAspect.playerAimPosition.ValueRO;
		AnimationOrientationCD animationOrientationCD = playerAttackLookups.animationOrientationLookup[playerAttackAspect.entity];
		PlayerStateCD playerStateCD = playerAttackLookups.playerStateLookup[playerAttackAspect.entity];
		LocalTransform localTransform = playerAttackLookups.localTransformLookup[playerAttackAspect.entity];
		if (!GetBeamPoints(in valueRO, in animationOrientationCD, in playerStateCD, in localTransform, out var _, out var toWorldPos))
		{
			return false;
		}
		PhysicsCollider sphereCollider = PhysicsManager.GetSphereCollider(float3.zero, 0.3f, 27u, playerAttackShared.colliderCache);
		playerAttackLookups.summarizeConiditionsLookup.TryGetBuffer(playerAttackAspect.entity, out var bufferData);
		using (NativeArray<SummarizedConditionsBuffer> conditionsAtHit = bufferData.ToNativeArray(Allocator.Temp))
		{
			playerAttackLookups.summarizeConiditionsEffectsLookup.TryGetBuffer(playerAttackAspect.entity, out var bufferData2);
			using NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit = bufferData2.ToNativeArray(Allocator.Temp);
			flag = TryDealDamageAtCollider(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, heldItemIsBroken, toWorldPos, conditionsAtHit, conditionEffectsAtHit, sphereCollider, damage, isRanged: false, isMagic: false, isGoKart: false, out var _, out var _, isWoundup: false, 1f, shouldKnockback: false, isBeam: true);
			Entity equipmentPrefab = playerAttackAspect.equippedObjectCD.ValueRO.equipmentPrefab;
			if (playerAttackLookups.durabilityLookup.HasComponent(equipmentPrefab) && flag)
			{
				playerAttackLookups.reduceDurabilityOfEquippedLookup.SetComponentEnabled(playerAttackAspect.entity, value: true);
				playerAttackLookups.reduceDurabilityOfEquippedLookup.GetRefRW(playerAttackAspect.entity).ValueRW.triggerCounter++;
			}
		}
		return flag;
	}

	public static bool TryGoKartAttack(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups)
	{
		NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsValuesArray = EntityUtility.GetConditionEffectsValuesArray(playerAttackAspect.entity, playerAttackLookups.summarizeConiditionsEffectsLookup);
		PhysicsCollider sphereCollider = PhysicsManager.GetSphereCollider(float3.zero, 0.4f, 1u, playerAttackShared.colliderCache);
		float3 pos = playerAttackLookups.localTransformLookup.GetRefRO(playerAttackAspect.entity).ValueRO.Position + math.normalizesafe(playerAttackAspect.playerMovementCD.ValueRO.targetMovementVelocity).ToFloat3() * 0.7f;
		bool hitAnyEnemy;
		bool hitAnyWall;
		return TryDealDamageAtCollider(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, heldItemIsBroken: false, pos, default(NativeArray<SummarizedConditionsBuffer>), conditionEffectsValuesArray, sphereCollider, 10, isRanged: false, isMagic: false, isGoKart: true, out hitAnyEnemy, out hitAnyWall);
	}

	private static bool TryDealDamageToEntities(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, bool heldItemIsBroken, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, PhysicsCollider hitCollider, int damage, bool isRanged, bool isMagic, bool canOnlyDamageDestructibles, out bool hitAnyEnemy, out bool hitAnyWall, bool isWoundup = false, float windupMult = 0f, bool shouldKnockback = false)
	{
		return TryDealDamageAtCollider(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, heldItemIsBroken, playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position, conditionsAtHit, conditionEffectsAtHit, hitCollider, damage, isRanged, isMagic, canOnlyDamageDestructibles, out hitAnyEnemy, out hitAnyWall, isWoundup, windupMult, shouldKnockback);
	}

	private static bool TryDealDamageAtCollider(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, bool heldItemIsBroken, float3 pos, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, PhysicsCollider hitCollider, int damage, bool isRanged, bool isMagic, bool isGoKart, out bool hitAnyEnemy, out bool hitAnyWall, bool isWoundup = false, float windupMult = 1f, bool shouldKnockback = false, bool isBeam = false)
	{
		bool isBeam2 = isBeam;
		return TryDealDamageToAnyEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, heldItemIsBroken, pos, conditionsAtHit, conditionEffectsAtHit, hitCollider, 0f, damage, isRanged, isMagic, isGoKart, out hitAnyEnemy, out hitAnyWall, canDamageTile: true, 1, isExplosive: false, isWoundup, windupMult, shouldKnockback, default(Vector3), 0f, isBeam2);
	}

	private static bool TryDealDamageToAnyEntity(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, bool heldItemIsBroken, float3 pos, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, PhysicsCollider hitCollider, float radius, int damage, bool isRanged, bool isMagic, bool isGoKart, out bool hitAnyEnemy, out bool hitAnyWall, bool canDamageTile = true, int skillMultiplier = 1, bool isExplosive = false, bool isWoundup = false, float windupMult = 1f, bool shouldKnockback = false, Vector3 projectileDirection = default(Vector3), float projectileDistance = 0f, bool isBeam = false)
	{
		NativeList<Entity> hitEntities = new NativeList<Entity>(10, Allocator.Temp);
		NativeList<Entity> entitiesToIgnore = new NativeList<Entity>(10, Allocator.Temp);
		CollisionFilter filter;
		NativeList<CastResult> colliderHits = DoHitCollisionCheck(in playerAttackAspect, in playerAttackShared, playerAttackLookups, pos, radius, projectileDirection, projectileDistance, hitCollider, isRanged, isBeam, out filter);
		ObjectType equippedObjectType = GetEquippedObjectType(in playerAttackAspect.equippedObjectCD.ValueRO, in playerAttackShared.databaseBank, heldItemIsBroken, isRanged, isExplosive);
		int playerHealthChange;
		bool flag = TryDamageAnyEnemies(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, colliderHits, hitEntities, isGoKart, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, isBeam, isExplosive, isWoundup, windupMult, shouldKnockback, projectileDirection, out playerHealthChange, out var _, out var playerManaChange, entitiesToIgnore, out hitAnyEnemy, out var enemyBlocksHittingObjects, equippedObjectType, skillMultiplier);
		bool canOnlyHitCertainObjects = !isExplosive && ((flag && enemyBlocksHittingObjects) || equippedObjectType == ObjectType.MeleeWeapon);
		NativeList<Entity> entitiesHitByExplosives = new NativeList<Entity>(Allocator.Temp);
		NativeList<float3> nativeList = new NativeList<float3>(Allocator.Temp);
		float3 @float = ((isBeam || isGoKart) ? pos : CalculateTargetHitPosition(in playerAttackAspect, in playerAttackShared, in playerAttackLookups));
		ObjectToDamageECSInfo otdi = FigureOutWhichObjectToDamage(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, pos, colliderHits, @float, entitiesToIgnore, canOnlyHitCertainObjects, equippedObjectType, isRanged, isGoKart, canDamageTile, hitEntities, entitiesHitByExplosives, nativeList, radius, projectileDirection, projectileDistance, filter);
		flag |= DealDamageToObject(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi, @float, isRanged, isMagic, hitEntities, conditionsAtHit, conditionEffectsAtHit, damage, isGoKart, isExplosive, isBeam, equippedObjectType, canOnlyHitCertainObjects, out var playerHealthChange2, out var playerManaChange2, out hitAnyWall, entitiesHitByExplosives, nativeList);
		playerHealthChange += playerHealthChange2;
		playerManaChange2 += playerManaChange2;
		ApplyPlayerHealthChange(in playerAttackAspect, in playerAttackLookups, playerAttackShared, playerHealthChange);
		ApplyPlayerManaChange(in playerAttackAspect.entity, in playerAttackLookups.manaLookup, playerManaChange);
		colliderHits.Dispose();
		entitiesHitByExplosives.Dispose();
		nativeList.Dispose();
		return flag;
	}

	private static NativeList<CastResult> DoHitCollisionCheck(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, PlayerAttackLookups playerAttackLookups, Vector3 pos, float radius, Vector3 projectileDirection, float projectileDistance, PhysicsCollider hitCollider, bool isRanged, bool isBeam, out CollisionFilter filter)
	{
		filter = new CollisionFilter
		{
			BelongsTo = uint.MaxValue,
			CollidesWith = (9u | ((!(isRanged || isBeam)) ? 64u : 0u) | ((!isRanged) ? 131328u : 0u))
		};
		if (playerAttackShared.worldInfo.pvpEnabled)
		{
			filter.CollidesWith |= 6u;
		}
		PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
		playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
		CollisionWorld collisionWorld = physicsWorld.CollisionWorld;
		ComponentLookup<UseLagCompensationCD> useLagCompensationLookup = playerAttackLookups.useLagCompensationLookup;
		NativeList<CastResult> nativeList = new NativeList<CastResult>(Allocator.Temp);
		if (hitCollider.IsValid)
		{
			NativeList<ColliderCastHit> allHits = new NativeList<ColliderCastHit>(Allocator.Temp);
			collWorld.CastCollider(PhysicsManager.GetColliderCastInput(pos, pos, hitCollider), ref allHits);
			foreach (ColliderCastHit item in allHits)
			{
				if (!(item.Entity == playerAttackAspect.entity) && useLagCompensationLookup.HasComponent(item.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = new DistanceHit
						{
							Entity = item.Entity,
							Position = item.Position
						},
						entityPositionAtHit = collWorld.Bodies[item.RigidBodyIndex].WorldFromBody.pos
					};
					nativeList.Add(in value);
				}
			}
			allHits.Clear();
			collisionWorld.CastCollider(PhysicsManager.GetColliderCastInput(pos, pos, hitCollider), ref allHits);
			foreach (ColliderCastHit item2 in allHits)
			{
				if (!(item2.Entity == playerAttackAspect.entity) && !useLagCompensationLookup.HasComponent(item2.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = new DistanceHit
						{
							Entity = item2.Entity,
							Position = item2.Position
						},
						entityPositionAtHit = collisionWorld.Bodies[item2.RigidBodyIndex].WorldFromBody.pos
					};
					nativeList.Add(in value);
				}
			}
			allHits.Dispose();
		}
		else if (isRanged)
		{
			NativeList<ColliderCastHit> outHits = new NativeList<ColliderCastHit>(Allocator.Temp);
			collWorld.SphereCastAll(pos, radius, projectileDirection, projectileDistance, ref outHits, filter);
			foreach (ColliderCastHit item3 in outHits)
			{
				if (useLagCompensationLookup.HasComponent(item3.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = new DistanceHit
						{
							Entity = item3.Entity,
							Position = item3.Position
						},
						entityPositionAtHit = collWorld.Bodies[item3.RigidBodyIndex].WorldFromBody.pos
					};
					nativeList.Add(in value);
				}
			}
			outHits.Clear();
			collisionWorld.SphereCastAll(pos, radius, projectileDirection, projectileDistance, ref outHits, filter);
			foreach (ColliderCastHit item4 in outHits)
			{
				if (!useLagCompensationLookup.HasComponent(item4.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = new DistanceHit
						{
							Entity = item4.Entity,
							Position = item4.Position
						},
						entityPositionAtHit = collisionWorld.Bodies[item4.RigidBodyIndex].WorldFromBody.pos
					};
					nativeList.Add(in value);
				}
			}
			outHits.Dispose();
		}
		else
		{
			NativeList<DistanceHit> outHits2 = new NativeList<DistanceHit>(Allocator.Temp);
			collWorld.OverlapSphere(pos, radius, ref outHits2, filter);
			foreach (DistanceHit item5 in outHits2)
			{
				if (useLagCompensationLookup.HasComponent(item5.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = item5,
						entityPositionAtHit = collWorld.Bodies[item5.RigidBodyIndex].WorldFromBody.pos
					};
					nativeList.Add(in value);
				}
			}
			outHits2.Clear();
			collisionWorld.OverlapSphere(pos, radius, ref outHits2, filter);
			foreach (DistanceHit item6 in outHits2)
			{
				if (!useLagCompensationLookup.HasComponent(item6.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = item6,
						entityPositionAtHit = collisionWorld.Bodies[item6.RigidBodyIndex].WorldFromBody.pos
					};
					nativeList.Add(in value);
				}
			}
			outHits2.Dispose();
		}
		NativeList<GhostIDWithIndex> list = new NativeList<GhostIDWithIndex>(nativeList.Length, Allocator.Temp);
		for (int i = 0; i < nativeList.Length; i++)
		{
			playerAttackLookups.ghostInstanceLookup.TryGetComponent(nativeList[i].distanceHit.Entity, out var componentData);
			GhostIDWithIndex value2 = new GhostIDWithIndex
			{
				ghostID = componentData.ghostId,
				index = i
			};
			list.Add(in value2);
		}
		list.Sort();
		NativeList<CastResult> result = new NativeList<CastResult>(nativeList.Length, Allocator.Temp);
		for (int j = 0; j < list.Length; j++)
		{
			CastResult value = nativeList[list[j].index];
			result.Add(in value);
		}
		list.Dispose();
		nativeList.Dispose();
		return result;
	}

	private static ObjectType GetEquippedObjectType(in EquippedObjectCD equippedObjectCD, in PugDatabase.DatabaseBankCD databaseBankCD, bool heldItemIsBroken, bool isRanged, bool isExplosive)
	{
		ObjectType result = ObjectType.NonUsable;
		if (!isRanged && !isExplosive && equippedObjectCD.containedObject.objectID != 0 && !heldItemIsBroken)
		{
			result = PugDatabase.GetEntityObjectInfo(equippedObjectCD.containedObject.objectID, databaseBankCD.databaseBankBlob, equippedObjectCD.containedObject.variation).objectType;
		}
		return result;
	}

	private static void Deflect(PlayerAttackLookups playerAttackLookups, PlayerAttackShared playerAttackShared, PlayerAttackAspect playerAttackAspect, NativeList<DistanceHit> colliderHits)
	{
		foreach (DistanceHit item in colliderHits)
		{
			Entity entity = item.Entity;
			if (playerAttackLookups.projectileLookup.HasComponent(entity) && (!playerAttackLookups.entityDestroyedLookup.HasComponent(entity) || !playerAttackLookups.entityDestroyedLookup.IsComponentEnabled(entity)) && playerAttackLookups.simulateLookup.IsComponentEnabled(entity))
			{
				playerAttackLookups.healthLookup[entity] = new HealthCD
				{
					health = 0,
					maxHealth = 1
				};
				if (playerAttackShared.isFirstTimeFullyPredictingTick)
				{
					RefRW<RandomCD> refRW = playerAttackLookups.randomLookup.GetRefRW(playerAttackAspect.entity);
					ClientSystem.SpawnProjectile(in playerAttackShared, in playerAttackLookups, ObjectID.DeflectProjectile, playerAttackLookups.localTransformLookup[entity].Position, playerAttackAspect.clientInput.ValueRO.targetingDirection.ToFloat3(), playerAttackAspect.entity, 100, weaponIsReinforced: false, ref refRW.ValueRW.Value, 0, controlledByPlayer: false);
				}
			}
		}
	}

	private static bool TryDamageAnyEnemies(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, NativeList<CastResult> colliderHits, NativeList<Entity> hitEntities, bool isGoKart, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, int damage, bool isRanged, bool isMagic, bool isBeam, bool isExplosive, bool isWoundup, float windupMult, bool shouldKnockback, Vector3 projectileDirection, out int playerHealthChange, out int ownerHealthChange, out int playerManaChange, NativeList<Entity> entitiesToIgnore, out bool hitAnyEnemy, out bool enemyBlocksHittingObjects, ObjectType equippedObjectType, int skillMultiplier)
	{
		bool flag = false;
		bool flag2 = false;
		playerHealthChange = 0;
		ownerHealthChange = 0;
		playerManaChange = 0;
		hitAnyEnemy = false;
		enemyBlocksHittingObjects = true;
		bool flag3 = false;
		using NativeArray<PlayerPreviouslyHitEnemiesBuffer> array = playerAttackAspect.previouslyHitEnemiesBuffer.ToNativeArray(Allocator.Temp);
		playerAttackAspect.previouslyHitEnemiesBuffer.Clear();
		for (int num = colliderHits.Length - 1; num >= 0; num--)
		{
			Entity entity = colliderHits[num].distanceHit.Entity;
			if (playerAttackLookups.immuneToDamageLookup.TryGetComponent(entity, out var componentData) && componentData.Value == ImmuneToDamageState.Immune)
			{
				CastResult value = colliderHits[num];
				colliderHits.Add(in value);
				colliderHits.RemoveAt(num);
			}
		}
		foreach (CastResult item in colliderHits)
		{
			if (isGoKart)
			{
				continue;
			}
			DistanceHit distanceHit = item.distanceHit;
			bool shouldShowHitFeedbackOnHitEntityPart = false;
			bool shouldHandleImmuneToDamageOnEntityPart = false;
			Entity hitEntityPart = Entity.Null;
			Entity value2 = distanceHit.Entity;
			if (playerAttackLookups.entityPartLookup.TryGetComponent(value2, out var componentData2))
			{
				shouldShowHitFeedbackOnHitEntityPart = componentData2.showHitFeedbackOnThisPart;
				shouldHandleImmuneToDamageOnEntityPart = componentData2.handleImmuneToDamageOnThisPart;
				hitEntityPart = value2;
				Entity mainEntity = componentData2.mainEntity;
				if (!(mainEntity != Entity.Null))
				{
					continue;
				}
				value2 = mainEntity;
			}
			if (hitEntities.Contains(value2) || entitiesToIgnore.Contains(value2) || !AttemptToDealDamageToEnemy(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, value2, item.entityPositionAtHit, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, isBeam, isExplosive, isWoundup, windupMult, shouldKnockback, projectileDirection, shouldShowHitFeedbackOnHitEntityPart, shouldHandleImmuneToDamageOnEntityPart, hitEntityPart, out var attackerHealthChange, out ownerHealthChange, out playerManaChange))
			{
				continue;
			}
			playerHealthChange = math.max(playerHealthChange, attackerHealthChange);
			hitEntities.Add(in value2);
			flag = true;
			hitAnyEnemy = true;
			if (!flag2 && !isExplosive)
			{
				FactionCD componentData3;
				FactionID factionID = (playerAttackLookups.factionLookup.TryGetComponent(value2, out componentData3) ? componentData3.faction : FactionID.None);
				flag2 = factionID != 0 && factionID != FactionID.Merchant;
			}
			if (enemyBlocksHittingObjects)
			{
				if (equippedObjectType == ObjectType.MiningPick || equippedObjectType == ObjectType.Sledge || equippedObjectType == ObjectType.DrillTool || equippedObjectType == ObjectType.BeamWeapon)
				{
					enemyBlocksHittingObjects = !playerAttackLookups.dontBlockPlayerFromHittingObjectsWhenMiningPickEquippedLookup.HasComponent(value2);
				}
				if (playerAttackLookups.cattleLookup.HasComponent(value2))
				{
					enemyBlocksHittingObjects = false;
				}
			}
			if (!flag3 && array.Contains(distanceHit.Entity))
			{
				flag3 = true;
			}
			playerAttackAspect.previouslyHitEnemiesBuffer.Add(distanceHit.Entity);
		}
		if (flag2)
		{
			SkillID skillID = (isMagic ? SkillID.Magic : (isRanged ? SkillID.Range : SkillID.Melee));
			AddSkill(playerAttackAspect.entity, skillID, skillMultiplier, playerAttackShared.ecb, playerAttackShared.isServer);
		}
		ApplyConditions(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, flag, flag3, isRanged, isExplosive);
		return flag;
	}

	private static void ApplyConditions(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, bool successHittingSomething, bool hitSameEnemyAsPreviousHit, bool isRanged, bool isExplosive)
	{
		ref PlayerAttackCD valueRW = ref playerAttackAspect.playerAttackCD.ValueRW;
		ConditionsTableCD conditionsTableCD = playerAttackShared.conditionsTableCD;
		DynamicBuffer<SummarizedConditionsBuffer> dynamicBuffer = playerAttackLookups.summarizeConiditionsLookup[playerAttackAspect.entity];
		if (hitSameEnemyAsPreviousHit)
		{
			valueRW.hitStreak++;
			if (valueRW.hitStreak >= 2)
			{
				int value = dynamicBuffer[94].value;
				if (value > 0)
				{
					ConditionData conditionData = default(ConditionData);
					conditionData.conditionID = ConditionID.MeleeDamageIncreaseFromHittingSameTarget;
					conditionData.value = value;
					conditionData.duration = 3f;
					EntityUtility.AddOrRefreshCondition(conditionData, playerAttackLookups.conditionsBufferLookup[playerAttackAspect.entity], conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, playerAttackLookups.summarizeConiditionsLookup[playerAttackAspect.entity]);
				}
			}
		}
		else
		{
			valueRW.hitStreak = 0;
		}
		if (!successHittingSomething)
		{
			return;
		}
		DynamicBuffer<ConditionsBuffer> conditionsBuffer = playerAttackLookups.conditionsBufferLookup[playerAttackAspect.entity];
		DynamicBuffer<SummarizedConditionsBuffer> summarizedConditionsBuffer = playerAttackLookups.summarizeConiditionsLookup[playerAttackAspect.entity];
		ref Unity.Mathematics.Random value2 = ref playerAttackLookups.randomLookup.GetRefRW(playerAttackAspect.entity).ValueRW.Value;
		if (!isRanged && !isExplosive)
		{
			int value3 = dynamicBuffer[82].value;
			if (value3 > 0)
			{
				int value4 = dynamicBuffer[83].value;
				ConditionData conditionData = default(ConditionData);
				conditionData.conditionID = ConditionID.MeleeDamageIncreaseFromHitting;
				conditionData.value = math.min(value4 + 20, value3 * 20);
				conditionData.duration = 8f;
				EntityUtility.AddOrRefreshConditionOverrideStacks(conditionData, conditionsBuffer, conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate);
			}
			float num = (float)dynamicBuffer[86].value / 100f;
			if (dynamicBuffer[87].value <= 0 && value2.NextFloat() < num)
			{
				ConditionData conditionData = default(ConditionData);
				conditionData.conditionID = ConditionID.MeleeAttackSpeedFromChanceOnHit;
				conditionData.value = 500;
				conditionData.duration = 2f;
				EntityUtility.AddOrRefreshCondition(conditionData, conditionsBuffer, conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, summarizedConditionsBuffer);
			}
			float num2 = (float)dynamicBuffer[90].value / 100f;
			if (value2.NextFloat() < num2)
			{
				ConditionData conditionData = default(ConditionData);
				conditionData.conditionID = ConditionID.IncreasedRangeDamageFromMeleeHit;
				conditionData.value = 300;
				conditionData.duration = 10f;
				EntityUtility.AddOrRefreshCondition(conditionData, conditionsBuffer, conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, summarizedConditionsBuffer);
			}
			float num3 = (float)dynamicBuffer[97].value / 100f;
			if (num3 > 0f && value2.NextFloat() <= 0.1f)
			{
				int maxHealthWithConditions = playerAttackLookups.healthLookup[playerAttackAspect.entity].GetMaxHealthWithConditions(playerAttackLookups.summarizeConiditionsEffectsLookup[playerAttackAspect.entity]);
				HealPlayer(in playerAttackShared, in playerAttackAspect, in playerAttackLookups, math.max(1, (int)math.round((float)maxHealthWithConditions * num3)));
			}
		}
		else if (isRanged)
		{
			if (dynamicBuffer[84].value > 0)
			{
				ConditionData conditionData = default(ConditionData);
				conditionData.conditionID = ConditionID.RangeDamageIncreaseFromShooting;
				conditionData.value = 20;
				conditionData.duration = 8f;
				EntityUtility.AddOrRefreshCondition(conditionData, conditionsBuffer, conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, summarizedConditionsBuffer);
			}
			float num4 = (float)dynamicBuffer[88].value / 100f;
			if (value2.NextFloat() < num4)
			{
				ConditionData conditionData = default(ConditionData);
				conditionData.conditionID = ConditionID.CriticalHitChanceFromShot;
				conditionData.value = 100;
				conditionData.duration = 3f;
				EntityUtility.AddOrRefreshCondition(conditionData, conditionsBuffer, conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, summarizedConditionsBuffer);
			}
			float num5 = (float)dynamicBuffer[92].value / 100f;
			if (value2.NextFloat() < num5)
			{
				ConditionData conditionData = default(ConditionData);
				conditionData.conditionID = ConditionID.IncreasedMeleeDamageFromShot;
				conditionData.value = 300;
				conditionData.duration = 10f;
				EntityUtility.AddOrRefreshCondition(conditionData, conditionsBuffer, conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, summarizedConditionsBuffer);
			}
		}
	}

	private static float3 CalculateTargetHitPosition(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups)
	{
		float3 position = playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position;
		Direction direction = playerAttackLookups.animationOrientationLookup[playerAttackAspect.entity].facingDirection;
		float3 result = position + direction.f3 * 0.6f;
		float3 @float = float3.zero;
		ClientInput valueRO = playerAttackAspect.clientInput.ValueRO;
		if (valueRO.prefersKeyboardAndMouse)
		{
			@float = valueRO.mouseOrJoystickWorldPoint.ToFloat3() - position;
		}
		else if (valueRO.wasAiming)
		{
			@float = valueRO.aimDirection.ToFloat3();
		}
		if (math.any(@float != float3.zero) && Vector3.Angle(@float, direction.vec3) < 110f)
		{
			@float = Vector3.ClampMagnitude(@float, 0.95f);
			result = position + @float;
		}
		return result;
	}

	private static ObjectToDamageECSInfo FigureOutWhichObjectToDamage(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, Vector3 pos, NativeList<CastResult> colliderHits, Vector3 targetHitPosition, NativeList<Entity> entitiesToIgnore, bool canOnlyHitCertainObjects, ObjectType equippedObjectType, bool isRanged, bool isGoKart, bool canDamageTile, NativeList<Entity> hitEntities, NativeList<Entity> entitiesHitByExplosives, NativeList<float3> entitiesHitByExplosivesPositions, float radius, Vector3 projectileDirection, float projectileDistance, CollisionFilter filter)
	{
		ObjectToDamageECSInfo @default = ObjectToDamageECSInfo.Default;
		PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
		playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
		CollisionWorld collisionWorld = physicsWorld.CollisionWorld;
		ComponentLookup<UseLagCompensationCD> useLagCompensationLookup = playerAttackLookups.useLagCompensationLookup;
		if (isRanged)
		{
			colliderHits.Clear();
			NativeList<ColliderCastHit> outHits = new NativeList<ColliderCastHit>(Allocator.Temp);
			collWorld.SphereCastAll(pos, radius * 0.1f, projectileDirection, projectileDistance, ref outHits, filter);
			foreach (ColliderCastHit item in outHits)
			{
				if (useLagCompensationLookup.HasComponent(item.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = new DistanceHit
						{
							Entity = item.Entity,
							Position = item.Position
						},
						entityPositionAtHit = collWorld.Bodies[item.RigidBodyIndex].WorldFromBody.pos
					};
					colliderHits.Add(in value);
				}
			}
			outHits.Clear();
			collisionWorld.SphereCastAll(pos, radius * 0.1f, projectileDirection, projectileDistance, ref outHits, filter);
			foreach (ColliderCastHit item2 in outHits)
			{
				if (!useLagCompensationLookup.HasComponent(item2.Entity))
				{
					CastResult value = new CastResult
					{
						distanceHit = new DistanceHit
						{
							Entity = item2.Entity,
							Position = item2.Position
						},
						entityPositionAtHit = collisionWorld.Bodies[item2.RigidBodyIndex].WorldFromBody.pos
					};
					colliderHits.Add(in value);
				}
			}
			outHits.Dispose();
		}
		foreach (CastResult item3 in colliderHits)
		{
			CastResult current3 = item3;
			DistanceHit distanceHit = current3.distanceHit;
			if (!playerAttackLookups.localTransformLookup.HasComponent(distanceHit.Entity) || playerAttackLookups.nonHittableLookup.HasComponent(distanceHit.Entity) || playerAttackLookups.enemyLookup.HasComponent(distanceHit.Entity) || EntityIsControlledByOtherEntity(distanceHit.Entity, in playerAttackLookups) || entitiesToIgnore.Contains(@default.closestHitEntity))
			{
				continue;
			}
			if (canOnlyHitCertainObjects)
			{
				bool damagedByMiningTool = equippedObjectType == ObjectType.MiningPick || equippedObjectType == ObjectType.Sledge || equippedObjectType == ObjectType.DrillTool || equippedObjectType == ObjectType.BeamWeapon;
				if (!EntityUtility.EvaluateCanOnlyHitCertainObjects(distanceHit.Entity, damagedByMiningTool, playerAttackLookups.tileLookup, playerAttackLookups.objectCategoryTagsLookup, playerAttackLookups.rootLookup, playerAttackLookups.destructibleObjectLookup, playerAttackLookups.mineableLookup, playerAttackLookups.playerGraveLookup))
				{
					continue;
				}
			}
			float3 @float = current3.entityPositionAtHit;
			if (playerAttackLookups.objectDataLookup.TryGetComponent(distanceHit.Entity, out var componentData) && componentData.objectID != 0)
			{
				ref PugDatabase.EntityObjectInfo entityObjectInfo = ref PugDatabase.GetEntityObjectInfo(componentData.objectID, playerAttackShared.databaseBank.databaseBankBlob, componentData.variation);
				float num = float.MaxValue;
				float3 float2 = @float;
				int2 prefabSize = EntityUtility.GetPrefabSize(distanceHit.Entity, ref entityObjectInfo, playerAttackLookups.directionLookup);
				if (prefabSize.x > 1 || prefabSize.y > 1)
				{
					int2 prefabOffset = EntityUtility.GetPrefabOffset(distanceHit.Entity, ref entityObjectInfo, playerAttackLookups.directionLookup);
					for (int i = prefabOffset.x; i < prefabSize.x + prefabOffset.x; i++)
					{
						for (int j = prefabOffset.y; j < prefabSize.y + prefabOffset.y; j++)
						{
							float3 float3 = @float + new float3(i, 0f, j);
							float magnitude = ((Vector3)float3 - targetHitPosition).magnitude;
							if (magnitude < num)
							{
								num = magnitude;
								float2 = float3;
							}
						}
					}
				}
				@float = float2;
			}
			playerAttackLookups.objectPropertiesLookup.TryGetComponent(distanceHit.Entity, out var componentData2);
			bool flag = EntityIsDamageableTileCollider(distanceHit.Entity, in playerAttackLookups);
			bool flag2 = EntityIsDamageableTile(distanceHit.Entity, in playerAttackLookups) && !flag;
			bool flag3 = EntityIsDamageablePseudoTile(distanceHit.Entity, playerAttackLookups);
			bool flag4 = false;
			if (flag2 || flag3 || flag)
			{
				flag4 = EntityIsWalkableTile(distanceHit.Entity, flag3, playerAttackLookups);
			}
			bool flag5 = componentData2.IsValid && componentData2.Has(-975748197);
			bool flag6 = EntityIsDamageableObject(distanceHit.Entity, in playerAttackLookups);
			bool flag7 = EntityIsDestructibleObject(distanceHit.Entity, in playerAttackLookups);
			bool flag8 = EntityIsIndestructible(distanceHit.Entity, in playerAttackLookups);
			bool flag9 = EntityIsImmuneToDamage(distanceHit.Entity, in playerAttackShared, in playerAttackLookups);
			bool closestEntityIsGroundDecoration = EntityIsGroundDecoration(distanceHit.Entity, in playerAttackLookups);
			bool flag10 = EntityHasSurfacePriority(distanceHit.Entity, in playerAttackLookups);
			int surfacePriority = GetSurfacePriority(distanceHit.Entity, in playerAttackLookups);
			bool flag11 = EntityIsCritter(distanceHit.Entity, in playerAttackLookups);
			bool flag12 = componentData2.IsValid && componentData2.Has(-1171081164);
			if (isGoKart && !flag7 && !EntityIsRoot(distanceHit.Entity, in playerAttackLookups))
			{
				continue;
			}
			if ((equippedObjectType == ObjectType.MeleeWeapon || equippedObjectType == ObjectType.MiningPick || equippedObjectType == ObjectType.Sledge || equippedObjectType == ObjectType.DrillTool || equippedObjectType == ObjectType.BeamWeapon) && componentData.objectID == ObjectID.Torch)
			{
				CollisionFilter collisionFilter = default(CollisionFilter);
				collisionFilter.BelongsTo = uint.MaxValue;
				collisionFilter.CollidesWith = 24u;
				CollisionFilter filter2 = collisionFilter;
				if (playerAttackShared.worldInfo.pvpEnabled)
				{
					filter2.CollidesWith |= 6u;
				}
				NativeList<ColliderCastHit> outHits2 = new NativeList<ColliderCastHit>(Allocator.Temp);
				bool flag13 = true;
				if (collWorld.SphereCastAll(pos, 2f, float3.zero, 0f, ref outHits2, filter2))
				{
					foreach (ColliderCastHit item4 in outHits2)
					{
						if (!playerAttackLookups.petLookup.HasComponent(item4.Entity) && !playerAttackLookups.cattleLookup.HasComponent(item4.Entity) && item4.Entity != playerAttackAspect.entity)
						{
							flag13 = false;
							break;
						}
					}
				}
				outHits2.Dispose();
				if (!flag13)
				{
					continue;
				}
			}
			bool isBeamWeapon = equippedObjectType == ObjectType.BeamWeapon;
			if (!(flag2 || flag3 || flag5 || flag6 || flag7 || flag || flag8 || flag9) || hitEntities.Contains(distanceHit.Entity) || !EntityIsValidObjectToDamage(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, distanceHit.Entity, @float, canDamageTile, flag2 || flag3, flag, isBeamWeapon) || (isRanged && flag11))
			{
				continue;
			}
			if (!flag2 && !flag)
			{
				Entity value2 = distanceHit.Entity;
				entitiesHitByExplosives.Add(in value2);
				entitiesHitByExplosivesPositions.Add(in current3.entityPositionAtHit);
			}
			if (flag4)
			{
				float magnitude2 = ((Vector3)@float - targetHitPosition).magnitude;
				TileInfo anyTileInfo = GetAnyTileInfo(distanceHit.Entity, in playerAttackLookups);
				if (magnitude2 < @default.closestGroundDistance || @default.groundEntityTileInfo.tileType.GetSurfacePriorityFromJob() < anyTileInfo.tileType.GetSurfacePriorityFromJob())
				{
					@default.groundEntityPosition = @float;
					@default.closestGroundDistance = magnitude2;
					@default.closestHitGroundEntity = distanceHit.Entity;
					@default.groundEntityTileInfo = anyTileInfo;
				}
				continue;
			}
			bool flag14 = false;
			float magnitude3 = ((Vector3)@float - targetHitPosition).magnitude;
			if (flag12 && componentData.objectID != 0)
			{
				bool variationZeroIsNoDirection = componentData2.Has(-377237680);
				int num2 = playerAttackLookups.objectDataLookup[distanceHit.Entity].variation;
				Vector3 vector = @float - DirectionBasedOnVariationCD.GetDirectionFromVariation(num2, variationZeroIsNoDirection).ToFloat3() * 0.5f;
				@float = vector;
				magnitude3 = (vector - targetHitPosition).magnitude;
			}
			if (math.all(@default.hitPosition.RoundToInt2() == @float.RoundToInt2()))
			{
				flag14 = @default.closestEntityHasSurfacePriority && (!flag10 || surfacePriority > @default.closestEntitySurfacePrio);
			}
			bool flag15 = System.Math.Abs(magnitude3 - @default.closestDistance) < 0.001f;
			bool flag16 = magnitude3 < @default.closestDistance;
			if (flag14 || (!flag15 && flag16) || (flag15 && flag2 && @default.closestEntityIsTileCollider))
			{
				@default.closestEntityPosition = @float;
				@default.closestDistance = magnitude3;
				@default.closestHitEntity = distanceHit.Entity;
				@default.hitPosition = @float;
				@default.closestEntityIsTile = flag2;
				@default.closestEntityIsDestructible = flag7;
				@default.closestEntityIsImmune = flag9;
				@default.closestEntityIsIndestructible = flag8;
				@default.closestEntityIsTileCollider = flag;
				@default.closestEntityIsGroundDecoration = closestEntityIsGroundDecoration;
				@default.closestEntityHasSurfacePriority = flag10;
				@default.closestEntitySurfacePrio = surfacePriority;
			}
		}
		return @default;
	}

	private static bool DealDamageToObject(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, ObjectToDamageECSInfo otdi, float3 targetHitPosition, bool isRanged, bool isMagic, NativeList<Entity> hitEntities, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, int damage, bool isGoKart, bool isExplosive, bool isBeam, ObjectType equippedObjectType, bool canOnlyHitCertainObjects, out int playerHealthChange, out int playerManaChange, out bool hitAnyWall, NativeList<Entity> entitiesHitByExplosives, NativeList<float3> entitiesHitByExplosivePositions)
	{
		bool result = false;
		playerHealthChange = 0;
		playerManaChange = 0;
		hitAnyWall = false;
		int damageAfterReduction;
		int ownerHealthChange;
		int attackerManaChange;
		bool wasKilled;
		bool spawnScarabBossProjectile;
		bool spawnOctopusBossProjectile;
		bool spawnThunderBeam;
		bool shouldBeKnockedback;
		if (isRanged)
		{
			if (otdi.closestHitEntity != Entity.Null && !otdi.closestEntityIsTile && !otdi.closestEntityIsIndestructible && !otdi.closestEntityIsImmune)
			{
				hitEntities.Add(in otdi.closestHitEntity);
				ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitEntity, otdi.closestEntityPosition, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, otdi.closestEntityPosition, out playerHealthChange, out damageAfterReduction, out ownerHealthChange, out attackerManaChange, out wasKilled, out spawnScarabBossProjectile, out spawnOctopusBossProjectile, out spawnThunderBeam, out shouldBeKnockedback, showDamageNumber: true, isExplosive: false, isDigging: false, attackWoundup: false, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
				result = true;
				OnHarvest(playerAttackAspect.entity, ref playerAttackAspect.hungerCD.ValueRW, in playerAttackAspect.playerStateCD.ValueRO, playerAttackShared.ecb, playerAttackShared.isServer, otdi.closestHitEntity, wasInstantlyDestroyed: false, playerAttackLookups.plantLookup, playerAttackLookups.growingLookup, playerAttackLookups.objectDataLookup, playerAttackLookups.healthLookup, playerAttackLookups.summarizeConiditionsLookup, playerAttackShared.achievementArchetype, playerAttackLookups.objectPropertiesLookup);
			}
		}
		else if (isExplosive || equippedObjectType == ObjectType.Sledge)
		{
			for (int i = 0; i < entitiesHitByExplosives.Length; i++)
			{
				Entity value = entitiesHitByExplosives[i];
				float3 position = entitiesHitByExplosivePositions[i];
				hitEntities.Add(in value);
				LocalTransform componentData;
				if (!EntityIsIndestructible(value, in playerAttackLookups) && !EntityIsImmuneToDamage(value, in playerAttackShared, in playerAttackLookups))
				{
					ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, value, position, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, otdi.closestEntityPosition, out playerHealthChange, out attackerManaChange, out ownerHealthChange, out damageAfterReduction, out shouldBeKnockedback, out spawnThunderBeam, out spawnOctopusBossProjectile, out spawnScarabBossProjectile, out wasKilled, showDamageNumber: true, isExplosive, isDigging: false, attackWoundup: false, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
				}
				else if (equippedObjectType == ObjectType.Sledge && playerAttackLookups.localTransformLookup.TryGetComponent(value, out componentData))
				{
					float3 position2 = componentData.Position;
					PlayMineFailedEffect(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, position2, Emote.EmoteType.__illegal__, allowPushback: false, raycastToFindSparksPosition: false);
				}
				OnHarvest(playerAttackAspect.entity, ref playerAttackAspect.hungerCD.ValueRW, in playerAttackAspect.playerStateCD.ValueRO, playerAttackShared.ecb, playerAttackShared.isServer, otdi.closestHitEntity, wasInstantlyDestroyed: false, playerAttackLookups.plantLookup, playerAttackLookups.growingLookup, playerAttackLookups.objectDataLookup, playerAttackLookups.healthLookup, playerAttackLookups.summarizeConiditionsLookup, playerAttackShared.achievementArchetype, playerAttackLookups.objectPropertiesLookup);
				result = true;
			}
		}
		else
		{
			float3 position3 = playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position;
			int3 @int = targetHitPosition.RoundToInt3();
			GetAnyDamageableTile(@int, out var hitTile, in playerAttackShared);
			float num = math.length(@int - targetHitPosition);
			bool flag = math.all(@int == position3.RoundToInt3());
			if (!flag)
			{
				foreach (Entity playerEntity in playerAttackShared.playerEntities)
				{
					if (math.all(@int == playerAttackLookups.localTransformLookup[playerEntity].Position.RoundToInt3()))
					{
						flag = true;
						break;
					}
				}
			}
			bool flag2 = hitTile.tileType == TileType.bridge && flag;
			bool flag3 = false;
			int surfacePriorityFromJob = hitTile.tileType.GetSurfacePriorityFromJob();
			if (otdi.closestHitEntity != Entity.Null && playerAttackLookups.surfacePriorityLookup.TryGetComponent(otdi.closestHitEntity, out var componentData2))
			{
				flag3 = surfacePriorityFromJob > componentData2.Value;
			}
			bool flag4 = false;
			if (otdi.closestHitEntity != Entity.Null && playerAttackLookups.objectDataLookup.TryGetComponent(otdi.closestHitEntity, out var componentData3))
			{
				ref PugDatabase.EntityObjectInfo entityObjectInfo = ref PugDatabase.GetEntityObjectInfo(componentData3.objectID, playerAttackShared.databaseBank.databaseBankBlob, componentData3.variation);
				int2 prefabSize = EntityUtility.GetPrefabSize(otdi.closestHitEntity, ref entityObjectInfo, playerAttackLookups.directionLookup);
				int2 prefabOffset = EntityUtility.GetPrefabOffset(otdi.closestHitEntity, ref entityObjectInfo, playerAttackLookups.directionLookup);
				int3 int2 = otdi.closestEntityPosition.RoundToInt3();
				for (int j = prefabOffset.x; j < prefabOffset.x + prefabSize.x; j++)
				{
					for (int k = prefabOffset.y; k < prefabOffset.y + prefabSize.y; k++)
					{
						if (math.all(int2 + new int3(j, 0, k) == @int))
						{
							flag4 = true;
							break;
						}
					}
				}
			}
			bool flag5 = !isGoKart && !canOnlyHitCertainObjects && (flag3 || (!flag2 && (otdi.closestHitEntity == Entity.Null || (num < otdi.closestDistance && hitTile.tileType != 0 && hitTile.tileType != TileType.wall && !flag4))) || (hitTile.tileType == TileType.groundSlime && otdi.closestEntityIsGroundDecoration));
			if (otdi.closestHitEntity != Entity.Null && !flag5)
			{
				hitEntities.Add(in otdi.closestHitEntity);
				ObjectDataCD componentData4;
				if (otdi.closestEntityIsTile)
				{
					ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitEntity, otdi.closestEntityPosition, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, otdi.closestEntityPosition, out playerHealthChange, out damageAfterReduction, out ownerHealthChange, out attackerManaChange, out wasKilled, out spawnScarabBossProjectile, out spawnOctopusBossProjectile, out spawnThunderBeam, out shouldBeKnockedback, showDamageNumber: true, isExplosive: false, isDigging: false, attackWoundup: false, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
					TileCD tileInfo = playerAttackLookups.tileLookup[otdi.closestHitEntity];
					GetTileDamageValues(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitEntity, otdi.closestEntityPosition.RoundToInt2(), conditionsAtHit, conditionEffectsAtHit, damage, out var normHealth, out var damageDone, out var _);
					OnDamagingTile(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, tileInfo, damageDone, normHealth, otdi.closestEntityPosition, out hitAnyWall);
					if (IsTileSet(in playerAttackShared, otdi.closestEntityPosition.RoundToInt3(), tileInfo.tileset, tileInfo.tileType))
					{
						DoImmediateTileDamageEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, tileInfo.tileset, tileInfo.tileType, otdi.closestEntityPosition.RoundToInt3(), normHealth, damageDone, otdi.closestHitEntity, isRanged: false, isBeam);
					}
					result = true;
				}
				else if (otdi.closestEntityIsDestructible)
				{
					ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitEntity, otdi.closestEntityPosition, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, otdi.closestEntityPosition, out playerHealthChange, out attackerManaChange, out playerManaChange, out var damageAfterReduction2, out shouldBeKnockedback, out spawnThunderBeam, out spawnOctopusBossProjectile, out spawnScarabBossProjectile, out wasKilled, showDamageNumber: true, isExplosive: false, isDigging: false, attackWoundup: false, isGoKart, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
					result = true;
					if (damageAfterReduction2 <= 0 && !isGoKart)
					{
						PlayMineFailedEffect(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.hitPosition, GetDamageEmoteType(otdi.closestHitEntity, damageAfterReduction2, in playerAttackLookups, in playerAttackShared), !isBeam);
					}
				}
				else if (otdi.closestEntityIsIndestructible || otdi.closestEntityIsImmune)
				{
					Emote.EmoteType damageEmoteType = GetDamageEmoteType(otdi.closestHitEntity, 0, in playerAttackLookups, in playerAttackShared);
					PlayMineFailedEffect(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.hitPosition, damageEmoteType, !isRanged && !isExplosive && !isBeam);
					result = true;
				}
				else if (otdi.closestEntityIsTileCollider)
				{
					int3 pos = otdi.closestEntityPosition.RoundToInt3();
					GetAnyDamageableTile(pos, out var hitTile2, in playerAttackShared);
					if (hitTile2.tileType != 0 && !hitTile2.tileType.IsWalkableTile())
					{
						Entity primaryPrefabEntity = PugDatabase.GetPrimaryPrefabEntity(PugDatabase.GetObjectID(hitTile2.tileset, hitTile2.tileType, playerAttackShared.databaseBank.databaseBankBlob), playerAttackShared.databaseBank.databaseBankBlob);
						GetTileDamageValues(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, primaryPrefabEntity, otdi.closestEntityPosition.RoundToInt2(), conditionsAtHit, conditionEffectsAtHit, damage, out var normHealth2, out var damageDone2, out var damageDoneBeforeReduction2);
						OnDamagingTile(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, hitTile2, damageDone2, normHealth2, otdi.closestEntityPosition, out hitAnyWall);
						DynamicBuffer<TileDamageBuffer> tileDamageBuffer = playerAttackLookups.tileDamageBufferLookup[playerAttackShared.tileDamageBufferSingleton];
						ClientSystem.CreateTileDamage(playerAttackAspect.entity, tileDamageBuffer, new int2(pos.x, pos.z), damageDoneBeforeReduction2, in playerAttackShared.worldInfo, playerAttackAspect.entity, canDamageGround: false, pullAnyLootToPlayer: true);
						DoImmediateTileDamageEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, hitTile2.tileset, hitTile2.tileType, pos, normHealth2, damageDone2, primaryPrefabEntity, isRanged: false, isBeam);
						result = true;
					}
				}
				else if (playerAttackLookups.objectDataLookup.TryGetComponent(otdi.closestHitEntity, out componentData4) && componentData4.objectID == ObjectID.PlayerGrave && EntityMonoBehaviour.IsClaimedByPlayer(otdi.closestHitEntity, playerAttackAspect.entity, playerAttackLookups))
				{
					EntityUtility.PickupGrave(playerAttackLookups.inventoryChangeBufferLookup[playerAttackShared.inventoryChangeBufferEntity], ghostEffectEventBuffer: playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity], ghostEffectEventBufferPointerCD: ref playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity).ValueRW, graveEntity: otdi.closestHitEntity, playerEntity: playerAttackAspect.entity, healthLookup: playerAttackLookups.healthLookup, killedByPlayerLookup: playerAttackLookups.killedByPlayerLookup, currentTick: playerAttackShared.currentTick, chestOpenSfxID: playerAttackShared.chestOpenSfxID, ecb: playerAttackShared.ecb, playerGhost: in playerAttackAspect.playerGhost.ValueRO);
				}
				else
				{
					ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitEntity, otdi.closestEntityPosition, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, otdi.closestEntityPosition, out playerHealthChange, out attackerManaChange, out ownerHealthChange, out var damageAfterReduction3, out wasKilled, out spawnScarabBossProjectile, out spawnOctopusBossProjectile, out spawnThunderBeam, out shouldBeKnockedback, showDamageNumber: true, isExplosive: false, isDigging: false, attackWoundup: false, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
					if (damageAfterReduction3 <= 0 && playerAttackLookups.mineableLookup.TryGetComponent(otdi.closestHitEntity, out var componentData5) && componentData5.playFailedEffectOnZeroDamage)
					{
						PlayMineFailedEffect(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.hitPosition, GetDamageEmoteType(otdi.closestHitEntity, damageAfterReduction3, in playerAttackLookups, in playerAttackShared), !isBeam);
					}
					result = true;
					OnHarvest(playerAttackAspect.entity, ref playerAttackAspect.hungerCD.ValueRW, in playerAttackAspect.playerStateCD.ValueRO, playerAttackShared.ecb, playerAttackShared.isServer, otdi.closestHitEntity, wasInstantlyDestroyed: false, playerAttackLookups.plantLookup, playerAttackLookups.growingLookup, playerAttackLookups.objectDataLookup, playerAttackLookups.healthLookup, playerAttackLookups.summarizeConiditionsLookup, playerAttackShared.achievementArchetype, playerAttackLookups.objectPropertiesLookup);
				}
			}
			else if (!flag2 && !canOnlyHitCertainObjects)
			{
				bool flag6 = false;
				if (otdi.closestHitGroundEntity != Entity.Null && !isGoKart && playerAttackLookups.healthLookup.HasComponent(otdi.closestHitGroundEntity))
				{
					if (otdi.closestGroundDistance - num < 0.1f)
					{
						if (otdi.groundEntityTileInfo.tileType.GetSurfacePriorityFromJob() >= hitTile.tileType.GetSurfacePriorityFromJob())
						{
							flag6 = true;
						}
					}
					else if (otdi.closestGroundDistance < num)
					{
						flag6 = true;
					}
				}
				if (flag6)
				{
					ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitGroundEntity, otdi.closestEntityPosition, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, otdi.groundEntityPosition, out playerHealthChange, out ownerHealthChange, out attackerManaChange, out damageAfterReduction, out shouldBeKnockedback, out spawnThunderBeam, out spawnOctopusBossProjectile, out spawnScarabBossProjectile, out wasKilled, showDamageNumber: true, isExplosive: false, isDigging: false, attackWoundup: false, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
					GetTileDamageValues(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.closestHitGroundEntity, otdi.groundEntityPosition.RoundToInt2(), conditionsAtHit, conditionEffectsAtHit, damage, out var normHealth3, out var damageDone3, out damageAfterReduction);
					DoImmediateTileDamageEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, otdi.groundEntityTileInfo.tileset, otdi.groundEntityTileInfo.tileType, otdi.groundEntityPosition.RoundToInt3(), normHealth3, damageDone3, otdi.closestHitGroundEntity, isRanged: false, isBeam);
					result = true;
				}
				else if ((isGoKart && hitTile.tileType == TileType.bigRoot) || (!isGoKart && hitTile.tileType != 0))
				{
					Entity primaryPrefabEntity2 = PugDatabase.GetPrimaryPrefabEntity(PugDatabase.GetObjectID(hitTile.tileset, hitTile.tileType, playerAttackShared.databaseBank.databaseBankBlob), playerAttackShared.databaseBank.databaseBankBlob);
					GetTileDamageValues(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, primaryPrefabEntity2, @int.ToInt2(), conditionsAtHit, conditionEffectsAtHit, damage, out var normHealth4, out var damageDone4, out var damageDoneBeforeReduction3);
					DynamicBuffer<TileDamageBuffer> tileDamageBuffer2 = playerAttackLookups.tileDamageBufferLookup[playerAttackShared.tileDamageBufferSingleton];
					ClientSystem.CreateTileDamage(playerAttackAspect.entity, tileDamageBuffer2, new int2(@int.x, @int.z), damageDoneBeforeReduction3, in playerAttackShared.worldInfo, playerAttackAspect.entity, canDamageGround: false, pullAnyLootToPlayer: true);
					OnDamagingTile(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, hitTile, damageDone4, normHealth4, otdi.closestEntityPosition, out hitAnyWall);
					DoImmediateTileDamageEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, hitTile.tileset, hitTile.tileType, @int, normHealth4, damageDone4, primaryPrefabEntity2, isRanged: false, isBeam);
					result = true;
				}
			}
		}
		return result;
	}

	public static void ApplyPlayerManaChange(in Entity entity, in ComponentLookup<ManaCD> manaLookup, int playerManaChange)
	{
		manaLookup.GetRefRW(entity).ValueRW.mana += playerManaChange;
	}

	private static void ApplyPlayerHealthChange(in PlayerAttackAspect playerAttackAspect, in PlayerAttackLookups playerAttackLookups, PlayerAttackShared playerAttackShared, int playerHealthChange)
	{
		if (playerHealthChange > 0)
		{
			HealPlayer(in playerAttackShared, in playerAttackAspect, in playerAttackLookups, playerHealthChange);
		}
		else if (playerHealthChange < 0)
		{
			DealDamageToPlayer(playerAttackAspect.entity, playerAttackAspect.entity, -playerHealthChange, float3.zero, float3.zero, float3.zero, 0f, isExplosiveDamage: false, playerAttackLookups.playerStateLookup, playerAttackLookups.lastDamageTakenTimeLookup, playerAttackLookups.playerInvincibilityLookup, playerAttackLookups.healthLookup, playerAttackLookups.localTransformLookup, playerAttackLookups.magicBarrierLookup, playerAttackLookups.manaLookup, playerAttackLookups.summarizeConiditionsLookup, playerAttackLookups.summarizeConiditionsEffectsLookup, playerAttackLookups.ghostEffectEventBufferLookup, playerAttackLookups.ghostEffectEventBufferPointerLookup, playerAttackLookups.ghostInstanceLookup, playerAttackLookups.receivedPushbackLookup, playerAttackLookups.factionLookup, playerAttackLookups.ownerLookup, playerAttackShared.worldInfo, playerAttackShared.currentTick, playerAttackShared.tickRate);
			RefRW<GhostEffectEventBufferPointerCD> refRW = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity);
			DynamicBuffer<GhostEffectEventBuffer> buffer = playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity];
			ref GhostEffectEventBufferPointerCD valueRW = ref refRW.ValueRW;
			GhostEffectEventBuffer item = new GhostEffectEventBuffer
			{
				Tick = playerAttackShared.currentTick,
				value = new EffectEventCD
				{
					entity = playerAttackAspect.entity,
					effectID = EffectID.RedDamageNumber,
					value1 = -playerHealthChange
				}
			};
			buffer.AddToRingBuffer(ref valueRW, in item);
		}
	}

	private static void OnDamagingTile(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, TileCD tileInfo, int damageDone, float normHealth, float3 pos, out bool hitAnyWall)
	{
		hitAnyWall = false;
		if (tileInfo.tileset != 2 && tileInfo.tileType != TileType.greatWall && tileInfo.tileType == TileType.wall)
		{
			if ((float)damageDone >= 12f)
			{
				AddSkill(playerAttackAspect.entity, SkillID.Mining, 1, playerAttackShared.ecb, playerAttackShared.isServer);
			}
			if (damageDone > 0)
			{
				OnMiningWallBlock(in playerAttackAspect, in playerAttackShared, in playerAttackLookups);
			}
			hitAnyWall = true;
			if (normHealth <= 0f)
			{
				DestroyAnyObjectsHangingAtWallPosition(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, pos);
			}
			if (ShouldReceiveDurabilityFromDestroyingOre(normHealth, in playerAttackShared, pos.RoundToInt2()))
			{
				playerAttackLookups.increaseDurabilityOfEquippedLookup.SetComponentEnabled(playerAttackAspect.entity, value: true);
				playerAttackLookups.increaseDurabilityOfEquippedLookup.GetRefRW(playerAttackAspect.entity).ValueRW.triggerCounter++;
			}
		}
	}

	private static bool ShouldReceiveDurabilityFromDestroyingOre(float normHealth, in PlayerAttackShared playerAttackShared, int2 tilePos)
	{
		if (normHealth <= 0f)
		{
			if (!playerAttackShared.tileAccessor.HasType(tilePos, TileType.ore))
			{
				return playerAttackShared.tileAccessor.HasType(tilePos, TileType.ancientCrystal);
			}
			return true;
		}
		return false;
	}

	private static void OnMiningWallBlock(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups)
	{
		int conditionValue = EntityUtility.GetConditionValue(ConditionID.MovementSpeedBoostAfterMining, playerAttackAspect.entity, playerAttackLookups.summarizeConiditionsLookup);
		if (conditionValue > 0)
		{
			ConditionData conditionData = default(ConditionData);
			conditionData.conditionID = ConditionID.ShortMovementSpeedBoost;
			conditionData.value = conditionValue * 10;
			conditionData.duration = 5f;
			EntityUtility.AddOrRefreshCondition(conditionData, playerAttackLookups.conditionsBufferLookup[playerAttackAspect.entity], playerAttackShared.conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, playerAttackLookups.summarizeConiditionsLookup[playerAttackAspect.entity]);
		}
		int conditionValue2 = EntityUtility.GetConditionValue(ConditionID.GainDamageIncreaseAfterMining, playerAttackAspect.entity, playerAttackLookups.summarizeConiditionsLookup);
		if (conditionValue2 > 0)
		{
			ConditionData conditionData = default(ConditionData);
			conditionData.conditionID = ConditionID.DamageIncreaseAfterMining;
			conditionData.value = conditionValue2;
			conditionData.duration = 5f;
			EntityUtility.AddOrRefreshCondition(conditionData, playerAttackLookups.conditionsBufferLookup[playerAttackAspect.entity], playerAttackShared.conditionsTableCD, playerAttackShared.currentTick, playerAttackShared.tickRate, playerAttackLookups.summarizeConiditionsLookup[playerAttackAspect.entity]);
		}
	}

	public static void OnHarvest(Entity entity, ref HungerCD hungerCD, in PlayerStateCD playerStateCD, EntityCommandBuffer ecb, bool isServer, Entity hitEntity, bool wasInstantlyDestroyed, ComponentLookup<PlantCD> plantLookup, ComponentLookup<GrowingCD> growingLookup, ComponentLookup<ObjectDataCD> objectDataLookup, ComponentLookup<HealthCD> healthLookup, BufferLookup<SummarizedConditionsBuffer> summarizeConiditionsLookup, EntityArchetype achievementArchetype, ComponentLookup<ObjectPropertiesCD> propertiesLookup)
	{
		if (!plantLookup.TryGetComponent(hitEntity, out var componentData) || !growingLookup.TryGetComponent(hitEntity, out var componentData2) || !propertiesLookup.TryGetComponent(hitEntity, out var componentData3) || !componentData2.HasReachedFinalStage(componentData3))
		{
			return;
		}
		bool flag = true;
		ObjectID objectID = objectDataLookup[hitEntity].objectID;
		if ((objectID == ObjectID.RootPlant || objectID == ObjectID.CoralRootPlant) && !wasInstantlyDestroyed && healthLookup.TryGetComponent(hitEntity, out var componentData4))
		{
			flag = componentData4.health <= 1;
		}
		if (flag)
		{
			AddSkill(entity, SkillID.Gardening, 1, ecb, isServer);
			int conditionValue = EntityUtility.GetConditionValue(ConditionID.HungerAdditionOnHarvest, entity, summarizeConiditionsLookup);
			if (conditionValue > 0)
			{
				AddHunger(conditionValue, in playerStateCD, ref hungerCD);
			}
		}
		if (componentData.objectToDropWhenHarvested.IsGoldenPlant())
		{
			AchievementSystem.TriggerAchievement(isServer, ecb, achievementArchetype, AchievementID.HarvestGoldenIngredient, entity);
		}
	}

	public static void DigUpTile(TileType tileType, int tileset, int3 position, Entity pullTowardsEntity, DynamicBuffer<TileUpdateBuffer> tileUpdateBuffer, TileAccessor tileAccessor, PugDatabase.DatabaseBankCD databaseBankCD, EntityCommandBuffer ecb, TileWithTilesetToObjectDataMapCD tileWithTilesetToObjectDataMapCD, ComponentLookup<TileCD> tileLookup, bool isFirstTimeFullyPredictingTick)
	{
		ObjectDataCD objectDataCD = PugDatabase.TryGetTileItemInfo(tileType, (Tileset)tileset, in tileWithTilesetToObjectDataMapCD);
		if (objectDataCD.objectID == ObjectID.None)
		{
			return;
		}
		Entity primaryPrefabEntity = PugDatabase.GetPrimaryPrefabEntity(objectDataCD.objectID, databaseBankCD.databaseBankBlob);
		if (!tileLookup.HasComponent(primaryPrefabEntity))
		{
			Debug.LogError("Trying to remove tile that is actually pseudo tile");
			return;
		}
		int2 pos = position.ToInt2();
		EntityUtility.RemoveTile(tileset, tileType, pos, tileUpdateBuffer, tileAccessor);
		if (tileType != TileType.dugUpGround && tileType != TileType.wateredGround && isFirstTimeFullyPredictingTick)
		{
			EntityUtility.CreateAndDropItem(objectDataCD.objectID, 0, 1, new float3(position.x, 0f, position.z), pullTowardsEntity, databaseBankCD.databaseBankBlob, ecb);
		}
	}

	private static bool EntityIsValidObjectToDamage(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, Entity entityToCheck, float3 entityPos, bool canDamageTile, bool isTile, bool isTileCollider, bool isBeamWeapon)
	{
		if (!isTileCollider && (!playerAttackLookups.mineableLookup.HasComponent(entityToCheck) || playerAttackLookups.healthLookup[entityToCheck].health <= 0 || (playerAttackLookups.entityDestroyedLookup.HasComponent(entityToCheck) && playerAttackLookups.entityDestroyedLookup.IsComponentEnabled(entityToCheck))))
		{
			return false;
		}
		if (!isTile && !isTileCollider)
		{
			return true;
		}
		if (!canDamageTile)
		{
			return false;
		}
		if (isBeamWeapon)
		{
			return true;
		}
		Direction direction = playerAttackLookups.animationOrientationLookup[playerAttackAspect.entity].facingDirection;
		float3 position = playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position;
		if (!(Vector3.Angle(entityPos - position, direction.vec3) < 110f))
		{
			return false;
		}
		TileCD top = playerAttackShared.tileAccessor.GetTop(new int2((int)math.round(entityPos.x), (int)math.round(entityPos.z)));
		if (top.tileType != TileType.wall && !top.tileType.IsContainedResource())
		{
			return true;
		}
		Vector3Int vector3Int = new Vector3Int((int)math.round(entityPos.x), 0, (int)math.round(entityPos.z));
		int3 @int = position.RoundToInt3();
		bool num = (direction.id == Direction.Id.right && vector3Int.z > @int.z) || (direction.id == Direction.Id.left && vector3Int.z < @int.z) || (direction.id == Direction.Id.forward && vector3Int.x < @int.x) || (direction.id == Direction.Id.back && vector3Int.x > @int.x);
		bool flag = (direction.id == Direction.Id.right && vector3Int.z < @int.z) || (direction.id == Direction.Id.left && vector3Int.z > @int.z) || (direction.id == Direction.Id.forward && vector3Int.x > @int.x) || (direction.id == Direction.Id.back && vector3Int.x < @int.x);
		if (!num && !flag)
		{
			return true;
		}
		PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
		playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
		CollisionWorld collisionWorld = physicsWorld.CollisionWorld;
		ComponentLookup<UseLagCompensationCD> useLagCompensationLookup = playerAttackLookups.useLagCompensationLookup;
		Vector3 vector = vector3Int - direction.vec3 + Vector3.up * 0.5f;
		Vector3 vector2 = new Vector3(0.9f, 0.9f, 0.9f);
		NativeList<ColliderCastHit> allHits = new NativeList<ColliderCastHit>(Allocator.Temp);
		ColliderCastInput colliderCastInput = PhysicsManager.GetColliderCastInput(vector, vector, PhysicsManager.GetBoxCollider(float3.zero, vector2, 1u, playerAttackShared.colliderCache));
		collWorld.CastCollider(colliderCastInput, ref allHits);
		bool flag2 = false;
		foreach (ColliderCastHit item in allHits)
		{
			if (useLagCompensationLookup.HasComponent(item.Entity) && playerAttackLookups.tileLookup.TryGetComponent(item.Entity, out var componentData) && (componentData.tileType == TileType.wall || componentData.tileType.IsContainedResource()))
			{
				flag2 = true;
				break;
			}
		}
		if (!flag2)
		{
			allHits.Clear();
			collisionWorld.CastCollider(colliderCastInput, ref allHits);
			foreach (ColliderCastHit item2 in allHits)
			{
				if (!useLagCompensationLookup.HasComponent(item2.Entity) && playerAttackLookups.tileLookup.TryGetComponent(item2.Entity, out var componentData2) && (componentData2.tileType == TileType.wall || componentData2.tileType.IsContainedResource()))
				{
					flag2 = true;
					break;
				}
			}
		}
		allHits.Dispose();
		return !flag2;
	}

	public static bool ReceivesVitalityFromKillingEntity(Entity entity, ComponentLookup<EnemyCD> enemyLookup, ComponentLookup<MerchantCD> merchantLookup, ComponentLookup<PlayerGhost> playerGhostLookup, ComponentLookup<ProjectileCD> projectileLookup)
	{
		if (!enemyLookup.HasComponent(entity) && !merchantLookup.HasComponent(entity) && !playerGhostLookup.HasComponent(entity))
		{
			return projectileLookup.HasComponent(entity);
		}
		return true;
	}

	private static bool EntityIsDamageableObject(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.damageableObjectLookup.HasComponent(entityToCheck);
	}

	private static bool EntityIsDestructibleObject(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.destructibleObjectLookup.HasComponent(entityToCheck);
	}

	private static bool EntityRequiresDrill(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.requiresDrillLookup.HasComponent(entityToCheck);
	}

	private static bool EntityIsDamageableTile(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		if (playerAttackLookups.tileLookup.TryGetComponent(entityToCheck, out var componentData))
		{
			return componentData.tileType.IsDamageableTile();
		}
		return false;
	}

	private static bool EntityIsWall(Entity entityToCheck, PlayerAttackLookups playerAttackLookups)
	{
		if (playerAttackLookups.tileLookup.TryGetComponent(entityToCheck, out var componentData))
		{
			return componentData.tileType.IsWallTile();
		}
		return false;
	}

	private static bool EntityIsDamageablePseudoTile(Entity entityToCheck, PlayerAttackLookups playerAttackLookups)
	{
		if (playerAttackLookups.pseudoTileLookup.TryGetComponent(entityToCheck, out var componentData))
		{
			return componentData.tileType.IsDamageableTile();
		}
		return false;
	}

	private static bool EntityIsWalkableTile(Entity entityToCheck, bool isPseudoTile, PlayerAttackLookups playerAttackLookups)
	{
		if (!isPseudoTile)
		{
			return playerAttackLookups.tileLookup[entityToCheck].tileType.IsWalkableTile();
		}
		return playerAttackLookups.pseudoTileLookup[entityToCheck].tileType.IsWalkableTile();
	}

	private static bool EntityIsDamageableTileCollider(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		if (playerAttackLookups.tileColliderLookup.HasComponent(entityToCheck) && playerAttackLookups.tileColliderLookup.IsComponentEnabled(entityToCheck) && playerAttackLookups.tileLookup.TryGetComponent(entityToCheck, out var componentData))
		{
			return componentData.tileType.IsDamageableTile();
		}
		return false;
	}

	private static bool EntityIsRoot(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		if (!playerAttackLookups.tileLookup.TryGetComponent(entityToCheck, out var componentData) || componentData.tileType != TileType.bigRoot)
		{
			return playerAttackLookups.rootLookup.HasComponent(entityToCheck);
		}
		return true;
	}

	private static bool EntityIsIndestructible(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.indestructibleLookup.HasAndIsComponentEnabled(entityToCheck);
	}

	private static bool EntityIsImmuneToDamage(Entity entityToCheck, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups)
	{
		if (playerAttackLookups.objectDataLookup.TryGetComponent(entityToCheck, out var componentData) && componentData.objectID == ObjectID.PlayerGrave)
		{
			return false;
		}
		if (!playerAttackLookups.localTransformLookup.TryGetComponent(entityToCheck, out var componentData2))
		{
			return false;
		}
		int2 worldPosition = componentData2.Position.RoundToInt2();
		return playerAttackShared.tileAccessor.HasType(worldPosition, TileType.immune);
	}

	private static bool PositionIsImmuneToDamage(int2 worldPosition, in TileAccessor tileAccessor)
	{
		return tileAccessor.HasType(worldPosition, TileType.immune);
	}

	private static bool EntityIsGroundDecoration(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.groundDecorationLookup.HasComponent(entityToCheck);
	}

	private static bool EntityHasSurfacePriority(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.surfacePriorityLookup.HasComponent(entityToCheck);
	}

	private static int GetSurfacePriority(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		playerAttackLookups.surfacePriorityLookup.TryGetComponent(entityToCheck, out var componentData);
		return componentData.Value;
	}

	private static bool EntityIsCritter(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		return playerAttackLookups.critterLookup.HasComponent(entityToCheck);
	}

	private static bool EntityIsControlledByOtherEntity(Entity entityToCheck, in PlayerAttackLookups playerAttackLookups)
	{
		if (playerAttackLookups.controlledByOtherEntityLookup.TryGetComponent(entityToCheck, out var componentData))
		{
			return componentData.controlledByEntity != Entity.Null;
		}
		return false;
	}

	private static TileInfo GetAnyTileInfo(Entity entity, in PlayerAttackLookups playerAttackLookups)
	{
		TileInfo result = default(TileInfo);
		if (EntityIsDamageableTile(entity, in playerAttackLookups))
		{
			TileCD tileCD = playerAttackLookups.tileLookup[entity];
			result.tileType = tileCD.tileType;
			result.tileset = tileCD.tileset;
		}
		else if (EntityIsDamageablePseudoTile(entity, playerAttackLookups))
		{
			PseudoTileCD pseudoTileCD = playerAttackLookups.pseudoTileLookup[entity];
			result.tileType = pseudoTileCD.tileType;
			result.tileset = pseudoTileCD.tileset;
		}
		return result;
	}

	private static bool AttemptToDealDamageToEnemy(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, Entity entityToTakeDamage, float3 entityToTakeDamagePosition, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, int damage, bool isRanged, bool isMagic, bool isBeam, bool isExplosive, bool isWoundup, float windupMult, bool shouldKnockback, Vector3 projectileDirection, bool shouldShowHitFeedbackOnHitEntityPart, bool shouldHandleImmuneToDamageOnEntityPart, Entity hitEntityPart, out int attackerHealthChange, out int ownerHealthChange, out int attackerManaChange)
	{
		attackerHealthChange = 0;
		ownerHealthChange = 0;
		attackerManaChange = 0;
		if (!EntityUtility.EntityIsValidEnemyToDamage(entityToTakeDamage, playerAttackLookups.enemyLookup, playerAttackLookups.merchantLookup, playerAttackLookups.objectPropertiesLookup, playerAttackLookups.entityDestroyedLookup, playerAttackLookups.healthLookup, playerAttackLookups.playerGhostLookup))
		{
			return false;
		}
		playerAttackLookups.factionLookup.TryGetComponent(playerAttackAspect.entity, out var componentData);
		playerAttackLookups.factionLookup.TryGetComponent(entityToTakeDamage, out var componentData2);
		if (!componentData.CanAttack(componentData2, playerAttackShared.worldInfo))
		{
			return false;
		}
		float3 position = playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position;
		Entity entity = ((hitEntityPart != Entity.Null) ? hitEntityPart : entityToTakeDamage);
		float3 position2 = playerAttackLookups.localTransformLookup[entity].Position;
		if (!isRanged && !isExplosive && !isBeam)
		{
			bool flag = playerAttackLookups.playerStateLookup[playerAttackAspect.entity].HasAnyState(PlayerStateEnum.BoatRiding);
			PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
			playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
			if (collWorld.CastRay(PhysicsManager.GetRaycastInput(position + 0.5f * new float3(0f, 1f, 0f), position2 + new float3(0f, 0.5f, 0f), uint.MaxValue, 1u), out var closestHit) && entity != closestHit.Entity)
			{
				EntityPartCD componentData4;
				if (playerAttackLookups.tileLookup.TryGetComponent(closestHit.Entity, out var componentData3))
				{
					TileType tileType = componentData3.tileType;
					if ((!flag && tileType != TileType.water && tileType != TileType.pit) || (flag && tileType.IsWallTile()))
					{
						return false;
					}
				}
				else if (!playerAttackLookups.entityPartLookup.TryGetComponent(closestHit.Entity, out componentData4) || componentData4.mainEntity != entityToTakeDamage)
				{
					return false;
				}
			}
		}
		playerAttackLookups.immuneToDamageLookup.TryGetComponent(shouldHandleImmuneToDamageOnEntityPart ? hitEntityPart : entityToTakeDamage, out var componentData5);
		bool flag2 = componentData5.Value == ImmuneToDamageState.Immune;
		if (flag2 || IsDamageBlockedByEnemy(entityToTakeDamage, isRanged, isExplosive, projectileDirection, playerAttackAspect.entity, playerAttackLookups.animationOrientationLookup, playerAttackLookups.localTransformLookup, playerAttackLookups.shieldLookup))
		{
			if (playerAttackLookups.ghostEffectEventBufferLookup.TryGetBuffer(entityToTakeDamage, out var bufferData))
			{
				Entity entity2 = (shouldShowHitFeedbackOnHitEntityPart ? hitEntityPart : entityToTakeDamage);
				RefRW<GhostEffectEventBufferPointerCD> refRW = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(entity2);
				if (flag2 && componentData5.effectIDOverride != 0)
				{
					DynamicBuffer<GhostEffectEventBuffer> buffer = bufferData;
					ref GhostEffectEventBufferPointerCD valueRW = ref refRW.ValueRW;
					GhostEffectEventBuffer item = new GhostEffectEventBuffer
					{
						Tick = playerAttackShared.currentTick,
						value = new EffectEventCD
						{
							entity = entity2,
							effectID = componentData5.effectIDOverride
						}
					};
					buffer.AddToRingBuffer(ref valueRW, in item);
				}
				else
				{
					DynamicBuffer<GhostEffectEventBuffer> buffer2 = bufferData;
					ref GhostEffectEventBufferPointerCD valueRW2 = ref refRW.ValueRW;
					GhostEffectEventBuffer item = new GhostEffectEventBuffer
					{
						Tick = playerAttackShared.currentTick,
						value = new EffectEventCD
						{
							entity = entity2,
							effectID = EffectID.Parry,
							value1 = 1
						}
					};
					buffer2.AddToRingBuffer(ref valueRW2, in item);
				}
			}
			return true;
		}
		if (!isRanged && !isExplosive)
		{
			Entity equipmentPrefab = playerAttackAspect.equippedObjectCD.ValueRO.equipmentPrefab;
			int value = 918;
			if (EquipmentSlotUtility.IsMeleeWeaponSlotWithSound(playerAttackAspect.equipmentSlotCD.ValueRO.GetSlotType()) && (!playerAttackLookups.meleeWeaponLookup.TryGetComponent(equipmentPrefab, out var componentData6) || !componentData6.moveFreely))
			{
				value = 332;
			}
			int value2 = 0;
			if (playerAttackLookups.customAttackSoundLookup.TryGetComponent(equipmentPrefab, out var componentData7) && componentData7.impactSoundId != 0)
			{
				value = componentData7.impactSoundId;
				value2 = 1;
			}
			RefRW<GhostEffectEventBufferPointerCD> refRW2 = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity);
			DynamicBuffer<GhostEffectEventBuffer> buffer3 = playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity];
			ref GhostEffectEventBufferPointerCD valueRW3 = ref refRW2.ValueRW;
			GhostEffectEventBuffer item = new GhostEffectEventBuffer
			{
				Tick = playerAttackShared.currentTick,
				value = new EffectEventCD
				{
					effectID = EffectID.HitDamageSound,
					position1 = position,
					value1 = value,
					value2 = value2
				}
			};
			buffer3.AddToRingBuffer(ref valueRW3, in item);
		}
		ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, entityToTakeDamage, entityToTakeDamagePosition, shouldShowHitFeedbackOnHitEntityPart, hitEntityPart, conditionsAtHit, conditionEffectsAtHit, damage, isRanged, isMagic, playerAttackAspect.entity, position2, out attackerHealthChange, out ownerHealthChange, out attackerManaChange, out var damageAfterReduction, out var shouldBeKnockedback, out var spawnThunderBeam, out var spawnOctopusBossProjectile, out var spawnScarabBossProjectile, out var wasKilled, showDamageNumber: true, isExplosive, isDigging: false, isWoundup, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
		FactionCD componentData8;
		FactionID factionID = (playerAttackLookups.factionLookup.TryGetComponent(entityToTakeDamage, out componentData8) ? componentData8.faction : FactionID.None);
		if (factionID != 0 && factionID != FactionID.Merchant)
		{
			int experienceFromDamage = PetExtensions.GetExperienceFromDamage(damageAfterReduction);
			Entity playerEntity = playerAttackAspect.entity;
			ref readonly PetOwnerCD valueRO = ref playerAttackAspect.petOwnerCD.ValueRO;
			ref readonly PlayerGhost valueRO2 = ref playerAttackAspect.playerGhost.ValueRO;
			DynamicBuffer<ContainedObjectsBuffer> containedObjectsBuffer = playerAttackLookups.containedObjectsBufferLookup[playerAttackAspect.entity];
			IncreasePetXp(playerEntity, experienceFromDamage, in valueRO, in valueRO2, in containedObjectsBuffer, playerAttackLookups.inventoryChangeBufferLookup, playerAttackShared.inventoryChangeBufferEntity);
		}
		if (!playerAttackAspect.playerAttackCD.ValueRW.spawnStuffOnHitCooldown.isRunning || playerAttackAspect.playerAttackCD.ValueRW.spawnStuffOnHitCooldown.IsTimerElapsed(playerAttackShared.currentTick))
		{
			playerAttackAspect.playerAttackCD.ValueRW.spawnStuffOnHitCooldown.Start(playerAttackShared.currentTick, 0.1f, playerAttackShared.tickRate);
			RefRW<RandomCD> refRWOptional = playerAttackLookups.randomLookup.GetRefRWOptional(playerAttackAspect.entity);
			if (!isRanged && !isExplosive && spawnThunderBeam && playerAttackAspect.CollectedAndEnabledSoulsMask.ValueRO.HasSoulEnabled(SoulID.SoulOfAzeos))
			{
				AnimationOrientationCD animationOrientationCD = playerAttackLookups.animationOrientationLookup[playerAttackAspect.entity];
				EntityUtility.GetAttackerDamageIncrease(isRanged, isMagic, conditionEffectsAtHit, out var damageIncrease, out var damageIncreasePercentage);
				int num = (int)math.round((float)damage * (1f + (float)damageIncrease / 100f) * math.max(1f + (float)damageIncreasePercentage / 100f, 0f));
				ClientSystem.SpawnThunderBeam(in playerAttackShared, in playerAttackLookups, position + animationOrientationCD.facingDirection.f3, animationOrientationCD.facingDirection.f3, playerAttackAspect.entity, (int)math.round((float)num * 0.5f), refRWOptional);
			}
			if (isRanged && !isExplosive && spawnOctopusBossProjectile && playerAttackAspect.CollectedAndEnabledSoulsMask.ValueRO.HasSoulEnabled(SoulID.SoulOfOmoroth) && playerAttackShared.isFirstTimeFullyPredictingTick)
			{
				ClientSystem.SpawnProjectile(in playerAttackShared, in playerAttackLookups, ObjectID.OctopusBossPlayerProjectile, position2, float3.zero, playerAttackAspect.entity, (int)math.round((float)damage * 0.5f), weaponIsReinforced: false, ref refRWOptional.ValueRW.Value, 0, controlledByPlayer: false);
			}
			if (!isExplosive && spawnScarabBossProjectile && playerAttackAspect.CollectedAndEnabledSoulsMask.ValueRO.HasSoulEnabled(SoulID.SoulOfScarab) && playerAttackShared.isFirstTimeFullyPredictingTick)
			{
				ClientSystem.SpawnProjectile(in playerAttackShared, in playerAttackLookups, ObjectID.ScarabBossPlayerProjectile, position, float3.zero, playerAttackAspect.entity, (int)math.round((float)damage * 2f), weaponIsReinforced: false, ref refRWOptional.ValueRW.Value, 0, controlledByPlayer: false, entityToTakeDamage);
			}
		}
		if ((shouldKnockback || shouldBeKnockedback) && !wasKilled && !playerAttackLookups.immuneToPushBackLookup.HasComponent(entityToTakeDamage))
		{
			float num2 = 2f * windupMult;
			float2 @float = math.normalizesafe(position2 - position).ToFloat2();
			EntityUtility.TryAddPushback(entityToTakeDamage, @float * num2, playerAttackShared.currentTick, playerAttackShared.tickRate, playerAttackLookups.immuneToPushBackLookup, playerAttackLookups.receivedPushbackLookup, playerAttackLookups.moveToPredictedByPushbackLookup);
			if (playerAttackLookups.moveToPredictedByCombatInteractionLookup.HasComponent(entityToTakeDamage))
			{
				playerAttackLookups.moveToPredictedByCombatInteractionLookup.GetRefRW(entityToTakeDamage).ValueRW.SetLastInteractionTick(playerAttackShared.currentTick);
			}
		}
		return true;
	}

	public static bool IsDamageBlockedByEnemy(Entity entityToTakeDamage, bool isRanged, bool isExplosive, Vector3 projectileDirection, Entity attackerEntity, ComponentLookup<AnimationOrientationCD> animationOrientationLookup, ComponentLookup<LocalTransform> localTransformLookup, ComponentLookup<ShieldCD> shieldLookup)
	{
		if (isExplosive || !shieldLookup.TryGetComponent(entityToTakeDamage, out var componentData) || !componentData.active)
		{
			return false;
		}
		AnimationOrientationCD animationOrientationCD = animationOrientationLookup[entityToTakeDamage];
		float3 position = localTransformLookup[entityToTakeDamage].Position;
		float3 position2 = localTransformLookup[attackerEntity].Position;
		float3 x = math.normalizesafe(isRanged ? ((float3)projectileDirection) : (position - position2));
		float3 y = math.normalizesafe(-animationOrientationCD.facingDirection.f3);
		return math.degrees(math.acos(math.clamp(math.dot(x, y), -1f, 1f))) < (float)componentData.shieldWidthDegrees / 2f;
	}

	public static void DealCritterDamage(float3 fomPos, float3 toPos, float3 size, bool canDamageFlyingCritter, bool killEvenIfSquashBugsIsOff, in DealDamageToCrittersCD dealDamageToCrittersCD, PlayerAttackAspect playerAttackAspect, PlayerAttackLookups playerAttackLookups, PlayerAttackShared playerAttackShared)
	{
		if (!dealDamageToCrittersCD.squashBugs && !killEvenIfSquashBugsIsOff)
		{
			return;
		}
		PhysicsCollider boxCollider = PhysicsManager.GetBoxCollider(float3.zero, size, 32768u, playerAttackShared.colliderCache);
		PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
		playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
		NativeList<ColliderCastHit> allHits = new NativeList<ColliderCastHit>(Allocator.Temp);
		collWorld.CastCollider(PhysicsManager.GetColliderCastInput(fomPos, toPos, boxCollider), ref allHits);
		foreach (ColliderCastHit item in allHits)
		{
			Entity entity = item.Entity;
			if (playerAttackLookups.objectPropertiesLookup.TryGetComponent(entity, out var componentData) && componentData.Has(-2112283771) && (canDamageFlyingCritter || !componentData.Has(1515467597)))
			{
				float3 pos = collWorld.Bodies[item.RigidBodyIndex].WorldFromBody.pos;
				ClientSystem.DealDamageToEntity(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, entity, pos, shouldShowHitFeedbackOnHitEntityPart: false, Entity.Null, default(NativeArray<SummarizedConditionsBuffer>), default(NativeArray<SummarizedConditionEffectsBuffer>), 1, isRanged: false, isMagic: false, playerAttackAspect.entity, pos, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _, showDamageNumber: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
			}
		}
		allHits.Dispose();
	}

	public static void GetTileDamageValues(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, Entity tileEntity, int2 position, NativeArray<SummarizedConditionsBuffer> conditionsAtHit, NativeArray<SummarizedConditionEffectsBuffer> conditionEffectsAtHit, int damage, out float normHealth, out int damageDone, out int damageDoneBeforeReduction, bool isTileDamage = false, bool isDigging = false)
	{
		NativeList<ConditionData> conditionsToApply = new NativeList<ConditionData>(Allocator.Temp);
		NativeList<ConditionData> conditionsToApplyToAttacker = new NativeList<ConditionData>(Allocator.Temp);
		NativeList<ConditionID> conditionsToRemove = new NativeList<ConditionID>(Allocator.Temp);
		NativeList<ConditionID> conditionsToRemoveFromAttacker = new NativeList<ConditionID>(Allocator.Temp);
		FactionCD attackerFaction = playerAttackLookups.factionLookup[playerAttackAspect.entity];
		EntityUtility.GetDamageInfo(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, tileEntity, playerAttackAspect.entity, damage, position, isRanged: false, isMagic: false, isReverseDamage: false, conditionsAtHit, conditionEffectsAtHit, out damageDone, out damageDoneBeforeReduction, out var _, out normHealth, conditionsToApply, conditionsToApplyToAttacker, conditionsToRemove, conditionsToRemoveFromAttacker, attackerFaction, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _, out var _, isTileDamage, isDigging, attackWoundup: false, bypassMaxDamagePerHit: false, playerAttackLookups.godModeLookup.IsComponentEnabled(playerAttackAspect.entity));
		conditionsToApply.Dispose();
		conditionsToApplyToAttacker.Dispose();
		conditionsToRemove.Dispose();
		conditionsToRemoveFromAttacker.Dispose();
	}

	public static void DoImmediateTileDamageEffects(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, int tileset, TileType tileType, int3 pos, float normHealth, int damageDone, Entity tileEntity, bool isRanged = false, bool isBeam = false, bool isExplosive = false, bool skipPushingPlayer = false)
	{
		int2 @int = pos.ToInt2();
		TileType tileType2 = tileType;
		int tileSet = tileset;
		if (!tileType.IsPseudoTile())
		{
			TileCD top = playerAttackShared.tileAccessor.GetTop(@int);
			tileType2 = top.tileType;
			tileSet = top.tileset;
		}
		if (tileType == TileType.ground)
		{
			DynamicBuffer<TileUpdateBuffer> dynamicBuffer = playerAttackLookups.tileUpdateBufferLookup[playerAttackShared.tileUpdateBufferEntity];
			dynamicBuffer.Add(new TileUpdateBuffer
			{
				command = TileUpdateBuffer.Command.Remove,
				position = @int,
				tile = new TileCD
				{
					tileType = TileType.smallGrass,
					tileset = 0
				}
			});
			dynamicBuffer.Add(new TileUpdateBuffer
			{
				command = TileUpdateBuffer.Command.Remove,
				position = @int,
				tile = new TileCD
				{
					tileType = TileType.smallStones,
					tileset = 0
				}
			});
			dynamicBuffer.Add(new TileUpdateBuffer
			{
				command = TileUpdateBuffer.Command.Remove,
				position = @int,
				tile = new TileCD
				{
					tileType = TileType.debris,
					tileset = 0
				}
			});
			dynamicBuffer.Add(new TileUpdateBuffer
			{
				command = TileUpdateBuffer.Command.Remove,
				position = @int,
				tile = new TileCD
				{
					tileType = TileType.debris2,
					tileset = 0
				}
			});
			tileType2 = tileType;
		}
		if (damageDone > 0)
		{
			if (normHealth <= 0f && tileType == TileType.wall && !skipPushingPlayer && !isRanged && !isExplosive && !isBeam && !playerAttackAspect.playerStateCD.ValueRO.HasAnyState(PlayerStateEnum.BoatRiding) && !playerAttackAspect.playerStateCD.ValueRO.HasAnyState(PlayerStateEnum.MinecartRiding))
			{
				float3 @float = pos - playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position;
				@float.y = 0f;
				PhysicsMass massData = playerAttackLookups.physicsMassLookup[playerAttackAspect.entity];
				ref PhysicsVelocity valueRW = ref playerAttackLookups.physicsVelocityLookup.GetRefRW(playerAttackAspect.entity).ValueRW;
				float3 impulse = @float * 10f;
				valueRW.ApplyLinearImpulse(in massData, in impulse);
			}
			HealthCD componentData;
			int num = (int)math.round((float)(playerAttackLookups.healthLookup.TryGetComponent(tileEntity, out componentData) ? componentData.maxHealth : 100) / (float)math.max(damageDone, 1));
			bool weakHit = (float)num >= 12f;
			PlayMineEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, pos + new float3(0f, 1f, 0f) * 0.5f, tileType2, tileSet, failed: false, normHealth <= 0f, Mathf.Clamp(1f - normHealth, 0.2f, 1f), weakHit, !isRanged && !isExplosive && !isBeam && !skipPushingPlayer, num);
			playerAttackAspect.cornerSmoothingCD.ValueRW.cornerMovementBlendMultiplier = 0f;
		}
		else
		{
			PlayMineEffects(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, pos, tileType2, tileSet, failed: true, destroyed: false, 0f, weakHit: false, !isRanged && !isExplosive && !isBeam && !skipPushingPlayer, 1, isRanged || isExplosive);
		}
	}

	private static void DestroyAnyObjectsHangingAtWallPosition(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, float3 wallPosition)
	{
		NativeList<DistanceHit> outHits = new NativeList<DistanceHit>(Allocator.Temp);
		CollisionFilter collisionFilter = default(CollisionFilter);
		collisionFilter.BelongsTo = uint.MaxValue;
		collisionFilter.CollidesWith = 64u;
		CollisionFilter filter = collisionFilter;
		PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
		playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
		collWorld.OverlapSphere(wallPosition, 0.55f, ref outHits, filter);
		for (int i = 0; i < outHits.Length; i++)
		{
			Entity entity = outHits[i].Entity;
			if (playerAttackLookups.objectPropertiesLookup.TryGetComponent(entity, out var componentData) && componentData.Has(-1171081164))
			{
				EntityUtility.Destroy(entity, dontDrop: false, playerAttackAspect.entity, playerAttackLookups.healthLookup, playerAttackLookups.entityDestroyedLookup, playerAttackLookups.dontDropSelfLookup, playerAttackLookups.dontDropLootLookup, playerAttackLookups.killedByPlayerLookup, playerAttackLookups.plantLookup, playerAttackLookups.summarizeConiditionsEffectsLookup, ref playerAttackLookups.randomLookup.GetRefRW(playerAttackAspect.entity).ValueRW.Value, playerAttackLookups.moveToPredictedByEntityDestroyedLookup, playerAttackShared.currentTick);
			}
		}
		outHits.Dispose();
	}

	private static bool IsTileSet(in PlayerAttackShared playerAttackShared, int3 pos, int tileset, TileType tileType)
	{
		bool result = false;
		NativeArray<TileCD> nativeArray = playerAttackShared.tileAccessor.Get(pos.ToInt2(), Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray[i].tileset == tileset && nativeArray[i].tileType == tileType)
			{
				result = true;
				break;
			}
		}
		nativeArray.Dispose();
		return result;
	}

	private static void GetAnyDamageableTile(int3 pos, out TileCD hitTile, in PlayerAttackShared playerAttackShared)
	{
		hitTile = default(TileCD);
		NativeArray<TileCD> nativeArray = playerAttackShared.tileAccessor.Get(pos.ToInt2(), Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (nativeArray[i].tileType.IsDamageableTile() && hitTile.tileType.GetSurfacePriorityFromJob() < nativeArray[i].tileType.GetSurfacePriorityFromJob())
			{
				hitTile = nativeArray[i];
			}
		}
		nativeArray.Dispose();
	}

	private static void PlayMineEffects(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, float3 position, TileType tileType, int tileSet, bool failed, bool destroyed, float amplitude = 0f, bool weakHit = false, bool allowPushback = true, int hitsNeededToDestroy = 1, bool skipEmote = false)
	{
		int value = 0;
		if (tileType.IsContainedResource() && playerAttackShared.tileAccessor.GetType(position.RoundToInt2(), TileType.wall, out var tileCD))
		{
			value = tileCD.tileset;
		}
		RefRW<GhostEffectEventBufferPointerCD> refRW;
		GhostEffectEventBuffer item;
		if (!failed && destroyed)
		{
			refRW = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity);
			ref GhostEffectEventBufferPointerCD valueRW = ref refRW.ValueRW;
			DynamicBuffer<GhostEffectEventBuffer> buffer = playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity];
			item = new GhostEffectEventBuffer
			{
				Tick = playerAttackShared.currentTick,
				value = new EffectEventCD
				{
					effectID = EffectID.DestroyTile,
					position1 = position,
					value1 = value,
					tileInfo = new TileInfo
					{
						tileset = tileSet,
						tileType = tileType
					}
				}
			};
			buffer.AddToRingBuffer(ref valueRW, in item);
			return;
		}
		if (failed)
		{
			Emote.EmoteType emoteText = (skipEmote ? Emote.EmoteType.__illegal__ : GetTileDamageEmoteType(tileType, (Tileset)tileSet, position.RoundToInt2(), 0, in playerAttackShared.tileAccessor));
			PlayMineFailedEffect(in playerAttackAspect, in playerAttackShared, in playerAttackLookups, position, emoteText, allowPushback);
			return;
		}
		refRW = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity);
		ref GhostEffectEventBufferPointerCD valueRW2 = ref refRW.ValueRW;
		DynamicBuffer<GhostEffectEventBuffer> buffer2 = playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity];
		item = new GhostEffectEventBuffer
		{
			Tick = playerAttackShared.currentTick,
			value = new EffectEventCD
			{
				effectID = (weakHit ? EffectID.DamageTileWeak : EffectID.DamageTile),
				position1 = new float3(position.x, 1f, position.z),
				value1 = value,
				tileInfo = new TileInfo
				{
					tileset = tileSet,
					tileType = tileType
				}
			}
		};
		buffer2.AddToRingBuffer(ref valueRW2, in item);
		if ((float)hitsNeededToDestroy >= 12f && tileSet != 36 && tileSet != 59 && tileType.IsWallTile())
		{
			playerAttackAspect.emoteEffectsCD.ValueRW.thisIsGoingToTakeAWhileLastDamageTick = playerAttackShared.currentTick;
			playerAttackAspect.emoteEffectsCD.ValueRW.thisIsGoingToTakeAWhileHitsNeededToDestroy = hitsNeededToDestroy;
		}
		else
		{
			playerAttackAspect.emoteEffectsCD.ValueRW.thisIsGoingToTakeAWhileLastDamageTick = default(NetworkTick);
		}
	}

	private static void PlayMineFailedEffect(in PlayerAttackAspect playerAttackAspect, in PlayerAttackShared playerAttackShared, in PlayerAttackLookups playerAttackLookups, float3 position, Emote.EmoteType emoteText = Emote.EmoteType.__illegal__, bool allowPushback = true, bool raycastToFindSparksPosition = true)
	{
		float3 position2 = playerAttackLookups.localTransformLookup[playerAttackAspect.entity].Position;
		float3 @float = math.normalizesafe(position2 - position);
		@float = new Vector3(@float.x, 0f, @float.z);
		if (allowPushback)
		{
			Entity playerEntity = playerAttackAspect.entity;
			float3 force = @float * 0.3f;
			PlayerStateCD playerStateCD = playerAttackLookups.playerStateLookup[playerAttackAspect.entity];
			Pushback(playerEntity, force, in playerStateCD, playerAttackLookups.receivedPushbackLookup, playerAttackShared.currentTick, playerAttackShared.tickRate);
		}
		bool flag = true;
		float3 position3 = ((!raycastToFindSparksPosition) ? position : (position2 - @float * 0.5f + new float3(0f, 1f, 0f) * 0.5f));
		if (raycastToFindSparksPosition)
		{
			PhysicsWorld physicsWorld = playerAttackShared.physicsWorld;
			playerAttackShared.physicsWorldHistory.GetCollisionWorldFromTick(playerAttackShared.currentTick, playerAttackAspect.interpolationDelay.ValueRO.Delay, ref physicsWorld, out var collWorld);
			RaycastInput raycastInput = default(RaycastInput);
			raycastInput.Start = position2 + new float3(0f, 1f, 0f) * 0.5f;
			raycastInput.End = position + new float3(0f, 1f, 0f) * 0.5f;
			raycastInput.Filter = new CollisionFilter
			{
				BelongsTo = uint.MaxValue,
				CollidesWith = 1u
			};
			RaycastInput input = raycastInput;
			if (collWorld.CastRay(input, out var closestHit))
			{
				position3 = closestHit.Position;
			}
			else
			{
				float3 float2 = playerAttackAspect.clientInput.ValueRO.targetingDirection.ToFloat3();
				input.End = position2 + float2 * 3f + new float3(0f, 1f, 0f) * 0.5f;
				if (collWorld.CastRay(input, out closestHit))
				{
					position3 = closestHit.Position;
				}
			}
		}
		DynamicBuffer<GhostEffectEventBuffer> buffer = playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity];
		RefRW<GhostEffectEventBufferPointerCD> refRW = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity);
		ref GhostEffectEventBufferPointerCD valueRW = ref refRW.ValueRW;
		GhostEffectEventBuffer item = new GhostEffectEventBuffer
		{
			Tick = playerAttackShared.currentTick,
			value = new EffectEventCD
			{
				effectID = ((!flag) ? EffectID.FailedHit : EffectID.FailedHitWithSparks),
				position1 = position3
			}
		};
		buffer.AddToRingBuffer(ref valueRW, in item);
		if (emoteText != Emote.EmoteType.__illegal__)
		{
			ref GhostEffectEventBufferPointerCD valueRW2 = ref refRW.ValueRW;
			item = new GhostEffectEventBuffer
			{
				Tick = playerAttackShared.currentTick,
				value = new EffectEventCD
				{
					entity = playerAttackAspect.entity,
					localOnlyEffect = 1,
					effectID = EffectID.Emote,
					value1 = (int)emoteText
				}
			};
			buffer.AddToRingBuffer(ref valueRW2, in item);
		}
	}

	private static Emote.EmoteType GetDamageEmoteType(Entity entityToCheck, int damageDoneAfterReduction, in PlayerAttackLookups playerAttackLookups, in PlayerAttackShared playerAttackShared)
	{
		if (EntityRequiresDrill(entityToCheck, in playerAttackLookups))
		{
			return Emote.EmoteType.RequiresDrill;
		}
		if (EntityIsIndestructible(entityToCheck, in playerAttackLookups))
		{
			return Emote.EmoteType.ObjectIsIndestructible;
		}
		if (EntityIsImmuneToDamage(entityToCheck, in playerAttackShared, in playerAttackLookups))
		{
			return Emote.EmoteType.ObjectIsImmune;
		}
		if (damageDoneAfterReduction <= 0)
		{
			return Emote.EmoteType.NeedHigherMiningSkill;
		}
		return Emote.EmoteType.__illegal__;
	}

	private static Emote.EmoteType GetTileDamageEmoteType(TileType tileType, Tileset tileset, int2 tileWorldPosition, int damageDoneAfterReduction, in TileAccessor tileAccessor)
	{
		if (damageDoneAfterReduction <= 0)
		{
			if (tileset == Tileset.Obsidian || tileType == TileType.greatWall)
			{
				return Emote.EmoteType.ObjectIsIndestructible;
			}
			if (PositionIsImmuneToDamage(tileWorldPosition, in tileAccessor))
			{
				return Emote.EmoteType.ObjectIsImmune;
			}
			return Emote.EmoteType.NeedHigherMiningSkill;
		}
		return Emote.EmoteType.__illegal__;
	}

	public static void RespawnPlayer(StateUpdateAspect stateUpdateAspect, SharedStateUpdateData sharedStateUpdateData, LookupStateUpdateData lookupStateUpdateData)
	{
		int health = stateUpdateAspect.healthCD.ValueRO.maxHealth;
		if (lookupStateUpdateData.summarizedConditionEffectsLookup.TryGetBuffer(stateUpdateAspect.entity, out var bufferData))
		{
			health = stateUpdateAspect.healthCD.ValueRO.GetMaxHealthWithConditions(bufferData);
		}
		stateUpdateAspect.healthCD.ValueRW.health = health;
		stateUpdateAspect.hungerCD.ValueRW.hunger = 70;
		stateUpdateAspect.playerSpawnCD.ValueRW.lastRespawnTick = sharedStateUpdateData.currentTick;
		ConditionData conditionData = default(ConditionData);
		conditionData.conditionID = ConditionID.ImmuneToDamageAfterRespawn;
		conditionData.value = 1;
		conditionData.duration = 0f;
		EntityUtility.AddOrRefreshCondition(conditionData, stateUpdateAspect.conditionsBuffers, sharedStateUpdateData.conditionsTableCD, sharedStateUpdateData.currentTick, sharedStateUpdateData.tickRate, lookupStateUpdateData.summarizedConditionsLookup[stateUpdateAspect.entity]);
	}

	public static void HealPlayer(in PlayerAttackShared playerAttackShared, in PlayerAttackAspect playerAttackAspect, in PlayerAttackLookups playerAttackLookups, int healing)
	{
		HealPlayer(in playerAttackAspect, in playerAttackLookups, healing);
		RefRW<GhostEffectEventBufferPointerCD> refRW = playerAttackLookups.ghostEffectEventBufferPointerLookup.GetRefRW(playerAttackAspect.entity);
		DynamicBuffer<GhostEffectEventBuffer> buffer = playerAttackLookups.ghostEffectEventBufferLookup[playerAttackAspect.entity];
		ref GhostEffectEventBufferPointerCD valueRW = ref refRW.ValueRW;
		GhostEffectEventBuffer item = new GhostEffectEventBuffer
		{
			Tick = playerAttackShared.currentTick,
			value = new EffectEventCD
			{
				entity = playerAttackAspect.entity,
				effectID = EffectID.HealingNumber,
				value1 = healing
			}
		};
		buffer.AddToRingBuffer(ref valueRW, in item);
	}

	public static void HealPlayer(in PlayerAttackAspect playerAttackAspect, in PlayerAttackLookups playerAttackLookups, int healing)
	{
		RefRW<HealthCD> refRW = playerAttackLookups.healthLookup.GetRefRW(playerAttackAspect.entity);
		ref HealthCD valueRW = ref refRW.ValueRW;
		PlayerStateCD playerStateCD = playerAttackLookups.playerStateLookup[playerAttackAspect.entity];
		DynamicBuffer<SummarizedConditionEffectsBuffer> summarizedConditionEffectsBuffer = playerAttackLookups.summarizeConiditionsEffectsLookup[playerAttackAspect.entity];
		HealPlayer(healing, ref valueRW, in playerStateCD, in summarizedConditionEffectsBuffer);
	}

	public static void HealPlayer(int healing, ref HealthCD healthCD, in PlayerStateCD playerStateCD, in DynamicBuffer<SummarizedConditionEffectsBuffer> summarizedConditionEffectsBuffer)
	{
		if (!playerStateCD.HasAnyState(PlayerStateEnum.NoClip | PlayerStateEnum.Death) && healing > 0)
		{
			if (summarizedConditionEffectsBuffer[12].value > 0 && summarizedConditionEffectsBuffer[30].value == 0)
			{
				healing = (int)math.max(math.round((float)healing * 0.25f), 1f);
			}
			int x = healthCD.health + healing;
			int maxHealthWithConditions = healthCD.GetMaxHealthWithConditions(summarizedConditionEffectsBuffer);
			healthCD.health = math.min(x, maxHealthWithConditions);
		}
	}

	public static int DealDamageToPlayer(Entity targetPlayer, Entity attacker, int damage, float3 position, float3 attackerPosition, float3 direction, float pushback, bool isExplosiveDamage, ComponentLookup<PlayerStateCD> playerStateLookup, ComponentLookup<LastDamageTakenTimeCD> timeSinceLastDamageTakenLookup, ComponentLookup<PlayerInvincibilityCD> playerInvincibilityLookup, ComponentLookup<HealthCD> healthLookup, ComponentLookup<LocalTransform> transformLookup, ComponentLookup<MagicBarrierCD> magicBarrierLookup, ComponentLookup<ManaCD> manaLookup, BufferLookup<SummarizedConditionsBuffer> summarizedConditionsBufferLookup, BufferLookup<SummarizedConditionEffectsBuffer> summarizedConditionEffectsBufferLookup, BufferLookup<GhostEffectEventBuffer> ghostEffectEventBufferLookup, ComponentLookup<GhostEffectEventBufferPointerCD> ghostEffectEventBufferPointerLookup, ComponentLookup<GhostInstance> ghostLookup, ComponentLookup<ReceivedPushbackCD> receivedPushbackLookup, ComponentLookup<FactionCD> factionsLookUp, ComponentLookup<OwnerReferenceCD> ownerLookup, WorldInfoCD worldInfo, NetworkTick currentTick, uint tickRate)
	{
		if (playerStateLookup[targetPlayer].isStateLocked || playerInvincibilityLookup[targetPlayer].isInvincible)
		{
			return 0;
		}
		timeSinceLastDamageTakenLookup.GetRefRW(targetPlayer).ValueRW.lastDamageTick = currentTick;
		Entity entity = (ghostEffectEventBufferLookup.HasBuffer(attacker) ? attacker : targetPlayer);
		if (magicBarrierLookup.TryGetComponent(targetPlayer, out var componentData) && componentData.barrierHealth > 0)
		{
			RefRW<GhostEffectEventBufferPointerCD> refRW = ghostEffectEventBufferPointerLookup.GetRefRW(entity);
			DynamicBuffer<GhostEffectEventBuffer> buffer = ghostEffectEventBufferLookup[entity];
			ref GhostEffectEventBufferPointerCD valueRW = ref refRW.ValueRW;
			GhostEffectEventBuffer item = new GhostEffectEventBuffer
			{
				Tick = currentTick,
				value = new EffectEventCD
				{
					effectID = EffectID.PlayerTakeMagicBarrierDamage,
					entity = targetPlayer
				}
			};
			buffer.AddToRingBuffer(ref valueRW, in item);
		}
		else
		{
			RefRW<GhostEffectEventBufferPointerCD> refRW2 = ghostEffectEventBufferPointerLookup.GetRefRW(entity);
			DynamicBuffer<GhostEffectEventBuffer> buffer2 = ghostEffectEventBufferLookup[entity];
			ref GhostEffectEventBufferPointerCD valueRW2 = ref refRW2.ValueRW;
			GhostEffectEventBuffer item = new GhostEffectEventBuffer
			{
				Tick = currentTick,
				value = new EffectEventCD
				{
					effectID = EffectID.PlayerTakeDamage,
					entity = targetPlayer,
					value1 = damage
				}
			};
			buffer2.AddToRingBuffer(ref valueRW2, in item);
		}
		damage = PlayerDamageAbsorbed(damage, targetPlayer, attacker, magicBarrierLookup, manaLookup, summarizedConditionsBufferLookup, summarizedConditionEffectsBufferLookup, factionsLookUp, ownerLookup, worldInfo, isExplosiveDamage);
		ref HealthCD valueRW3 = ref healthLookup.GetRefRW(targetPlayer).ValueRW;
		valueRW3.health -= damage;
		if (valueRW3.health <= 0)
		{
			GhostInstance ghostInstance = ghostLookup[targetPlayer];
			float num = (float)EntityUtility.GetConditionValue(ConditionID.CheatDeath, targetPlayer, summarizedConditionsBufferLookup) / 100f;
			if (Unity.Mathematics.Random.CreateFromIndex((uint)((int)(ghostInstance.ghostId ^ ghostInstance.ghostType ^ currentTick.TickIndexForValidTick) + 5)).NextFloat() < num)
			{
				int maxHealthWithConditions = valueRW3.GetMaxHealthWithConditions(summarizedConditionEffectsBufferLookup[targetPlayer]);
				valueRW3.health = (int)math.round((float)maxHealthWithConditions * 0.2f);
				RefRW<GhostEffectEventBufferPointerCD> refRW3 = ghostEffectEventBufferPointerLookup.GetRefRW(targetPlayer);
				DynamicBuffer<GhostEffectEventBuffer> buffer3 = ghostEffectEventBufferLookup[targetPlayer];
				ref GhostEffectEventBufferPointerCD valueRW4 = ref refRW3.ValueRW;
				GhostEffectEventBuffer item = new GhostEffectEventBuffer
				{
					Tick = currentTick,
					value = new EffectEventCD
					{
						entity = targetPlayer,
						effectID = EffectID.CheatDeath
					}
				};
				buffer3.AddToRingBuffer(ref valueRW4, in item);
			}
		}
		if (valueRW3.health > 0 && pushback != 0f)
		{
			if (math.all(direction == float3.zero))
			{
				float2 xz = transformLookup[targetPlayer].Position.xz;
				float2 @float = (isExplosiveDamage ? attackerPosition.xz : position.xz);
				direction = math.normalizesafe(xz - @float).X0Y();
			}
			float3 force = direction * pushback;
			PlayerStateCD playerStateCD = playerStateLookup[targetPlayer];
			Pushback(targetPlayer, force, in playerStateCD, receivedPushbackLookup, currentTick, tickRate);
		}
		return damage;
	}

	public static int PlayerDamageAbsorbed(int damageIn, Entity targetPlayer, Entity attacker, ComponentLookup<MagicBarrierCD> magicBarrierLookup, ComponentLookup<ManaCD> manaLookup, BufferLookup<SummarizedConditionsBuffer> summarizedConditionsBufferLookup, BufferLookup<SummarizedConditionEffectsBuffer> summarizedConditionEffectsBufferLookup, ComponentLookup<FactionCD> factionsLookUp, ComponentLookup<OwnerReferenceCD> ownerLookup, WorldInfoCD worldInfo, bool isExplosive = false)
	{
		int num = damageIn;
		RefRW<MagicBarrierCD> refRW = magicBarrierLookup.GetRefRW(targetPlayer);
		if (refRW.ValueRW.barrierHealth > 0)
		{
			int barrierHealth = magicBarrierLookup.GetRefRO(targetPlayer).ValueRO.barrierHealth;
			if (barrierHealth > num)
			{
				refRW = magicBarrierLookup.GetRefRW(targetPlayer);
				refRW.ValueRW.barrierHealth -= damageIn;
				num = 0;
			}
			else
			{
				refRW = magicBarrierLookup.GetRefRW(targetPlayer);
				refRW.ValueRW.barrierHealth = 0;
				num -= barrierHealth;
			}
		}
		int conditionValue = EntityUtility.GetConditionValue(ConditionID.ManaBarrier, targetPlayer, summarizedConditionsBufferLookup);
		RefRW<ManaCD> refRW2;
		if (conditionValue != 0)
		{
			refRW2 = manaLookup.GetRefRW(targetPlayer);
			ManaCD valueRW = refRW2.ValueRW;
			float num2 = (float)num * ((float)conditionValue / 100f);
			int mana = valueRW.mana;
			if ((float)mana > num2)
			{
				refRW2 = manaLookup.GetRefRW(targetPlayer);
				refRW2.ValueRW.mana = math.clamp(valueRW.mana -= (int)num2, 0, valueRW.maxMana);
				num = (int)num2;
			}
			else
			{
				refRW2 = manaLookup.GetRefRW(targetPlayer);
				refRW2.ValueRW.mana = math.clamp(valueRW.mana -= mana, 0, valueRW.maxMana);
				num -= mana;
			}
			refRW2 = manaLookup.GetRefRW(targetPlayer);
			refRW2.ValueRW.delay = true;
		}
		if (num > 0)
		{
			int conditionValue2 = EntityUtility.GetConditionValue(ConditionID.GainManaPercentageOnDamageTaken, targetPlayer, summarizedConditionsBufferLookup);
			if (conditionValue2 > 0)
			{
				refRW2 = manaLookup.GetRefRW(targetPlayer);
				ManaCD valueRO = refRW2.ValueRO;
				int num3 = (int)math.round((float)valueRO.maxMana / 100f * (float)conditionValue2);
				refRW2 = manaLookup.GetRefRW(targetPlayer);
				refRW2.ValueRW.mana = math.clamp(valueRO.mana += num3, 0, valueRO.maxMana);
			}
		}
		if (isExplosive)
		{
			factionsLookUp.TryGetComponent(attacker, out var componentData);
			if (ownerLookup.TryGetComponent(attacker, out var componentData2) && factionsLookUp.TryGetComponent(componentData2.owner, out var componentData3))
			{
				componentData = componentData3;
			}
			if (componentData.IsFriendlyFire(factionsLookUp[targetPlayer], worldInfo))
			{
				float num4 = (float)EntityUtility.GetConditionEffectValue(ConditionEffect.ReducedDamageFromExplosions, targetPlayer, summarizedConditionEffectsBufferLookup) / 100f;
				if (num4 > 0f)
				{
					num -= (int)(num4 * (float)num);
				}
			}
		}
		return num;
	}

	public bool HeldItemIsBroken()
	{
		EquipmentSlot equippedSlot = GetEquippedSlot();
		if (equippedSlot != null)
		{
			return ItemIsBroken(equippedSlot.objectData);
		}
		return false;
	}

	public static bool ItemIsBroken(ObjectDataCD itemObjectData)
	{
		if (itemObjectData.amount <= 0 && PugDatabase.HasComponent<DurabilityCD>(itemObjectData.objectID))
		{
			ObjectInfo objectInfo = PugDatabase.GetObjectInfo(itemObjectData.objectID, itemObjectData.variation);
			if (objectInfo != null && !objectInfo.isStackable)
			{
				return true;
			}
		}
		return false;
	}

	public static bool ItemIsBroken(ObjectDataCD itemObjectData, BlobAssetReference<PugDatabase.PugDatabaseBank> pugDatabaseBank, ComponentLookup<DurabilityCD> durabilityLookup)
	{
		return ItemIsBroken(PugDatabase.GetPrimaryPrefabEntity(itemObjectData.objectID, pugDatabaseBank, itemObjectData.variation), itemObjectData, pugDatabaseBank, durabilityLookup);
	}

	public static bool ItemIsBroken(Entity equipmentPrefab, ObjectDataCD itemObjectData, BlobAssetReference<PugDatabase.PugDatabaseBank> pugDatabaseBank, ComponentLookup<DurabilityCD> durabilityLookup)
	{
		if (equipmentPrefab == Entity.Null || itemObjectData.amount > 0 || !durabilityLookup.HasComponent(equipmentPrefab))
		{
			return false;
		}
		return !PugDatabase.GetEntityObjectInfo(itemObjectData.objectID, pugDatabaseBank, itemObjectData.variation).isStackable;
	}

	public bool HeldItemIsMortar()
	{
		if (EntityUtility.TryGetComponentData<AimIndicatorCachedStatesCD>(base.entity, base.world, out var value))
		{
			return value.isMortar;
		}
		return false;
	}

	private void UpdateHungerEmote()
	{
		int hunger = hungerComponent.hunger;
		bool flag = hunger != previousHunger;
		if (flag || hunger == 0)
		{
			if (!isDyingOrDead && ((flag && hunger < 25 && hunger % 4 == 0) || ((!hungerEmoteTimer.isRunning || hungerEmoteTimer.isTimerElapsed) && hunger == 0)))
			{
				hungerEmoteTimer.Start(180f);
				Emote.SpawnEmoteText(center, Emote.EmoteType.Hungry);
			}
			previousHunger = hungerComponent.hunger;
		}
	}

	public override void UpdateHealthChangeAnimations(in HealthCD healthCD, BufferLookup<SummarizedConditionEffectsBuffer> summarizedConditionsEffectsBufferLookup)
	{
		if (isLocal)
		{
			Manager.saves.SetMaxHealth(GetMaxHealth());
		}
		base.UpdateHealthChangeAnimations(in healthCD, summarizedConditionsEffectsBufferLookup);
	}

	public void PlayTakeDamageEffect()
	{
		OnTakeDamage();
	}

	public void PlayTakeBarrierDamageEffect()
	{
		OnTakeBarrierDamage();
	}

	public static void AddHunger(int amount, in PlayerStateCD playerStateCD, ref HungerCD hungerCD)
	{
		PlayerStateEnum currentState = playerStateCD.currentState;
		if (currentState != PlayerStateEnum.NoClip && currentState != PlayerStateEnum.Death)
		{
			hungerCD.hunger = math.clamp(hungerCD.hunger + amount, 0, 100);
		}
	}

	public void KillThroughStuckOption()
	{
		playerCommandSystem.Unstuck(base.entity);
	}

	public void AE_StarExplo()
	{
		Manager.effects.PlayPuff(PuffID.SlowStarPoof, base.transform.position, 16);
	}

	public void AE_CatchSpoon()
	{
		if (isLocal)
		{
			inputModule.RumbleNow();
		}
		AudioManager.SfxFollowTransform(SfxID.whip, base.transform, 0.25f, 0.95f, 0.1f);
		AudioManager.SfxFollowTransform(SfxID.ridiculous, base.transform, 0.55f, 0.9f, 0.05f);
		AudioManager.SfxFollowTransform(SfxID.ridiculous, base.transform, 0.65f, 1.05f, 0.05f);
	}

	public void AE_AngryLand()
	{
		AudioManager.SfxMono(SfxID.punch, 1f, 0.6f, 0.1f);
		Manager.effects.PlayPuff(PuffID.SlowCircleDust, base.transform.position + new Vector3(0f, -0.5f), 4);
	}

	public void AE_Plop()
	{
		AudioManager.SfxFollowTransform(SfxID.ridiculous, base.transform, 1f, 0.6f, 0.1f);
	}

	public void AE_UhOh()
	{
		AudioManager.SfxFollowTransform(SfxID.uhoh, base.transform);
	}

	public void AE_Whip(int backwards)
	{
		AudioManager.SfxFollowTransform((backwards != 0) ? SfxID.whip_backwards : SfxID.whip, base.transform);
	}

	public void AE_DarkGleam()
	{
		AudioManager.SfxFollowTransform(SfxID.darkgleam, base.transform);
	}

	public void AE_FootStep()
	{
		float num = (((double)targetMovementVelocity.sqrMagnitude > 0.1) ? 0.1f : 0.02f);
		num += PugRandom.GenerateUniform(0f, 0.1f);
		if (onSlime && !onGlass)
		{
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.slimeFootstep, base.transform, volume, 1.15f, 0.3f);
		}
		if (onGlass)
		{
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			AudioManager.Sfx(SfxTableID.footstepGlass, base.transform.position);
		}
		else if (onOrangeSlime)
		{
			Manager.effects.PlayPuff(PuffID.SlimeFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.FootstepSlime, new Vector3(footStepTransform.position.x, 0.02f, footStepTransform.position.z), 0.25f, 0.5f);
		}
		else if (onAcid)
		{
			Manager.effects.PlayPuff(PuffID.AcidFootstep, footStepTransform.position, 12);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.FootstepAcid, new Vector3(footStepTransform.position.x, 0.02f, footStepTransform.position.z), 0.25f, 0.5f);
		}
		else if (onPoisonSlime)
		{
			Manager.effects.PlayPuff(PuffID.PoisonFootstep, footStepTransform.position, 12);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.FootstepAcid, new Vector3(footStepTransform.position.x, 0.02f, footStepTransform.position.z), 0.25f, 0.5f);
		}
		else if (onSlipperySlime)
		{
			Manager.effects.PlayPuff(PuffID.SlipperySlimeFootstep, footStepTransform.position, 12);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.FootstepBlueSplat, new Vector3(footStepTransform.position.x, 0.02f, footStepTransform.position.z), 0.25f, 0.5f);
		}
		else if (onWood)
		{
			num *= 0.6f;
			Manager.effects.PlayPuff(PuffID.DirtFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.chestclose, base.transform, volume, 1.2f, 0.15f);
			volume = num;
			AudioManager.SfxFollowTransform(SfxID.shoop, base.transform, volume, 0.75f, 0.3f);
		}
		else if (onGrass || onMoss)
		{
			Manager.effects.PlayPuff(PuffID.GrassFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Grass, base.transform, volume, 1f, 0.2f);
		}
		else if (onChrysalis)
		{
			num *= 0.6f;
			Manager.effects.PlayPuff(PuffID.DirtFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.woodenDestructable, base.transform, volume, 1.8f, 0.2f);
			volume = num;
			AudioManager.SfxFollowTransform(SfxID.nom1, base.transform, volume, 1.6f, 0.3f);
			volume = num * 0.6f;
			AudioManager.SfxFollowTransform(SfxID.shoop, base.transform, volume, 0.75f, 0.3f);
		}
		else if (onStone)
		{
			Manager.effects.PlayPuff(PuffID.StoneFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			AudioManager.Sfx(SfxTableID.footstepStone, base.transform.position, num);
		}
		else if (onFlesh)
		{
			num *= 0.6f;
			Manager.effects.PlayPuff(PuffID.BloodFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Larva, base.transform, volume, 1f, 0.2f);
		}
		else if (onSand)
		{
			Manager.effects.PlayPuff(PuffID.SandFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Sand, base.transform, volume, 1f, 0.2f);
		}
		else if (onBeach)
		{
			Manager.effects.PlayPuff(PuffID.BeachFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Sand, base.transform, volume, 1f, 0.2f);
		}
		else if (onOasis)
		{
			Manager.effects.PlayPuff(PuffID.OasisFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Sand, base.transform, volume, 1f, 0.2f);
		}
		else if (onClay)
		{
			num *= 0.5f;
			Manager.effects.PlayPuff(PuffID.ClayFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Clay, base.transform, volume, 1f, 0.1f);
		}
		else if (onMold)
		{
			num *= 0.5f;
			Manager.effects.PlayPuff(PuffID.DirtFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Clay, base.transform, volume, 1f, 0.1f);
		}
		else if (onDirt)
		{
			Manager.effects.PlayPuff(PuffID.DirtFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Dirt, base.transform, volume, 1f, 0.2f);
		}
		else if (onMetal)
		{
			Manager.effects.PlayPuff(PuffID.DirtFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.shoop, base.transform, volume, 0.75f, 0.3f);
		}
		else if (onRoses)
		{
			Manager.effects.PlayPuff(PuffID.RoseFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.Footstep_Grass, base.transform, volume, 1f, 0.2f);
		}
		else if (onMeadow)
		{
			Manager.effects.PlayPuff(PuffID.SandFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			AudioManager.Sfx(SfxTableID.footstepMeadow, base.transform.position);
		}
		else
		{
			Manager.effects.PlayPuff(PuffID.DirtFootstep, footStepTransform.position, 6);
			Manager.effects.PlayTempSprite(SpriteTempEffectID.Footstep, new Vector3(footStepTransform.position.x, 0.01f, footStepTransform.position.z), 2f, 3f);
			float volume = num;
			AudioManager.SfxFollowTransform(SfxID.shoop, base.transform, volume, 0.75f, 0.3f);
		}
	}

	public void AE_TakeDamage()
	{
		AudioManager.SfxMono(SfxID.mjeh);
		AudioManager.SfxMono(SfxID.bullethit, 1f, 1f, 0f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, muteVolumeWhilePaused: false, playOnGamepad: true);
		Manager.effects.PlayPuff(PuffID.HitStarPoof, base.transform.position + new Vector3(0f, -0.5f), 4);
		Manager.input.singleplayerInputModule.FlashGamepadOnHit();
	}

	private void AE_PickUpSound()
	{
		AudioManager.SfxFollowTransform(SfxID.fat, base.transform, 0.8f, 1f, 0.1f, reuse: false, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: false, 16f, 10f, muteVolumeWhilePaused: true, freeAudioSourceAfterItStoppedPlaying: true, playOnGamepad: true);
	}

	public void AE_HidePlayer()
	{
		animator.SetTrigger(1914833150);
	}

	public void PlayEquipmentBreakSound()
	{
		AudioManager.SfxFollowTransform(SfxID.anvil, Manager.main.player.transform, 0.5f, 2f, 0.1f, reuse: true);
		AudioManager.SfxFollowTransform(SfxID.toolbreak, Manager.main.player.transform, 1f, 1f, 0.1f, reuse: true, AudioManager.MixerGroupEnum.EFFECTS, ignoreAudioIfOutsideOfViewport: false, useSpatialSound: true, loop: false, 16f, 10f, muteVolumeWhilePaused: true, freeAudioSourceAfterItStoppedPlaying: true, playOnGamepad: true);
		Manager.effects.PlayPuff(PuffID.DirtBlockDust, Manager.main.player.transform.position + new Vector3(0f, 0.4f, -0.25f), 20);
		Manager.effects.PlayPuff(PuffID.PotDebris, Manager.main.player.transform.position + new Vector3(0f, 0.4f, -0.25f), 12);
	}

	public void AE_BlinkEyes()
	{
		AudioManager.Sfx(SfxTableID.playerBlinkEyes, base.transform.position);
	}
}
