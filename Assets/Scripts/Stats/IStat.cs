namespace Ships
{
	public interface IStat
	{
		StatType Name { get; }

		float BaseMaximum { get; }
		float BaseCurrent { get; }

		float Maximum { get; }
		float Current { get; }
		float Amount { get; }
		void AddModifier(StatModifier modifier);
		void RemoveModifier(StatModifier modifier);
		void RemoveModifiersFromSource(object source);

		void AddToCurrent(float delta);

		void Tick();
	}
}
