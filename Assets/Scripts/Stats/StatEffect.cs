using System.Collections.Generic;

namespace Ships
{
	public enum StatEffectOperation
	{
		Add,
		PercentAdd,
		PercentMult,
		Set
	}

	[System.Serializable]
	public sealed class StatEffectModel
	{
		public string Stat;
		public StatEffectOperation Operation = StatEffectOperation.Add;
		public float Value;
		public StatModifierTarget Target = StatModifierTarget.Maximum;
	}

	public static class StatEffectApplier
	{
		public static void ApplyAll(
			Stats stats,
			IEnumerable<StatEffectModel> effects,
			StatModifierSource sourceType,
			object sourceObject = null)
		{
			if (stats == null || effects == null)
				return;

			foreach (var effect in effects)
			{
				if (effect == null)
					continue;

				if (!TryCreateModifier(effect, sourceType, sourceObject, out var statType, out var modifier))
					continue;

				var stat = stats.GetOrCreateStat(statType, 0f);
				stat.AddModifier(modifier);
			}
		}

		private static bool TryCreateModifier(
			StatEffectModel effect,
			StatModifierSource sourceType,
			object sourceObject,
			out StatType statType,
			out StatModifier modifier)
		{
			if (effect == null || string.IsNullOrEmpty(effect.Stat))
			{
				statType = default;
				modifier = null;
				return false;
			}

			if (!System.Enum.TryParse(effect.Stat, true, out statType))
			{
				modifier = null;
				return false;
			}

			var modType = effect.Operation switch
			{
				StatEffectOperation.Add => StatModifierType.Flat,
				StatEffectOperation.PercentAdd => StatModifierType.PercentAdd,
				StatEffectOperation.PercentMult => StatModifierType.PercentMult,
				StatEffectOperation.Set => StatModifierType.Set,
				_ => StatModifierType.Flat
			};

			modifier = new StatModifier(
				modType,
				effect.Target,
				StatModifierPeriodicity.Permanent,
				effect.Value,
				source: sourceObject,
				sourceType: sourceType);
			return true;
		}
	}
}
