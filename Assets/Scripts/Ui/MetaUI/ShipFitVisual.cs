using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class ShipFitVisual : MonoBehaviour
	{
		private ShipFitView _view;
		public List<ShipFitSlotVisual> WeaponSlots = new();  // 4 UI слота
		public List<ShipFitSlotVisual> ActiveSlots = new();  // 4 UI слота
		public List<ShipFitSlotVisual> PassiveSlots = new(); // 4 UI слота
		public List<ShipFitSlotVisual> HullSlots = new();    // 4 UI слота

		public void Init(ShipFitView view)
		{
			_view = view;
		}

		public void OnWeaponSlotClicked(string slotId)
		{
			_view.OnSlotClicked(slotId, isWeaponSlot: true);
		}

		public void UpdateSlotIcon(string slotId, Sprite icon)
		{
			// Находит UI-элемент, обновляет спрайт
		}

		public void UpdateSlot(string arg1, bool arg2)
		{
			
		}
	}

}