using System.Collections.Generic;

namespace Tanks
{
	[System.Serializable]
	public class StatVisual
	{
		public StatType Name;
		public float BaseMaximum;
		public float BaseCurrent;
		public float Maximum;
		public float Current;

		public List<StatModifierVisual> ModifierVisuals = new List<StatModifierVisual>();
	}
}