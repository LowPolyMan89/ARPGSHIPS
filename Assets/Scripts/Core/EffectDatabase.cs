using System.Collections.Generic;
using Ships.HitEffect;
using System.IO;
using UnityEngine;

namespace Ships
{
	public static class EffectStatUtils
	{
		public static float GetStat(
			EffectValue value,
			string name,
			float defaultValue = 0f)
		{
			if (value?.Stats == null)
				return defaultValue;

			foreach (var s in value.Stats)
				if (s.Name == name)
					return s.Value;

			return defaultValue;
		}
	}

	
	public static class EffectFactory
	{
		public static IOnHitEffect Create(EffectValue value)
		{
			var tpl = EffectDatabase.Get(value.Name);
			if (tpl == null)
			{
				Debug.LogError($"Effect template not found: {value.Name}");
				return null;
			}

			return tpl.Script switch
			{
				"DotFireDamage" => CreateDot(value),
				_ => null
			};
		}

		private static IOnHitEffect CreateDot(EffectValue value)
		{
			var chance = EffectStatUtils.GetStat(value, "Chance");
			var damagePerTick = EffectStatUtils.GetStat(value, "DamagePerTick", float.NaN);
			var minDmg = EffectStatUtils.GetStat(value, "MinDamage");
			var maxDmg = EffectStatUtils.GetStat(value, "MaxDamage", minDmg);
			var duration = EffectStatUtils.GetStat(value, "Duration");
			//var stack = EffectStatUtils.GetStat(value, "MaxStacks");
			if (float.IsNaN(damagePerTick) || damagePerTick <= 0f)
			{
				if (minDmg > 0f && maxDmg > 0f && !Mathf.Approximately(minDmg, maxDmg))
					damagePerTick = (minDmg + maxDmg) * 0.5f;
				else
					damagePerTick = Mathf.Max(minDmg, maxDmg);
			}

			return new HitEffect.DamageOverTimeEffect(
				chance: chance,
				damagePerTick: damagePerTick,
				duration: duration,
				canStack: false,
				maxStacks: (int)1
			);
		}
	}
	
	public static class EffectDatabase
	{
		private static Dictionary<string, EffectTemplate> _effects;

		public static void Init()
		{
			_effects = new Dictionary<string, EffectTemplate>();

			foreach (var file in ResourceLoader.GetStreamingFiles(PathConstant.EffectsConfigs, "*.json"))
			{
				if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.EffectsConfigs, file), out EffectTemplateCollection collection))
					continue;

				foreach (var e in collection.Effects)
					_effects[e.Name] = e;
			}
		}

		public static EffectTemplate Get(string name)
		{
			if (_effects == null)
				Init();

			return _effects.TryGetValue(name, out var e) ? e : null;
		}
	}

}
