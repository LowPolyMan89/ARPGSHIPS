using System;
using UnityEngine;

namespace Ships
{
	[Serializable]
	public class InventoryItem
	{
		public string ItemId;     // legacy id, now matches TemplateId
		public string TemplateId; // weapon_p_small_bolter_1.json
		public string Rarity;     // Common, Rare, etc.
		public int Count = 1;     // total owned in stack

		[NonSerialized] public int EquippedCount;

		public string ResolvedId => !string.IsNullOrEmpty(TemplateId) ? TemplateId : ItemId;
		public int AvailableCount => Mathf.Max(0, Count - EquippedCount);
		public bool IsEquipped => EquippedCount > 0;
	}
}
