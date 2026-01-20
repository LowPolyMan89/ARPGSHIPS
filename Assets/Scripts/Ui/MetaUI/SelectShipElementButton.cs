using UnityEngine;
using UnityEngine.UI;

public class SelectShipElementButton : MonoBehaviour
{
	[SerializeField] private Button _button;
	[SerializeField] private Image _shipImage;

	public void Init(Sprite icon, System.Action onClick)
	{
		if (_shipImage != null)
		{
			_shipImage.sprite = icon;
			_shipImage.enabled = icon != null;
		}

		if (_button != null)
		{
			_button.onClick.RemoveAllListeners();
			if (onClick != null)
				_button.onClick.AddListener(() => onClick());
		}
	}
}
