using System.Collections.Generic;
using UnityEngine;

namespace Tanks
{
	public class TankFitVisual : MonoBehaviour
	{
		private TankFitView _view;
		public List<TankFitSlotVisual> WeaponSlots = new();  // 4 UI слота
		public List<TankFitSlotVisual> ActiveSlots = new();  // 4 UI слота
		public List<TankFitSlotVisual> PassiveSlots = new(); // 4 UI слота
		public List<TankFitSlotVisual> HullSlots = new();    // 4 UI слота

		public void Init(TankFitView view)
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