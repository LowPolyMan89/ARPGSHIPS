using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ships
{
	public class TargetFinder
	{
		private readonly List<ITargetable> _targets = new();

		public void UpdateTargets(IEnumerable<ITargetable> list, TeamMask hitMask)
		{
			_targets.Clear();

			foreach (var t in list)
			{
				if (!t.IsAlive) continue;
				if (!HitRules.CanHit(hitMask, t.Team)) continue;
				_targets.Add(t);
			}
		}

		public ITargetable FindBestTarget(
			Vector3 origin,
			Vector3 forward,
			float maxAngleDeg,
			float maxDistance,
			IReadOnlyList<ShipClass> classPriority = null)
		{
			origin.y = 0f;
			forward.y = 0f;

			Dictionary<ShipClass, int> priority = null;
			if (classPriority != null && classPriority.Count > 0)
			{
				priority = new Dictionary<ShipClass, int>(classPriority.Count);
				for (var i = 0; i < classPriority.Count; i++)
					priority[classPriority[i]] = i;
			}

			var valid =
				_targets
					.Where(t =>
					{
						var pos = t.Transform.position;
						pos.y = 0f;

						var toTarget = pos - origin;
						if (priority != null && !priority.ContainsKey(t.Class))
							return false;

						return toTarget.magnitude <= maxDistance &&
						       Vector3.Angle(forward, toTarget) <= maxAngleDeg;
					})
					.OrderBy(t => GetClassPriority(t.Class, priority))
					.ThenBy(t =>
					{
						var pos = t.Transform.position;
						pos.y = 0f;
						return Vector3.Distance(origin, pos);
					})
					.ToList();

			return valid.FirstOrDefault();
		}

		public bool IsValidTarget(
			ITargetable target,
			Vector3 origin,
			Vector3 forward,
			float maxAngleDeg,
			float maxDistance,
			IReadOnlyList<ShipClass> classPriority = null)
		{
			if (target == null || !target.IsAlive)
				return false;

			Dictionary<ShipClass, int> priority = null;
			if (classPriority != null && classPriority.Count > 0)
			{
				priority = new Dictionary<ShipClass, int>(classPriority.Count);
				for (var i = 0; i < classPriority.Count; i++)
					priority[classPriority[i]] = i;
			}

			if (priority != null && !priority.ContainsKey(target.Class))
				return false;

			var pos = target.Transform.position;
			pos.y = 0f;
			origin.y = 0f;
			forward.y = 0f;

			var toTarget = pos - origin;
			return toTarget.magnitude <= maxDistance &&
			       Vector3.Angle(forward, toTarget) <= maxAngleDeg;
		}

		private static int GetClassPriority(ShipClass shipClass, Dictionary<ShipClass, int> priority)
		{
			if (priority == null)
				return 0;

			return priority.TryGetValue(shipClass, out var value) ? value : int.MaxValue;
		}

		// упреждение
		public Vector3 Predict(ITargetable t, Vector3 firePos, float projSpeed)
		{
			Vector3 tp = t.Transform.position;
			Vector3 vel = t.Velocity;

			float dist = Vector3.Distance(tp, firePos);
			float time = dist / projSpeed;

			return tp + vel * time;
		}

		// проверка что наведено
		public bool IsAimedAt(Transform pivot, Vector3 toAimDir, float toleranceDeg)
		{
			return IsAimedAt(pivot.forward, toAimDir, toleranceDeg);
		}

		public bool IsAimedAt(Vector3 pivotDirection, Vector3 toAimDir, float toleranceDeg)
		{
			var angle = Vector3.Angle(pivotDirection, toAimDir);
			//Debug.Log($"{pivot.root} angle {angle}");
			return angle <= toleranceDeg;
		}
	}
}
