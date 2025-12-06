using UnityEngine;
using UnityEngine.UI;

namespace Ships
{
	public class InventoryItemVisual : MonoBehaviour
	{
		private InventoryItem _item;
		private System.Action<InventoryItem> _onClick;

		[Header("UI")]
		public Image Icon;
		public Text CountLabel;
		public Button Button;

		public void Init(InventoryItem item, System.Action<InventoryItem> onClick)
		{
			_item = item;
			_onClick = onClick;

			CountLabel.text = item.Count.ToString();

			Button.onClick.RemoveAllListeners();
			Button.onClick.AddListener(() => _onClick(_item));
		}

		public void UpdateCount()
		{
			CountLabel.text = _item.Count.ToString();
		}
	}
}