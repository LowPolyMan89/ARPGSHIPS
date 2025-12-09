using UnityEngine;
using UnityEngine.UI;

namespace Tanks
{
	public class TankFitSlotVisual : MonoBehaviour
	{
		public string SlotId;
		public bool IsWeapon;

		private TankFitView _view;

		public void Init(TankFitView view)
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