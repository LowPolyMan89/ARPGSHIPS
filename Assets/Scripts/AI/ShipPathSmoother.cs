using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

namespace Ships
{
	[AddComponentMenu("Pathfinding/Modifiers/Ship Path Smoother")]
	[RequireComponent(typeof(Seeker))]
	public sealed class ShipPathSmoother : MonoModifier
	{
		[SerializeField] private bool enableSmoothing = true;
		[SerializeField] private float minPointDistance = 0.1f;
		[SerializeField] private float simplifyEpsilon = 0.5f;
		[SerializeField] private int smoothIterations = 2;
		[SerializeField, Range(0f, 1f)] private float smoothStrength = 0.5f;

		public override int Order => 50;

		public override void Apply(Path path)
		{
			if (!enableSmoothing || path.vectorPath == null || path.vectorPath.Count < 3)
				return;

			var points = new List<Vector3>(path.vectorPath);

			if (minPointDistance > 0f)
				points = RemoveClosePoints(points, minPointDistance);

			if (simplifyEpsilon > 0f && points.Count >= 3)
				points = SimplifyRdp(points, simplifyEpsilon);

			if (smoothIterations > 0 && smoothStrength > 0f && points.Count >= 3)
				points = Smooth(points, smoothIterations, smoothStrength);

			path.vectorPath = points;
		}

		private static List<Vector3> RemoveClosePoints(List<Vector3> points, float minDistance)
		{
			if (points.Count < 2)
				return points;

			var result = new List<Vector3>(points.Count);
			result.Add(points[0]);
			var last = points[0];

			for (int i = 1; i < points.Count - 1; i++)
			{
				if (DistanceXZ(last, points[i]) < minDistance)
					continue;

				result.Add(points[i]);
				last = points[i];
			}

			result.Add(points[points.Count - 1]);
			return result;
		}

		private static List<Vector3> SimplifyRdp(List<Vector3> points, float epsilon)
		{
			int count = points.Count;
			if (count < 3)
				return points;

			var keep = new bool[count];
			keep[0] = true;
			keep[count - 1] = true;

			var stack = new Stack<(int start, int end)>();
			stack.Push((0, count - 1));

			while (stack.Count > 0)
			{
				var (start, end) = stack.Pop();
				float maxDist = 0f;
				int index = -1;

				for (int i = start + 1; i < end; i++)
				{
					float dist = DistancePointToSegmentXZ(points[i], points[start], points[end]);
					if (dist > maxDist)
					{
						maxDist = dist;
						index = i;
					}
				}

				if (index >= 0 && maxDist > epsilon)
				{
					keep[index] = true;
					stack.Push((start, index));
					stack.Push((index, end));
				}
			}

			var result = new List<Vector3>(count);
			for (int i = 0; i < count; i++)
			{
				if (keep[i])
					result.Add(points[i]);
			}

			return result;
		}

		private static List<Vector3> Smooth(List<Vector3> points, int iterations, float strength)
		{
			var current = points;
			for (int it = 0; it < iterations; it++)
			{
				var smoothed = new List<Vector3>(current.Count);
				smoothed.Add(current[0]);

				for (int i = 1; i < current.Count - 1; i++)
				{
					var prev = current[i - 1];
					var next = current[i + 1];
					var target = (prev + next) * 0.5f;
					smoothed.Add(Vector3.Lerp(current[i], target, strength));
				}

				smoothed.Add(current[current.Count - 1]);
				current = smoothed;
			}

			return current;
		}

		private static float DistanceXZ(Vector3 a, Vector3 b)
		{
			float dx = a.x - b.x;
			float dz = a.z - b.z;
			return Mathf.Sqrt(dx * dx + dz * dz);
		}

		private static float DistancePointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
		{
			Vector2 pa = new Vector2(p.x - a.x, p.z - a.z);
			Vector2 ba = new Vector2(b.x - a.x, b.z - a.z);
			float denom = Vector2.Dot(ba, ba);
			if (denom <= 0.0001f)
				return pa.magnitude;

			float t = Mathf.Clamp01(Vector2.Dot(pa, ba) / denom);
			Vector2 proj = new Vector2(a.x, a.z) + ba * t;
			return Vector2.Distance(new Vector2(p.x, p.z), proj);
		}
	}
}
