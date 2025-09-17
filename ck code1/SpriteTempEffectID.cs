using UnityEngine;

public static class SpriteTempEffectID
{
	public enum ID
	{
		ExploDiscID,
		Splash,
		Flash,
		Footstep,
		FootstepSlime,
		FootstepAcid,
		FootstepPoison,
		FootstepIce,
		WaterSplash,
		WaterSplashYellow,
		WaterSplashMold,
		WaterSplashLava,
		WaterRipple,
		AcidImpact,
		AcidSplat,
		AcidSplat2,
		BloodImpact,
		BloodSplat,
		SmallBloodSplat,
		SmallSlimeSplat,
		SmallLarvaSplat,
		BlueSplat,
		BlueSplat2,
		BlueImpact,
		MoldSplat2,
		HitEffect,
		PoisonSplat,
		SnowSplat,
		FootstepBlueSplat,
		WaterRippleLava,
		WaterRippleWhite,
		WaterRippleYellow
	}

	public static readonly int ExploDisc = Animator.StringToHash("ExploDisc");

	public static readonly int Splash = Animator.StringToHash("Splash");

	public static readonly int Flash = Animator.StringToHash("Flash");

	public static readonly int Footstep = Animator.StringToHash("Footstep");

	public static readonly int FootstepSlime = Animator.StringToHash("FootstepSlime");

	public static readonly int FootstepAcid = Animator.StringToHash("FootstepAcid");

	public static readonly int FootstepPoison = Animator.StringToHash("FootstepPoison");

	public static readonly int BigSplash = Animator.StringToHash("BigSplash");

	public static readonly int WaterSplash = Animator.StringToHash("WaterSplash");

	public static readonly int WaterSplashYellow = Animator.StringToHash("WaterSplashYellow");

	public static readonly int WaterSplashMold = Animator.StringToHash("WaterSplashMold");

	public static readonly int WaterSplashLava = Animator.StringToHash("WaterSplashLava");

	public static readonly int WaterRipple = Animator.StringToHash("WaterRipple");

	public static readonly int AcidImpact = Animator.StringToHash("AcidImpact");

	public static readonly int AcidSplat = Animator.StringToHash("AcidSplat");

	public static readonly int AcidSplat2 = Animator.StringToHash("AcidSplat2");

	public static readonly int BloodImpact = Animator.StringToHash("BloodImpact");

	public static readonly int BloodSplat = Animator.StringToHash("BloodSplat");

	public static readonly int SmallBloodSplat = Animator.StringToHash("SmallBloodSplat");

	public static readonly int SmallSlimeSplat = Animator.StringToHash("SmallSlimeSplat");

	public static readonly int SmallLarvaSplat = Animator.StringToHash("SmallLarvaSplat");

	public static readonly int BlueSplat = Animator.StringToHash("BlueSplat");

	public static readonly int BlueSplat2 = Animator.StringToHash("BlueSplat2");

	public static readonly int BlueImpact = Animator.StringToHash("BlueImpact");

	public static readonly int MoldSplat2 = Animator.StringToHash("MoldSplat2");

	public static readonly int HitEffect = Animator.StringToHash("HitEffect");

	public static readonly int PoisonSplat = Animator.StringToHash("PoisonSplat");

	public static readonly int SnowSplat = Animator.StringToHash("SnowSplat");

	public static readonly int FootstepBlueSplat = Animator.StringToHash("FootstepBlueSplat");

	public static readonly int WaterRippleLava = Animator.StringToHash("WaterRippleLava");

	public static readonly int WaterRippleWhite = Animator.StringToHash("WaterRippleWhite");

	public static readonly int WaterRippleYellow = Animator.StringToHash("WaterRippleYellow");

	public static int GetHash(ID id)
	{
		return id switch
		{
			ID.ExploDiscID => ExploDisc, 
			ID.Splash => Splash, 
			ID.Flash => Flash, 
			ID.Footstep => Footstep, 
			ID.FootstepSlime => FootstepSlime, 
			ID.FootstepAcid => FootstepAcid, 
			ID.FootstepPoison => FootstepPoison, 
			ID.WaterSplash => WaterSplash, 
			ID.WaterSplashYellow => WaterSplashYellow, 
			ID.WaterSplashMold => WaterSplashMold, 
			ID.WaterSplashLava => WaterSplashLava, 
			ID.WaterRipple => WaterRipple, 
			ID.AcidImpact => AcidImpact, 
			ID.AcidSplat => AcidSplat, 
			ID.AcidSplat2 => AcidSplat2, 
			ID.BloodImpact => BloodImpact, 
			ID.BloodSplat => BloodSplat, 
			ID.SmallBloodSplat => SmallBloodSplat, 
			ID.SmallSlimeSplat => SmallSlimeSplat, 
			ID.SmallLarvaSplat => SmallLarvaSplat, 
			ID.BlueSplat => BlueSplat, 
			ID.BlueSplat2 => BlueSplat2, 
			ID.BlueImpact => BlueImpact, 
			ID.MoldSplat2 => MoldSplat2, 
			ID.HitEffect => HitEffect, 
			ID.PoisonSplat => PoisonSplat, 
			ID.SnowSplat => SnowSplat, 
			ID.FootstepBlueSplat => FootstepBlueSplat, 
			ID.WaterRippleLava => WaterRippleLava, 
			ID.WaterRippleWhite => WaterRippleWhite, 
			ID.WaterRippleYellow => WaterRippleYellow, 
			_ => -1, 
		};
	}

	public static void Init()
	{
	}
}
