using UnityEngine;

namespace Ships
{
	public static class LineOfSightUtility
	{
		/// <summary>
		/// Проверка прямой видимости между двумя точками в мире
		/// </summary>
		public static bool HasLOS(
			Vector3 from,
			Vector3 to,
			LayerMask obstacleMask,
			float heightOffset = 0.6f)
		{
			var a = from + Vector3.up * heightOffset;
			var b = to + Vector3.up * heightOffset;

			var dir = b - a;
			dir.y = 0f;

			if (dir.sqrMagnitude < 0.0001f)
				return true;

			return !Physics.Raycast(
				a,
				dir.normalized,
				dir.magnitude,
				obstacleMask,
				QueryTriggerInteraction.Ignore
			);
		}

		/// <summary>
		/// Проверка LOS от позиции до трансформа цели
		/// </summary>
		public static bool HasLOS(
			Vector3 from,
			Transform target,
			LayerMask obstacleMask,
			float heightOffset = 0.6f)
		{
			if (!target)
				return false;

			return HasLOS(from, target.position, obstacleMask, heightOffset);
		}
	}
}
