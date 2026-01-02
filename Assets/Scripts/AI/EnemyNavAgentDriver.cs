using Pathfinding;
using UnityEngine;

namespace Ships
{
	[RequireComponent(typeof(AIPath))]
	[RequireComponent(typeof(Seeker))]
	public sealed class EnemyNavAgentDriver : MonoBehaviour
	{
		private AIPath _agent;
		private ShipBase _ship;
		public AIPath Agent => _agent;

		public float RemainingDistance
		{
			get
			{
				if (_agent == null)
					return 0f;

				var dist = _agent.remainingDistance;

				if (float.IsInfinity(dist))
					return float.PositiveInfinity;

				if (_agent.reachedDestination)
					return 0f;

				return dist;
			}
		}

		private void Awake()
		{
			_agent = GetComponent<AIPath>();
			_ship = GetComponent<ShipBase>();
			_agent.canMove = true;
			_agent.isStopped = false;
		}

		private void Update()
		{
			if (_ship == null || _ship.ShipStats == null)
				return;

			_agent.maxSpeed = _ship.ShipStats.GetStat(StatType.MoveSpeed).Current;
			_agent.rotationSpeed = _ship.ShipStats.GetStat(StatType.TurnSpeed).Current;
			_agent.maxAcceleration = _ship.ShipStats.GetStat(StatType.Acceleration).Current;
		}

		public void SetDestination(Vector3 pos)
		{
			_agent.canMove = true;
			_agent.isStopped = false;
			_agent.destination = pos;
			_agent.SearchPath();
		}

		public void Stop()
		{
			_agent.isStopped = true;
			_agent.canMove = false;
			_agent.destination = transform.position;
			_agent.SetPath(null);
		}

		public bool SampleOnNavMesh(Vector3 pos, float radius, out Vector3 result)
		{
			result = Vector3.zero;

			var active = AstarPath.active;
			if (active == null)
				return false;

			var nnInfo = active.GetNearest(pos, NNConstraint.Default);
			if (nnInfo.node == null)
				return false;

			result = nnInfo.position;
			return (result - pos).sqrMagnitude <= radius * radius;
		}
	}
}
