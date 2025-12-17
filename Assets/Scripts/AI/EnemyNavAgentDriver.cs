using UnityEngine;
using UnityEngine.AI;

namespace Ships
{
	[RequireComponent(typeof(NavMeshAgent))]
	public sealed class EnemyNavAgentDriver : MonoBehaviour
	{
		private NavMeshAgent _agent;
		public NavMeshAgent Agent => _agent;

		public float RemainingDistance => _agent.remainingDistance;

		private void Awake()
		{
			_agent = GetComponent<NavMeshAgent>();
			_agent.updatePosition = true;
			_agent.updateRotation = true;
			_agent.autoBraking = true;
		}

		public void SetDestination(Vector3 pos)
		{
			_agent.isStopped = false;
			_agent.SetDestination(pos);
		}

		public void Stop()
		{
			_agent.isStopped = true;
			_agent.ResetPath();
		}

		public bool SampleOnNavMesh(Vector3 pos, float radius, out Vector3 result)
		{
			if (NavMesh.SamplePosition(pos, out var hit, radius, NavMesh.AllAreas))
			{
				result = hit.position;
				return true;
			}

			result = Vector3.zero;
			return false;
		}
	}
}
