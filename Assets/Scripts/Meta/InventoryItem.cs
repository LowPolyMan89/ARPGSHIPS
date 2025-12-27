using System;
using UnityEngine;

namespace Ships
{
	[Serializable]
	public class InventoryItem
	{
		public string ItemId;          // уникальный ID предмета
		public string TemplateId;      // weapon_p_small_bolter_1.json
		public string EquippedOnFitId; // фит, на котором установлен

		// Grid-based equipment (meta)
		public string EquippedGridId;
		public int EquippedGridX = -1;
		public int EquippedGridY = -1;
		public Vector2 EquippedGridPos;
		public float EquippedGridRot;

		public bool IsEquipped => !string.IsNullOrEmpty(EquippedOnFitId);
	}
}
