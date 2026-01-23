using System.Collections.Generic;

namespace Ships
{
	public static class AiFocusRegistry
	{
		private static readonly Dictionary<ShipBase, int> Counts = new();

		public static int GetCount(ShipBase target)
		{
			if (target == null)
				return 0;

			return Counts.TryGetValue(target, out var count) ? count : 0;
		}

		public static void AddTarget(ShipBase target)
		{
			if (target == null)
				return;

			Counts[target] = GetCount(target) + 1;
		}

		public static void RemoveTarget(ShipBase target)
		{
			if (target == null)
				return;

			if (!Counts.TryGetValue(target, out var count))
				return;

			count -= 1;
			if (count <= 0)
				Counts.Remove(target);
			else
				Counts[target] = count;
		}
	}
}
