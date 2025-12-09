using System;

namespace Tanks
{
	[Serializable]
	public class LootTable
	{
		public string Id;
		public LootEntry[] Drops;
	}

	[Serializable]
	public class LootEntry
	{
		public int Chance;       // шанс выпадения предмета
		public string ItemId;    // шаблон предмета
		public int[] Weights;    // веса редкостей
	}
}