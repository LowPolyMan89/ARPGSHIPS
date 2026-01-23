using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Ships
{
	[Serializable]
	public sealed class AiConfig
	{
		public AiTargetingConfig Targeting = new();
		public AiMovementConfig Movement = new();
		public AiClassConfig[] Classes = Array.Empty<AiClassConfig>();
		public AiTargetValueEntry[] TargetValues = Array.Empty<AiTargetValueEntry>();
		public AiWeaponSizeMultiplier[] WeaponSizeMultipliers = Array.Empty<AiWeaponSizeMultiplier>();
		public AiFocusLimitEntry[] FocusLimits = Array.Empty<AiFocusLimitEntry>();

		public AiClassConfig GetClassConfig(ShipClass shipClass)
		{
			if (Classes == null || Classes.Length == 0)
				return new AiClassConfig { Class = shipClass.ToString() };

			for (var i = 0; i < Classes.Length; i++)
			{
				var cfg = Classes[i];
				if (cfg != null && cfg.Matches(shipClass))
					return cfg;
			}

			return new AiClassConfig { Class = shipClass.ToString() };
		}

		public float GetTargetValue(ShipClass shipClass)
		{
			if (TargetValues == null)
				return 0f;

			for (var i = 0; i < TargetValues.Length; i++)
			{
				var entry = TargetValues[i];
				if (entry != null && entry.Matches(shipClass))
					return entry.Value;
			}

			return 0f;
		}

		public AiFocusLimitEntry GetFocusLimit(ShipClass shipClass)
		{
			if (FocusLimits == null)
				return new AiFocusLimitEntry { Class = shipClass.ToString(), Min = 1, Max = 1 };

			for (var i = 0; i < FocusLimits.Length; i++)
			{
				var entry = FocusLimits[i];
				if (entry != null && entry.Matches(shipClass))
					return entry;
			}

			return new AiFocusLimitEntry { Class = shipClass.ToString(), Min = 1, Max = 1 };
		}

		public float GetWeaponSizeMultiplier(string size)
		{
			if (string.IsNullOrEmpty(size) || WeaponSizeMultipliers == null)
				return 1f;

			for (var i = 0; i < WeaponSizeMultipliers.Length; i++)
			{
				var entry = WeaponSizeMultipliers[i];
				if (entry != null && entry.Matches(size))
					return entry.GetRandom();
			}

			return 1f;
		}
	}

	[Serializable]
	public sealed class AiTargetingConfig
	{
		public float SwitchThreshold = 3f;
		public float AttackerMemorySeconds = 5f;
		public float ThreatMissionBonus = 4f;
		public float ThreatHeavyLightBonus = 3f;
		public float ThreatAttackingBonus = 2f;
		public float LowHpThreshold = 0.3f;
		public float LowHpBonus = 2f;
		public float CriticalHpThreshold = 0.1f;
		public float CriticalHpBonus = 4f;
		public float OptimalDistanceBonus = 1f;
		public float TooFarPenalty = -2f;
		public float OptimalDistanceTolerance = 0.15f;
		public float TooFarFactor = 1.4f;
		public float TooCloseFactor = 0.4f;
		public float OverkillPenalty = -999f;
	}

	[Serializable]
	public sealed class AiMovementConfig
	{
		public float NavSampleRadius = 5f;
		public float OrbitAngleMin = 30f;
		public float OrbitAngleMax = 80f;
		public float FallbackRange = 5f;
		public float OrbitDistanceFactor = 1f;
		public float StrafeDistanceFactor = 1f;
		public float BackstepDistanceFactor = 1.2f;
		public float FlankDistanceFactor = 1f;
	}

	[Serializable]
	public sealed class AiClassConfig
	{
		public string Class;
		public float AggroRange = 0f;
		public float ReevalMin = 3f;
		public float ReevalMax = 5f;
		public float RepositionChance = 0.35f;
		public float RepositionMin = 1.5f;
		public float RepositionMax = 2.5f;
		public float BackstepHpThreshold = 0.3f;
		public float EscortDistance = 8f;
		public float FlankAngleMin = 20f;
		public float FlankAngleMax = 40f;
		public float OrbitWeight = 0.4f;
		public float StrafeWeight = 0.3f;
		public float BackstepWeight = 0.2f;
		public float FlankWeight = 0.1f;
		public float MaxChaseFactor = 1.6f;

		public bool Matches(ShipClass shipClass)
		{
			if (string.IsNullOrEmpty(Class))
				return false;

			return Class.Equals(shipClass.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		public float GetReevalInterval()
		{
			return UnityEngine.Random.Range(Mathf.Min(ReevalMin, ReevalMax), Mathf.Max(ReevalMin, ReevalMax));
		}

		public float GetRepositionDuration()
		{
			return UnityEngine.Random.Range(Mathf.Min(RepositionMin, RepositionMax), Mathf.Max(RepositionMin, RepositionMax));
		}
	}

	[Serializable]
	public sealed class AiTargetValueEntry
	{
		public string Class;
		public float Value = 1f;

		public bool Matches(ShipClass shipClass)
		{
			if (string.IsNullOrEmpty(Class))
				return false;

			return Class.Equals(shipClass.ToString(), StringComparison.OrdinalIgnoreCase);
		}
	}

	[Serializable]
	public sealed class AiWeaponSizeMultiplier
	{
		public string Size;
		public float Min = 1f;
		public float Max = 1f;

		public bool Matches(string size)
		{
			if (string.IsNullOrEmpty(Size) || string.IsNullOrEmpty(size))
				return false;

			return Size.Equals(size, StringComparison.OrdinalIgnoreCase) ||
			       Size.StartsWith(size, StringComparison.OrdinalIgnoreCase) ||
			       size.StartsWith(Size, StringComparison.OrdinalIgnoreCase);
		}

		public float GetRandom()
		{
			return UnityEngine.Random.Range(Mathf.Min(Min, Max), Mathf.Max(Min, Max));
		}
	}

	[Serializable]
	public sealed class AiFocusLimitEntry
	{
		public string Class;
		public int Min = 1;
		public int Max = 1;

		public bool Matches(ShipClass shipClass)
		{
			if (string.IsNullOrEmpty(Class))
				return false;

			return Class.Equals(shipClass.ToString(), StringComparison.OrdinalIgnoreCase);
		}

		public int GetRandom()
		{
			return UnityEngine.Random.Range(Mathf.Min(Min, Max), Mathf.Max(Min, Max) + 1);
		}
	}

	public static class AiConfigLoader
	{
		private static AiConfig _cached;

		public static AiConfig Load()
		{
			if (_cached != null)
				return _cached;

			var path = Path.Combine(PathConstant.AiConfigs, "AiConfig.json");
			if (ResourceLoader.TryLoadStreamingJson(path, out AiConfig config) && config != null)
			{
				_cached = config;
				return _cached;
			}

			_cached = new AiConfig();
			return _cached;
		}
	}
}
