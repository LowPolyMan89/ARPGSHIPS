namespace Ships
{
	public interface IStackableEffect : IOnHitEffect
	{
		string EffectId { get; }
		bool CanStack { get; }
		int MaxStacks { get; }
	}

}