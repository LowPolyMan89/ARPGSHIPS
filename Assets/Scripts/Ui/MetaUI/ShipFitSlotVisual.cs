using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	public class ShipFitSlotVisual : MonoBehaviour
	{
		public string SlotId;
		public bool IsWeapon;

		private ShipFitView _view;

		public void Init(ShipFitView view)
		{
			_view = view;
		}

		public void OnClick()
		{
			_view.OnSlotClicked(SlotId, IsWeapon);
		}

		public void SetIcon(Sprite sprite)
		{
			GetComponent<Image>().sprite = sprite;
		}
	}
}
