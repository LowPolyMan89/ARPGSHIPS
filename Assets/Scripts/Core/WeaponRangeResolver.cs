using UnityEngine;

namespace Tanks
{
	public static class WeaponRangeResolver
	{
		public static float GetMinFireRange(TankBase tank)
		{
			float minRange = float.MaxValue;

			var weapons = tank.GetComponentsInChildren<WeaponBase>(true);
			foreach (var weapon in weapons)
			{
				if (weapon.Model == null)
					continue;

				var stat = weapon.Model.Stats.GetStat(StatType.FireRange);
				if (stat != null && stat.Current > 0f)
					minRange = Mathf.Min(minRange, stat.Current);
			}

			if (minRange == float.MaxValue)
				minRange = 5f; // fallback

			return minRange;
		}
	}
}