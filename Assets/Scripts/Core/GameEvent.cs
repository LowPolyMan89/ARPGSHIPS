using System;

namespace Ships
{
	public static class GameEvent
	{
		public static Action<CalculatedDamage> OnTakeDamage;
		public static Action OnUiUpdate;
		public static void TakeDamage(CalculatedDamage calculatedDamage)
		{
			UiUpdate();
			OnTakeDamage?.Invoke(calculatedDamage);
		}
		public static void UiUpdate()
		{
			OnUiUpdate?.Invoke();
		}
	}
}