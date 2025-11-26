namespace Ships
{
	[System.Serializable]
	public class ActiveEffectVisual
	{
		public string EffectName;
		public float Duration;
		public float Remaining;

		public ActiveEffectVisual(string name, float duration)
		{
			EffectName = name;
			Duration = duration;
			Remaining = duration;
		}
	}

}