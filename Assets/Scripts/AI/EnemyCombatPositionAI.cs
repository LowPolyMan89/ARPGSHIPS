using UnityEngine;

namespace Tanks
{
	[RequireComponent(typeof(EnemyNavAgentDriver))]
	public sealed class EnemyCombatPositionAI : MonoBehaviour
	{
		[Header("Combat distance")]
		[SerializeField] private float _stopDistanceFactor = 0.85f;
		[SerializeField] private float _minDistanceFactor = 0.3f;

		[Header("Search")]
		[SerializeField] private int _positionSamples = 16;
		[SerializeField] private float _navSampleRadius = 3f;

		[Header("Repath")]
		[SerializeField] private float _repathCooldown = 0.8f;

		[Header("LOS")]
		[SerializeField] private LayerMask _obstacleMask;

		private EnemyNavAgentDriver _nav;
		private TankBase _tank;
		private Transform _target;

		private Vector3 _combatPos;
		private bool _hasCombatPos;
		private float _fireRange;
		private float _nextRepathTime;

		private void Awake()
		{
			_nav = GetComponent<EnemyNavAgentDriver>();
			_tank = GetComponent<TankBase>();
		}

		private void Update()
		{
			UpdateTarget();
			if (!_target)
			{
				_nav.Stop();
				return;
			}

			_fireRange = WeaponRangeResolver.GetMinFireRange(_tank);

			var dist = Vector3.Distance(transform.position, _target.position);

			// 1) нет позиции → ищем
			if (!_hasCombatPos)
			{
				BuildCombatPosition();
				return;
			}

			// 2) цель ушла слишком далеко
			if (dist > _fireRange)
			{
				Invalidate();
				return;
			}

			// 3) слишком близко
			if (dist < _fireRange * _minDistanceFactor)
			{
				Invalidate();
				return;
			}

			// 4) потеряли LOS
			if (!LineOfSightUtility.HasLOS(
				    transform.position,
				    _target.position,
				    _obstacleMask))
			{
				Invalidate();
				return;
			}



			// 5) на позиции и LOS есть → стоим
			if (_nav.RemainingDistance <= 0.6f)
			{
				_nav.Stop();
			}
		}

		// =====================================================
		// POSITION SEARCH
		// =====================================================

		private void Invalidate()
		{
			if (Time.time < _nextRepathTime)
				return;

			_hasCombatPos = false;
			_nextRepathTime = Time.time + _repathCooldown;
		}

		private void BuildCombatPosition()
		{
			var wantDist = _fireRange * _stopDistanceFactor;
			var baseDir = transform.position - _target.position;
			baseDir.y = 0f;

			if (baseDir.sqrMagnitude < 0.001f)
				baseDir = transform.forward;

			baseDir.Normalize();

			// 1) сначала пробуем по прямой
			if (TryCandidate(baseDir, wantDist, out var pos))
			{
				AcceptPosition(pos);
				return;
			}

			// 2) потом по окружности
			var step = 360f / Mathf.Max(4, _positionSamples);
			for (var i = 0; i < _positionSamples; i++)
			{
				var dir = Quaternion.Euler(0f, step * i, 0f) * baseDir;
				if (TryCandidate(dir, wantDist, out pos))
				{
					AcceptPosition(pos);
					return;
				}
			}

			// 3) ничего не нашли — едем прямо к цели
			_nav.SetDestination(_target.position);
		}

		private bool TryCandidate(Vector3 dir, float dist, out Vector3 result)
		{
			var raw = _target.position + dir * dist;

			if (!_nav.SampleOnNavMesh(raw, _navSampleRadius, out result))
				return false;

			// ❗ КЛЮЧЕВОЕ: проверяем LOS ИЗ ТОЧКИ
			return LineOfSightUtility.HasLOS(
				result,
				_target.position,
				_obstacleMask
			);

		}

		private void AcceptPosition(Vector3 pos)
		{
			_combatPos = pos;
			_hasCombatPos = true;
			_nav.SetDestination(_combatPos);
		}
		private void UpdateTarget()
		{
			_target = Battle.Instance && Battle.Instance.Player
				? Battle.Instance.Player.transform
				: null;
		}
	}
}
