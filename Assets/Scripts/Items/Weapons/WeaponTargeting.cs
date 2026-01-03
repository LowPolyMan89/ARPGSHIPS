using UnityEngine;

namespace Ships
{
	public class WeaponTargeting : MonoBehaviour
	{
		public WeaponBase Weapon;
		public float AimTolerance = 3f;
		public float RotationSpeedDeg = 720f;

		private readonly TargetFinder _finder = new();
		private ShipBase _owner;
		private Vector2 _cursorScreen;
		private bool _fireHeld;

		private void Awake()
		{
			_owner = GetComponentInParent<ShipBase>();
		}

		private void OnEnable()
		{
			GameEvent.OnCursorInput += HandleCursorInput;
			GameEvent.OnFireLmbInput += HandleFireLmbInput;
		}

		private void OnDisable()
		{
			GameEvent.OnCursorInput -= HandleCursorInput;
			GameEvent.OnFireLmbInput -= HandleFireLmbInput;
		}

		private void Update()
		{
			if (_owner == null || Weapon == null || Weapon.Model == null)
				return;

			if (Battle.Instance == null)
				return;

			var plane = Battle.Instance.Plane;
			if (Weapon.Model.IsAutoFire)
				AutoAimAndFire(plane);
			else
				ManualAimAndFire(plane);
		}

		private void AutoAimAndFire(Battle.WorldPlane plane)
		{
			var aimPlane = plane == Battle.WorldPlane.XY ? WeaponRotator.AimPlane.XY : WeaponRotator.AimPlane.XZ;
			var rotationSpeedDeg = GetRotationSpeedDeg();

			_finder.UpdateTargets(Battle.Instance.AllShips, _owner.HitMask);

			var baseTransform = Weapon.BaseTransform != null ? Weapon.BaseTransform : Weapon.transform;
			var rotatingTransform = Weapon.TurretTransform != null ? Weapon.TurretTransform : Weapon.transform;

			var range = Weapon.Model.Stats.GetStat(StatType.FireRange).Current;
			var firePoint = Weapon.FirePoint != null ? Weapon.FirePoint.position : baseTransform.position;
			var origin = firePoint;
			var baseForward = plane == Battle.WorldPlane.XY ? baseTransform.up : baseTransform.forward;
			var maxAngle = Weapon.FireArcDeg <= 0 ? 360f : Weapon.FireArcDeg;

			var target = _finder.FindBestTarget(origin, baseForward, maxAngle, range, plane);
			if (target == null)
				return;

			var projectileSpeed = Weapon.Model.Stats.GetStat(StatType.ProjectileSpeed).Current;
			var predicted = _finder.Predict(target, firePoint, projectileSpeed);
			var dir = predicted - origin;

			if (aimPlane == WeaponRotator.AimPlane.XY)
				dir.z = 0f;
			else
				dir.y = 0f;

			WeaponRotator.Rotate(
				rotatingTransform: rotatingTransform,
				baseTransform: baseTransform,
				worldDirection: dir,
				rotationSpeedDeg: rotationSpeedDeg,
				maxAngleDeg: maxAngle,
				aimPlane: aimPlane
			);

			var currentForward = plane == Battle.WorldPlane.XY ? rotatingTransform.up : rotatingTransform.forward;
			if (_finder.IsAimedAt(currentForward, dir, AimTolerance))
				Weapon.TryFire(target);
		}

		private void ManualAimAndFire(Battle.WorldPlane plane)
		{
			if (!TryGetCursorDirection(plane, out var origin, out var dir))
				return;

			var baseTransform = Weapon.BaseTransform != null ? Weapon.BaseTransform : Weapon.transform;
			var rotatingTransform = Weapon.TurretTransform != null ? Weapon.TurretTransform : Weapon.transform;
			var aimPlane = plane == Battle.WorldPlane.XY ? WeaponRotator.AimPlane.XY : WeaponRotator.AimPlane.XZ;
			var maxAngle = Weapon.FireArcDeg <= 0 ? 360f : Weapon.FireArcDeg;
			var baseForward = plane == Battle.WorldPlane.XY ? baseTransform.up : baseTransform.forward;
			var rotationSpeedDeg = GetRotationSpeedDeg();

			WeaponRotator.Rotate(
				rotatingTransform: rotatingTransform,
				baseTransform: baseTransform,
				worldDirection: dir,
				rotationSpeedDeg: rotationSpeedDeg,
				maxAngleDeg: maxAngle,
				aimPlane: aimPlane
			);

			if (!_fireHeld)
				return;

			if (plane == Battle.WorldPlane.XY)
			{
				baseForward.z = 0f;
				dir.z = 0f;
			}
			else
			{
				baseForward.y = 0f;
				dir.y = 0f;
			}

			if (Vector3.Angle(baseForward, dir) > maxAngle)
				return;

			var currentForward = plane == Battle.WorldPlane.XY ? rotatingTransform.up : rotatingTransform.forward;
			if (_finder.IsAimedAt(currentForward, dir, AimTolerance))
				Weapon.TryFire(null);
		}

		private bool TryGetCursorDirection(Battle.WorldPlane plane, out Vector3 origin, out Vector3 direction)
		{
			var baseTransform = Weapon.BaseTransform != null ? Weapon.BaseTransform : Weapon.transform;
			var firePoint = Weapon.FirePoint != null ? Weapon.FirePoint.position : baseTransform.position;
			origin = firePoint;

			var cam = Camera.main;
			if (cam == null && Battle.Instance != null && Battle.Instance.CameraController != null)
				cam = Battle.Instance.CameraController.GetComponent<Camera>();

			if (cam == null)
			{
				direction = Vector3.zero;
				return false;
			}

			var planePoint = plane == Battle.WorldPlane.XY
				? new Vector3(0f, 0f, firePoint.z)
				: new Vector3(0f, firePoint.y, 0f);
			if (cam.orthographic)
			{
				var depth = Vector3.Dot(planePoint - cam.transform.position, cam.transform.forward);
				var sp = new Vector3(_cursorScreen.x, _cursorScreen.y, depth);
				var hitWorld = cam.ScreenToWorldPoint(sp);
				direction = hitWorld - origin;
				return direction.sqrMagnitude > 0.0001f;
			}

			direction = Vector3.zero;
			return false;
		}

		private void HandleCursorInput(Vector2 screenPosition)
		{
			_cursorScreen = screenPosition;
		}

		private void HandleFireLmbInput(bool isPressed)
		{
			_fireHeld = isPressed;
		}

		private float GetRotationSpeedDeg()
		{
			if (Weapon?.Model?.Stats != null &&
			    Weapon.Model.Stats.TryGetStat(StatType.RotationSpeed, out var stat) &&
			    stat != null && stat.Current > 0f)
				return stat.Current;

			return RotationSpeedDeg;
		}
	}
}
