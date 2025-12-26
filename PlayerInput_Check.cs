using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pug.UnityExtensions;
using Rewired;
using Rewired.ControllerExtensions;
using Unity.Mathematics;
using UnityEngine;

public class PlayerInput
{
	public enum InputType
	{
		INTERACT = 2,
		SECOND_INTERACT = 3,
		NEXT_SLOT = 17,
		CANCEL = 15,
		EQUIPSLOT1 = 46,
		EQUIPSLOT2 = 19,
		EQUIPSLOT3 = 20,
		EQUIPSLOT4 = 21,
		EQUIPSLOT5 = 48,
		EQUIPSLOT6 = 49,
		EQUIPSLOT7 = 50,
		EQUIPSLOT8 = 51,
		EQUIPSLOT9 = 52,
		EQUIPSLOT10 = 53,
		PREVIOUS_SLOT = 47,
		TOGGLE_INVENTORY = 54,
		TOGGLE_MAP = 55,
		MAP_PING = 110,
		UI_INTERACT = 105,
		UI_SECOND_INTERACT = 106,
		DROP_SELECTED_ITEM = 87,
		INTERACT_WITH_OBJECT = 68,
		QUICK_MOVE_ITEMS = 90,
		PICK_UP_ALL_ITEMS = 108,
		PICK_UP_ITEMS = 107,
		PICK_UP_10 = 98,
		PICK_UP_HALF = 99,
		ZOOM_IN_MAP = 92,
		ZOOM_OUT_MAP = 93,
		OPEN_CHAT = 94,
		QUICK_STACK = 111,
		SORT = 109,
		QUICK_SWAP_TORCH = 101,
		MOVE_FASTER = 218,
		TOGGLE_SPECTATED_PLAYER = 104,
		USE_OFF_HAND = 112,
		SELECT_NEXT_MAP_MARKER = 113,
		SELECT_PREVIOUS_MAP_MARKER = 114,
		ACCELERATE_VEHICLE = 115,
		REVERSE_VEHICLE = 116,
		HONK = 117,
		ROTATE = 207,
		TOGGLE_UI = 208,
		TRASH_ITEM = 209,
		TOGGLE_SHORTCUTS_WINDOW = 214,
		EQUIP_PRESET_1 = 215,
		EQUIP_PRESET_2 = 216,
		EQUIP_PRESET_3 = 217,
		HOT_BAR_SWAP_MODIFIER = 225,
		SWAP_NEXT_HOTBAR = 304,
		SWAP_PREVIOUS_HOTBAR = 305,
		LOCKING_TOGGLE = 226,
		TOUCHPAD = 228,
		MENU_UP = 211,
		MENU_DOWN = 210,
		MENU_LEFT = 212,
		MENU_RIGHT = 213,
		C1_NOTE = 178,
		C1S_NOTE = 179,
		D1_NOTE = 180,
		D1S_NOTE = 181,
		E1_NOTE = 182,
		F1_NOTE = 183,
		F1S_NOTE = 184,
		G1_NOTE = 185,
		G1S_NOTE = 186,
		A1_NOTE = 187,
		A1S_NOTE = 188,
		B1_NOTE = 189,
		C2_NOTE = 192,
		C2S_NOTE = 193,
		D2_NOTE = 194,
		D2S_NOTE = 195,
		E2_NOTE = 196,
		F2_NOTE = 197,
		F2S_NOTE = 198,
		G2_NOTE = 199,
		G2S_NOTE = 200,
		A2_NOTE = 201,
		A2S_NOTE = 202,
		B2_NOTE = 203,
		STOP_PLAYING_INSTRUMENT = 190,
		OCTAVE_CHANGE = 191
	}

	public enum InputAxisType
	{
		CHARACTER_MOVEMENT_HORIZONTAL = 292,
		CHARACTER_MOVEMENT_VERTICAL,
		CHARACTER_AIM_HORIZONTAL,
		CHARACTER_AIM_VERTICAL,
		MAP_MOVEMENT_HORIZONTAL,
		MAP_MOVEMENT_VERTICAL
	}

	public enum RumbleInstanceId : byte
	{
		None,
		DrillTools
	}

	private class RumbleInstance
	{
		public RumbleInstanceId InstanceId;

		public AnimationCurve curve;

		protected TimerSimple timer;

		public float intensityScale = 1f;

		public RumbleInstance(AnimationCurve rumbleCurve, float rumbleTime, float rumbleIntensityScale, bool useUnscaledTime = true, RumbleInstanceId instanceId = RumbleInstanceId.None)
		{
			timer = new TimerSimple(rumbleTime, useUnscaledTime);
			timer.Start();
			intensityScale = rumbleIntensityScale;
			curve = rumbleCurve;
			InstanceId = instanceId;
		}

		public bool HasElapsed()
		{
			return timer.isTimerElapsed;
		}

		public float GetIntensity(bool scaled = true)
		{
			return (scaled ? intensityScale : 1f) * curve.Evaluate(timer.elapsedRatio);
		}
	}

	public TimerSimple forceMovementTimer = new TimerSimple(0f);

	public Vector2 forceMovementDirection = Vector2.zero;

	protected TimerSimple disableInputTimer;

	protected bool forceMovement;

	public readonly Player rewiredPlayer;

	public int playerControllerIndex = -1;

	private Color gamepadColor = Color.blue;

	private float rumbleAcc;

	private List<RumbleInstance> rumbleInstances;

	public virtual string name => rewiredPlayer.name;

	public bool InputEnabled { get; private set; } = true;


	public float timeSinceAnyButtonPressed { get; private set; }

	public PlayerInput(Player rewiredPlayer)
	{
		playerControllerIndex = -1;
		rumbleInstances = new List<RumbleInstance>();
		disableInputTimer = new TimerSimple(0f, unscaled: true);
		this.rewiredPlayer = rewiredPlayer;
		SetGamepadLight(gamepadColor);
	}

	public bool PrefersKeyboardAndMouse()
	{
		Controller lastActiveController = rewiredPlayer.controllers.GetLastActiveController();
		if (lastActiveController != null && lastActiveController.type != 0 && lastActiveController.type != ControllerType.Mouse)
		{
			return Manager.input.touchpadInUse;
		}
		return true;
	}

	public virtual void UpdateState()
	{
		if (!InputEnabled && disableInputTimer.isRunning && disableInputTimer.isTimerElapsed)
		{
			disableInputTimer.Stop();
			InputEnabled = true;
		}
		rumbleAcc += Time.unscaledDeltaTime;
		if (rumbleAcc >= 0.0167f)
		{
			rumbleAcc = 0f;
			float rumbleIntensity = GetRumbleIntensity();
			if (!Manager.prefs.vibration || rumbleIntensity <= 0f)
			{
				rewiredPlayer.StopVibration();
			}
			else
			{
				SetRumble(rumbleIntensity);
			}
		}
		UpdateTimeSinceAnyButtonPressed();
	}

	private void UpdateTimeSinceAnyButtonPressed()
	{
		timeSinceAnyButtonPressed += Time.deltaTime;
		if (rewiredPlayer.GetAnyButton() || GetRawAxisInput().sqrMagnitude > 0.01f || GetRawAxisInput(discardDisabledInput: false, discardForceMovement: false, getRightJoystick: true).sqrMagnitude > 0.01f)
		{
			timeSinceAnyButtonPressed = 0f;
		}
	}

	private void AddNewRumbleInstance(AnimationCurve rumbleCurve, float rumbleTime, float rumbleIntensity, RumbleInstanceId instanceId = RumbleInstanceId.None)
	{
		int maxRumbleInstanceCount = Manager.input.maxRumbleInstanceCount;
		if (rumbleInstances.Count >= maxRumbleInstanceCount)
		{
			rumbleInstances.RemoveAt(0);
		}
		rumbleInstances.Add(new RumbleInstance(rumbleCurve, rumbleTime, rumbleIntensity, useUnscaledTime: true, instanceId));
	}

	public void RumbleNow(float rumbleTime = 0.18f, float rumbleIntensityScale = 1f, AnimationCurve rumbleIntensityCurve = null, RumbleInstanceId instanceId = RumbleInstanceId.None)
	{
		if (Manager.prefs.vibration)
		{
			AnimationCurve rumbleCurve = ((rumbleIntensityCurve == null) ? Manager.input.defaultQuickRumbleCurve : rumbleIntensityCurve);
			AddNewRumbleInstance(rumbleCurve, rumbleTime, rumbleIntensityScale, instanceId);
		}
	}

	public void RemoveRumbleInstance(RumbleInstanceId instanceId)
	{
		if ((int)instanceId <= 0)
		{
			Debug.LogWarning("PlayerInput.RemoveRumbleInstance: can't remove a rumble instance with instance if of 0. Value must be between 1 and 255.");
			return;
		}
		RumbleInstance rumbleInstance = rumbleInstances.FirstOrDefault((RumbleInstance x) => x.InstanceId == instanceId);
		if (rumbleInstance != null)
		{
			rumbleInstances.Remove(rumbleInstance);
		}
	}

	private float GetRumbleIntensity()
	{
		if (!Manager.prefs.vibration || rumbleInstances == null || rumbleInstances.Count == 0)
		{
			return 0f;
		}
		while (rumbleInstances.Count > 0 && rumbleInstances[0].HasElapsed())
		{
			rumbleInstances.RemoveAt(0);
		}
		float num = 0f;
		foreach (RumbleInstance rumbleInstance in rumbleInstances)
		{
			num += rumbleInstance.GetIntensity();
		}
		float num2 = rumbleInstances.Count;
		return Mathf.Clamp(num / Mathf.Log(num2 + 1f, 2f), 0f, 1f);
	}

	public virtual Vector2 GetRawAxisInput(bool discardDisabledInput = false, bool discardForceMovement = false, bool getRightJoystick = false)
	{
		if (!discardForceMovement && forceMovement)
		{
			return forceMovementDirection;
		}
		if (discardDisabledInput || InputEnabled)
		{
			if (getRightJoystick)
			{
				return rewiredPlayer.GetAxis2D(59, 60);
			}
			return rewiredPlayer.GetAxis2D(0, 1);
		}
		return Vector2.zero;
	}

	public virtual Vector2 GetInputAxisValue(InputAxisType horizontalAxisType, InputAxisType verticalAxisType)
	{
		if (InputEnabled)
		{
			return rewiredPlayer.GetAxis2D((int)horizontalAxisType, (int)verticalAxisType);
		}
		return Vector2.zero;
	}

	public virtual float GetInputAxisValue(InputAxisType axisType)
	{
		if (InputEnabled)
		{
			return rewiredPlayer.GetAxis((int)axisType);
		}
		return 0f;
	}

	public virtual float GetYAxisInput(InputType inputType)
	{
		if (InputEnabled)
		{
			return rewiredPlayer.GetAxis2D(0, (int)inputType).y;
		}
		return 0f;
	}

	public virtual bool IsButtonCurrentlyDown(InputType inputType, bool discardDisabledInput = false)
	{
		if (InputEnabled || discardDisabledInput)
		{
			return rewiredPlayer.GetButton((int)inputType);
		}
		return false;
	}

	public virtual bool IsSlotButtonCurrentlyDown(int slotIndex, bool discardDisabledInput = false)
	{
		switch (slotIndex)
		{
		case 0:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT1);
		case 1:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT2);
		case 2:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT3);
		case 3:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT4);
		case 4:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT5);
		case 5:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT6);
		case 6:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT7);
		case 7:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT8);
		case 8:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT9);
		case 9:
			return IsButtonCurrentlyDown(InputType.EQUIPSLOT10);
		default:
			Debug.LogError("Key bind slot " + slotIndex + " not bound.");
			return false;
		}
	}

	public virtual bool WasButtonPressedDownThisFrame(InputType inputType, bool discardDisabledInput = false)
	{
		if (InputEnabled || discardDisabledInput)
		{
			return rewiredPlayer.GetButtonDown((int)inputType);
		}
		return false;
	}

	public virtual bool WasSlotButtonPressedDownThisFrame(int index, bool discardDisabledInput = false)
	{
		switch (index)
		{
		case 0:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT1);
		case 1:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT2);
		case 2:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT3);
		case 3:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT4);
		case 4:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT5);
		case 5:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT6);
		case 6:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT7);
		case 7:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT8);
		case 8:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT9);
		case 9:
			return WasButtonPressedDownThisFrame(InputType.EQUIPSLOT10);
		default:
			Debug.LogError("Key bind slot " + index + " not bound.");
			return false;
		}
	}

	public virtual bool WasButtonReleasedThisFrame(InputType inputType, bool discardDisabledInput = false)
	{
		if (InputEnabled || discardDisabledInput)
		{
			return rewiredPlayer.GetButtonUp((int)inputType);
		}
		return false;
	}

	public void DisableInputFor(float duration = -1f)
	{
		if (!InputEnabled && duration > 0f)
		{
			duration = ((!disableInputTimer.isRunning) ? (-1f) : math.max(duration, disableInputTimer.remainingTime));
		}
		if (duration > 0f)
		{
			disableInputTimer.Start(duration);
		}
		else
		{
			disableInputTimer.Stop();
		}
		InputEnabled = false;
	}

	public void EnableInput()
	{
		InputEnabled = true;
	}

	public bool ActionsAreUsingTheSameInput(InputType action1, InputType action2)
	{
		Controller lastActiveController = rewiredPlayer.controllers.GetLastActiveController();
		List<ActionElementMap> list = new List<ActionElementMap>();
		rewiredPlayer.controllers.maps.GetButtonMapsWithAction(lastActiveController, (int)action1, skipDisabledMaps: true, list);
		List<ActionElementMap> list2 = new List<ActionElementMap>();
		rewiredPlayer.controllers.maps.GetButtonMapsWithAction(lastActiveController, (int)action2, skipDisabledMaps: true, list2);
		foreach (ActionElementMap item in list)
		{
			foreach (ActionElementMap item2 in list2)
			{
				if (item.elementIdentifierId == item2.elementIdentifierId)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void SetRumble(float rumbleIntensity)
	{
		if (rumbleIntensity != 0f)
		{
			rumbleIntensity *= Manager.prefs.vibrationIntensity;
			rewiredPlayer.SetVibration(0, rumbleIntensity);
			rewiredPlayer.SetVibration(1, rumbleIntensity);
			rewiredPlayer.SetVibration(2, rumbleIntensity);
		}
	}

	public void SetGamepadLight(Color color)
	{
		IDualShock4Extension dS4Extension = GetDS4Extension();
		if (dS4Extension != null)
		{
			dS4Extension.SetLightColor(color);
			gamepadColor = color;
		}
	}

	public void ResetGamepadLight()
	{
		GetDS4Extension()?.SetLightColor(gamepadColor);
	}

	private IDualShock4Extension GetDS4Extension()
	{
		return GetLastActiveJoystick()?.GetExtension<IDualShock4Extension>();
	}

	public Joystick GetLastActiveJoystick()
	{
		Controller lastActiveController = rewiredPlayer.controllers.GetLastActiveController();
		foreach (Joystick joystick in rewiredPlayer.controllers.Joysticks)
		{
			if (joystick != null && joystick.isConnected && joystick.enabled && lastActiveController != null && lastActiveController.id == joystick.id)
			{
				return joystick;
			}
		}
		if (rewiredPlayer.controllers.Joysticks.Count > 0)
		{
			return rewiredPlayer.controllers.Joysticks[0];
		}
		return null;
	}

	public void FlashGamepadOnHit()
	{
		IDualShock4Extension dS4Extension = GetDS4Extension();
		if (dS4Extension != null)
		{
			Manager.StartIEnumerator(FlashGamepadOnHit(dS4Extension));
		}
	}

	private IEnumerator FlashGamepadOnHit(IDualShock4Extension extension)
	{
		for (int i = 0; i < 5; i++)
		{
			extension.SetLightColor(Color.white);
			yield return new WaitForSeconds(0.03f);
			extension.SetLightColor(Color.red);
			yield return new WaitForSeconds(0.03f);
		}
		extension.SetLightColor(gamepadColor);
	}
}
