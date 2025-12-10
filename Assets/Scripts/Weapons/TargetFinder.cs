using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tanks
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
			float maxDistance)
		{
			var valid =
				_targets
					.Where(t =>
						Vector3.Distance(origin, t.Transform.position) <= maxDistance &&
						Vector3.Angle(forward, (t.Transform.position - origin)) <= maxAngleDeg)
					.OrderBy(t => Vector3.Distance(origin, t.Transform.position))
					.ToList();

			return valid.FirstOrDefault();
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
			float angle = Vector3.Angle(pivot.forward, toAimDir);
			Debug.Log($"{pivot.root} angle {angle}");
			return angle <= toleranceDeg;
		}
	}
}