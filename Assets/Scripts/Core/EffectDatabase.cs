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
			var minDmg = EffectStatUtils.GetStat(value, "MinDamage");
			var maxDmg = EffectStatUtils.GetStat(value, "MaxDamage");
			var duration = EffectStatUtils.GetStat(value, "Duration");
			//var stack = EffectStatUtils.GetStat(value, "MaxStacks");
			var dmgPerTick = Random.Range(minDmg, maxDmg);

			return new HitEffect.DamageOverTimeEffect(
				chance: chance,
				damagePerTick: dmgPerTick,
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
