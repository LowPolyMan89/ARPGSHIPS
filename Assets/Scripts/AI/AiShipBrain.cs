using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	[RequireComponent(typeof(EnemyNavAgentDriver))]
	public sealed class AiShipBrain : MonoBehaviour
	{
		private enum AiState
		{
			Engage,
			Reposition,
			Escort
		}

		private EnemyNavAgentDriver _nav;
		private ShipBase _ship;
		private WeaponTargeting[] _weaponTargeting;
		private AiConfig _config;
		private AiClassConfig _classConfig;
		private AiState _state = AiState.Engage;
		private ShipBase _currentTarget;
		private ShipBase _escortTarget;
		private Vector3 _repositionDestination;
		private float _desiredRange;
		private float _nextReevalTime;
		private float _stateEndTime;
		private int _focusLimit;
		private bool _hasDestination;

		public ShipBase Ship => _ship;
		public ShipBase CurrentTarget => _currentTarget;
		public ShipBase EscortTarget => _escortTarget;
		public string CurrentState => _state.ToString();
		public float DesiredRange => _desiredRange;
		public float NextReevalTime => _nextReevalTime;
		public float StateEndTime => _stateEndTime;
		public int FocusLimit => _focusLimit;
		public bool HasDestination => _hasDestination;
		public Vector3 RepositionDestination => _repositionDestination;

		private void Awake()
		{
			_nav = GetComponent<EnemyNavAgentDriver>();
			_ship = GetComponent<ShipBase>();
			_weaponTargeting = GetComponentsInChildren<WeaponTargeting>(true);
			_config = AiConfigLoader.Load();
			_classConfig = _config.GetClassConfig(_ship.Class);
			_focusLimit = Mathf.Max(1, _config.GetFocusLimit(_ship.Class).GetRandom());
		}

		private void OnDisable()
		{
			ClearFocus();
		}

		private void Update()
		{
			if (_ship == null || !_ship.IsAlive)
			{
				ClearFocus();
				return;
			}

			if (Battle.Instance == null)
				return;

			if (Time.time >= _nextReevalTime)
				Reevaluate();

			TickState();
			UpdateWeaponTargets();
		}

		private void Reevaluate()
		{
			_nextReevalTime = Time.time + _classConfig.GetReevalInterval();

			EnsureDesiredRange();
			UpdateEscortTarget();

			if (TryForceDamageAggroTarget(out var damageTarget))
			{
				if (damageTarget != _currentTarget)
					SetTarget(damageTarget);
				_state = AiState.Engage;
				return;
			}

			if (TryForceAggroTarget(out var forcedTarget))
			{
				if (forcedTarget != _currentTarget)
					SetTarget(forcedTarget);
				_state = AiState.Engage;
				return;
			}

			var newTarget = SelectTarget();
			if (newTarget != _currentTarget)
				SetTarget(newTarget);

			PickState();
		}

		private void TickState()
		{
			if (_currentTarget == null && _state != AiState.Escort)
			{
				_nav.Stop();
				return;
			}

			switch (_state)
			{
				case AiState.Engage:
					TickEngage();
					break;
				case AiState.Reposition:
					TickReposition();
					break;
				case AiState.Escort:
					TickEscort();
					break;
			}
		}

		private void TickEngage()
		{
			if (_currentTarget == null)
				return;

			var targetPos = _currentTarget.transform.position;
			var dist = PlanarDistance(transform.position, targetPos);

			var tolerance = _desiredRange * _config.Targeting.OptimalDistanceTolerance;
			if (Mathf.Abs(dist - _desiredRange) <= tolerance)
			{
				_nav.Stop();
				return;
			}

			var dir = FlattenToPlane(transform.position - targetPos);
			if (dir.sqrMagnitude < 0.001f)
				dir = FlattenToPlane(transform.forward);
			dir.Normalize();

			var desiredPos = targetPos + dir * _desiredRange;
			MoveTo(desiredPos);
		}

		private void TickReposition()
		{
			if (Time.time >= _stateEndTime)
			{
				_state = AiState.Engage;
				_hasDestination = false;
				return;
			}

			if (!_hasDestination)
				BuildRepositionDestination();

			if (_hasDestination)
				MoveTo(_repositionDestination);
		}

		private void TickEscort()
		{
			if (_escortTarget == null)
			{
				_state = AiState.Engage;
				return;
			}

			var targetPos = _escortTarget.transform.position;
			var dist = PlanarDistance(transform.position, targetPos);
			if (dist <= _classConfig.EscortDistance)
			{
				_nav.Stop();
				return;
			}

			var dir = FlattenToPlane(transform.position - targetPos);
			if (dir.sqrMagnitude < 0.001f)
				dir = FlattenToPlane(transform.forward);
			dir.Normalize();
			var desiredPos = targetPos + dir * _classConfig.EscortDistance;
			MoveTo(desiredPos);
		}

		private void PickState()
		{
			if (_currentTarget == null)
			{
				_state = _escortTarget != null ? AiState.Escort : AiState.Reposition;
				_stateEndTime = Time.time + _classConfig.GetRepositionDuration();
				_hasDestination = false;
				return;
			}

			var dist = PlanarDistance(transform.position, _currentTarget.transform.position);
			if (dist > _desiredRange * _classConfig.MaxChaseFactor)
			{
				SetTarget(null);
				_state = AiState.Reposition;
				_stateEndTime = Time.time + _classConfig.GetRepositionDuration();
				_hasDestination = false;
				return;
			}
			if (dist > _desiredRange * _config.Targeting.TooFarFactor ||
			    dist < _desiredRange * _config.Targeting.TooCloseFactor)
			{
				_state = AiState.Reposition;
				_stateEndTime = Time.time + _classConfig.GetRepositionDuration();
				_hasDestination = false;
				return;
			}

			if (Random.value < _classConfig.RepositionChance)
			{
				_state = AiState.Reposition;
				_stateEndTime = Time.time + _classConfig.GetRepositionDuration();
				_hasDestination = false;
				return;
			}

			_state = AiState.Engage;
		}

		private void BuildRepositionDestination()
		{
			if (_currentTarget == null)
				return;

			var maneuver = PickManeuver();
			var targetPos = _currentTarget.transform.position;
			var toSelf = FlattenToPlane(transform.position - targetPos);
			if (toSelf.sqrMagnitude < 0.001f)
				toSelf = FlattenToPlane(transform.forward);
			toSelf.Normalize();

			Vector3 dir;
			switch (maneuver)
			{
				case AiManeuver.Backstep:
					dir = toSelf;
					_repositionDestination = targetPos + dir * (_desiredRange * _config.Movement.BackstepDistanceFactor);
					break;
				case AiManeuver.Strafe:
					dir = Quaternion.AngleAxis(90f, Vector3.up) * toSelf;
					_repositionDestination = targetPos + dir * (_desiredRange * _config.Movement.StrafeDistanceFactor);
					break;
				case AiManeuver.FlankShift:
					var angle = Random.Range(_classConfig.FlankAngleMin, _classConfig.FlankAngleMax);
					dir = Quaternion.AngleAxis(angle, Vector3.up) * toSelf;
					_repositionDestination = targetPos + dir * (_desiredRange * _config.Movement.FlankDistanceFactor);
					break;
				case AiManeuver.Orbit:
				default:
					dir = Quaternion.AngleAxis(Random.Range(_config.Movement.OrbitAngleMin, _config.Movement.OrbitAngleMax), Vector3.up) * toSelf;
					_repositionDestination = targetPos + dir * (_desiredRange * _config.Movement.OrbitDistanceFactor);
					break;
			}

			if (_nav.SampleOnNavMesh(_repositionDestination, _config.Movement.NavSampleRadius, out var sampled))
				_repositionDestination = sampled;

			_hasDestination = true;
		}

		private AiManeuver PickManeuver()
		{
			var hpPercent = GetHpPercent(_ship);
			if (hpPercent > 0f && hpPercent <= _classConfig.BackstepHpThreshold)
				return AiManeuver.Backstep;

			var total = _classConfig.OrbitWeight + _classConfig.StrafeWeight +
			            _classConfig.BackstepWeight + _classConfig.FlankWeight;
			if (total <= 0f)
				return AiManeuver.Orbit;

			var roll = Random.value * total;
			if (roll <= _classConfig.OrbitWeight)
				return AiManeuver.Orbit;
			roll -= _classConfig.OrbitWeight;
			if (roll <= _classConfig.StrafeWeight)
				return AiManeuver.Strafe;
			roll -= _classConfig.StrafeWeight;
			if (roll <= _classConfig.BackstepWeight)
				return AiManeuver.Backstep;

			return AiManeuver.FlankShift;
		}

		private void UpdateWeaponTargets()
		{
			if (_weaponTargeting == null || _weaponTargeting.Length == 0)
				return;

			for (var i = 0; i < _weaponTargeting.Length; i++)
			{
				var wt = _weaponTargeting[i];
				if (wt == null)
					continue;
				wt.SetPreferredTarget(_currentTarget);
			}
		}

		private ShipBase SelectTarget()
		{
			var candidates = Battle.Instance.AllShips;
			if (candidates == null || candidates.Count == 0)
				return null;

			ShipBase best = null;
			var bestScore = float.MinValue;

			for (var i = 0; i < candidates.Count; i++)
			{
				var target = candidates[i];
				if (!IsValidTarget(target))
					continue;

				var score = ScoreTarget(target);
				if (score > bestScore)
				{
					bestScore = score;
					best = target;
				}
			}

			if (_currentTarget != null && IsValidTarget(_currentTarget))
			{
				var currentScore = ScoreTarget(_currentTarget);
				if (best == null || bestScore < currentScore + _config.Targeting.SwitchThreshold)
					return _currentTarget;
			}

			return best;
		}

		private bool TryForceAggroTarget(out ShipBase target)
		{
			target = null;

			if (_classConfig == null || _classConfig.AggroRange <= 0f)
				return false;

			var player = Battle.Instance != null ? Battle.Instance.Player : null;
			if (player == null || !IsValidTarget(player))
				return false;

			var dist = PlanarDistance(transform.position, player.transform.position);
			if (dist > _classConfig.AggroRange)
				return false;

			target = player;
			return true;
		}

		private bool TryForceDamageAggroTarget(out ShipBase target)
		{
			target = null;

			if (_ship == null)
				return false;

			var attacker = _ship.LastAttacker;
			if (attacker == null || !IsValidTarget(attacker))
				return false;

			if (Time.time - _ship.LastAttackerTime > _config.Targeting.AttackerMemorySeconds)
				return false;

			target = attacker;
			return true;
		}

		private bool IsValidTarget(ShipBase target)
		{
			if (target == null || !target.IsAlive)
				return false;

			if (target == _ship)
				return false;

			return (_ship.HitMask & target.Team) != 0;
		}

		private float ScoreTarget(ShipBase target)
		{
			var score = _config.GetTargetValue(target.Class);

			if (IsHighThreatLight(target))
				score += _config.Targeting.ThreatHeavyLightBonus;

			if (WasAttackingUs(target))
				score += _config.Targeting.ThreatAttackingBonus;

			var hpPercent = GetHpPercent(target);
			if (hpPercent > 0f && hpPercent < _config.Targeting.CriticalHpThreshold)
				score += _config.Targeting.CriticalHpBonus;
			else if (hpPercent > 0f && hpPercent < _config.Targeting.LowHpThreshold)
				score += _config.Targeting.LowHpBonus;

			var dist = PlanarDistance(transform.position, target.transform.position);
			var tolerance = _desiredRange * _config.Targeting.OptimalDistanceTolerance;
			if (Mathf.Abs(dist - _desiredRange) <= tolerance)
				score += _config.Targeting.OptimalDistanceBonus;
			else if (dist > _desiredRange * _config.Targeting.TooFarFactor)
				score += _config.Targeting.TooFarPenalty;

			var focus = AiFocusRegistry.GetCount(target);
			if (focus >= _focusLimit)
				score += _config.Targeting.OverkillPenalty;

			return score;
		}

		private bool WasAttackingUs(ShipBase target)
		{
			if (target == null || _ship == null)
				return false;

			return _ship.LastAttacker == target &&
			       Time.time - _ship.LastAttackerTime <= _config.Targeting.AttackerMemorySeconds;
		}

		private bool IsHighThreatLight(ShipBase target)
		{
			if (target == null)
				return false;

			if (target.Class != ShipClass.Frigate && target.Class != ShipClass.Destroyer)
				return false;

			return HasLargeWeapon(target);
		}

		private bool HasLargeWeapon(ShipBase ship)
		{
			var weapons = ship.GetComponentsInChildren<WeaponBase>(true);
			for (var i = 0; i < weapons.Length; i++)
			{
				var weapon = weapons[i];
				var size = weapon?.Model?.Size;
				if (string.IsNullOrEmpty(size))
					continue;

				if (size.Equals("Large", System.StringComparison.OrdinalIgnoreCase) ||
				    size.Equals("L", System.StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		private void UpdateEscortTarget()
		{
			if (_ship == null)
				return;

			if (_ship.Class == ShipClass.Flagship)
			{
				_escortTarget = null;
				return;
			}

			ShipBase best = null;
			var bestClass = ShipClass.Frigate;
			var ships = Battle.Instance != null ? Battle.Instance.AllShips : null;
			if (ships == null)
				return;

			for (var i = 0; i < ships.Count; i++)
			{
				var candidate = ships[i];
				if (candidate == null || candidate == _ship)
					continue;
				if (candidate.Team != _ship.Team)
					continue;

				if (candidate.Class == ShipClass.Flagship)
				{
					best = candidate;
					break;
				}

				if ((int)candidate.Class > (int)bestClass)
				{
					bestClass = candidate.Class;
					best = candidate;
				}
			}

			_escortTarget = best;
		}

		private void EnsureDesiredRange()
		{
			var weapons = _ship.GetComponentsInChildren<WeaponBase>(true);
			var maxRange = 0f;
			var size = "";

			for (var i = 0; i < weapons.Length; i++)
			{
				var weapon = weapons[i];
				if (weapon?.Model?.Stats == null)
					continue;

				var range = weapon.Model.Stats.GetStat(StatType.FireRange)?.Current ?? 0f;
				if (range > maxRange)
				{
					maxRange = range;
					size = weapon.Model.Size;
				}
			}

			if (maxRange <= 0f)
				maxRange = _config.Movement.FallbackRange;

			var mult = _config.GetWeaponSizeMultiplier(size);
			if (mult <= 0f)
				mult = 1f;

			_desiredRange = maxRange * mult;
		}

		private void SetTarget(ShipBase target)
		{
			if (_currentTarget == target)
				return;

			ClearFocus();
			_currentTarget = target;
			if (_currentTarget != null)
				AiFocusRegistry.AddTarget(_currentTarget);
		}

		private void ClearFocus()
		{
			if (_currentTarget != null)
				AiFocusRegistry.RemoveTarget(_currentTarget);
			_currentTarget = null;
		}

		private void MoveTo(Vector3 destination)
		{
			_nav.SetDestination(destination);
		}

		private static float GetHpPercent(ShipBase ship)
		{
			if (ship == null)
				return 1f;

			if (!ship.TryGetStat(StatType.HitPoint, out var hp) || hp == null)
				return 1f;

			var max = hp.Maximum;
			if (max <= 0f)
				return 1f;

			return Mathf.Clamp01(hp.Current / max);
		}

		private static Vector3 FlattenToPlane(Vector3 v)
		{
			return new Vector3(v.x, 0f, v.z);
		}

		private static float PlanarDistance(Vector3 a, Vector3 b)
		{
			return FlattenToPlane(a - b).magnitude;
		}

		private enum AiManeuver
		{
			Orbit,
			Strafe,
			Backstep,
			FlankShift
		}
	}
}
