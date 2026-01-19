using Pathfinding;
using UnityEngine;

namespace Ships
{
	[RequireComponent(typeof(AIPath))]
	[RequireComponent(typeof(Seeker))]
	[RequireComponent(typeof(ShipBase))]
	public sealed class PlayerClickMoveDriver : MonoBehaviour
	{
		[SerializeField] private Camera inputCamera;
		[SerializeField] private float navSampleRadius = 2f;
		[SerializeField] private bool requireNavmeshPoint = false;

		private AIPath _agent;
		private ShipBase _ship;
		private PlayerInputSystem _input;
		private bool _prevRmb;

		private void Awake()
		{
			_agent = GetComponent<AIPath>();
			_ship = GetComponent<ShipBase>();
			_input = FindAnyObjectByType<PlayerInputSystem>();
			if (inputCamera == null)
				inputCamera = Camera.main;
		}

		private void Update()
		{
			if (_ship != null && _ship.ShipStats != null)
			{
				_agent.maxSpeed = _ship.ShipStats.GetStat(StatType.MoveSpeed).Current;
				_agent.rotationSpeed = _ship.ShipStats.GetStat(StatType.TurnSpeed).Current;
				_agent.maxAcceleration = _ship.ShipStats.GetStat(StatType.Acceleration).Current;
			}

			if (_input == null)
				return;

			if (!IsShipSelected())
				return;

			var rmb = _input.FireRMB;
			if (rmb && !_prevRmb)
				HandleClick();

			_prevRmb = rmb;
		}

		private void HandleClick()
		{
			if (inputCamera == null)
			{
				inputCamera = Camera.main;
				if (inputCamera == null)
					return;
			}

			var ray = inputCamera.ScreenPointToRay(_input.CursorScreen);
			var plane = new Plane(Vector3.up, Vector3.zero);
			if (!plane.Raycast(ray, out var enter))
				return;

			var target = ray.GetPoint(enter);
			if (requireNavmeshPoint && !TryGetNavPoint(target, out target))
				return;

			_agent.canMove = true;
			_agent.isStopped = false;
			_agent.destination = target;
			_agent.SearchPath();
		}

		private bool IsShipSelected()
		{
			if (_ship == null)
				return false;

			if (Battle.Instance == null)
				return false;

			return Battle.Instance.IsShipSelected(_ship);
		}

		private bool TryGetNavPoint(Vector3 pos, out Vector3 result)
		{
			result = Vector3.zero;
			var active = AstarPath.active;
			if (active == null)
				return false;

			var nnInfo = active.GetNearest(pos, NNConstraint.Default);
			if (nnInfo.node == null)
				return false;

			result = nnInfo.position;
			return (result - pos).sqrMagnitude <= navSampleRadius * navSampleRadius;
		}
	}
}
