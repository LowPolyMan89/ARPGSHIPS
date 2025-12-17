namespace Ships
{
	[System.Serializable]
	public class StatModifierVisual
	{
		public StatModifierType Type;
		public StatModifierTarget Target;
		public StatModifierPeriodicity Periodicity;
		public float Value;
		public int RemainingTicks;
		public string SourceName;
	}
}
