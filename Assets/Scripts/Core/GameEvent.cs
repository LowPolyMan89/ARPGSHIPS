using System;
using UnityEngine;

namespace Ships
{
	public static class GameEvent
	{
		public static Action<CalculatedDamage> OnTakeDamage;
		public static Action OnUiUpdate;
		public static Action<InventoryItem> OnClickItem;
		public static event Action<Vector2> OnCursorInput;
		public static event Action<bool> OnFireLmbInput;
		public static event Action<PlayerInventoryModel> OnInventoryUpdated;
		public static event Action<InventoryItem> OnItemSelected;
		public static void TakeDamage(CalculatedDamage calculatedDamage)
		{
			UiUpdate();
			OnTakeDamage?.Invoke(calculatedDamage);
		}
		public static void UiUpdate()
		{
			OnUiUpdate?.Invoke();
		}
		public static void InventoryUpdated(PlayerInventoryModel playerInventoryModel)
		{
			OnInventoryUpdated?.Invoke(playerInventoryModel);
		}
		public static void CursorInput(Vector2 screenPosition)
		{
			OnCursorInput?.Invoke(screenPosition);
		}
		public static void FireLmbInput(bool isPressed)
		{
			OnFireLmbInput?.Invoke(isPressed);
		}
		public static void ItemSelected(InventoryItem item)
		{
			OnItemSelected?.Invoke(item);
		}
		public static void ClickItem(InventoryItem item)
		{
			OnClickItem?.Invoke(item);
		}
	}
}
