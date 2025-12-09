using System;

namespace Tanks
{
	[Serializable]
	public class InventoryItem
	{
		public string ItemId;          // уникальный ID предмета
		public string TemplateId;      // weapon_p_small_bolter_1.json
		public string EquippedOnFitId; // фит, на котором установлен
		public int EquippedSlotIndex = -1;

		public bool IsEquipped => !string.IsNullOrEmpty(EquippedOnFitId);
	}
}