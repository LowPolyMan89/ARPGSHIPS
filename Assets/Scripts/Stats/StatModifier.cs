namespace Ships
{
	public sealed class StatModifier
	{
		public StatModifierType Type { get; }
		public StatModifierTarget Target { get; }
		public StatModifierPeriodicity Periodicity { get; }
		public StatModifierSource SourceType { get; }

		/// <summary>
		/// Величина модификатора:
		/// Flat: +X
		/// PercentAdd/PercentMult: X = 0.1f => +10%
		/// Set: новое значение.
		/// </summary>
		public float Value { get; }

		public int RemainingTicks { get; set; }
		public object Source { get; }

		public StatModifier(
			StatModifierType type,
			StatModifierTarget target,
			StatModifierPeriodicity periodicity,
			float value,
			int remainingTicks = 0,
			object source = null,
			StatModifierSource sourceType = StatModifierSource.Buff)
		{
			Type = type;
			Target = target;
			Periodicity = periodicity;
			Value = value;
			RemainingTicks = remainingTicks;
			Source = source;
			SourceType = sourceType;
		}
	}
}
