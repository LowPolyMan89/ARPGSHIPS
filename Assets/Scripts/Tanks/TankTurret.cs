using UnityEngine;

namespace Tanks
{
	public class TankTurret : MonoBehaviour, IAimOrigin
	{
		public float RotationSpeed = 180f;
		public float MaxAngle = 360f;
		[SerializeField] private float _detectionRange = 30f;
		public Transform TurretTransform;
		public Transform BaseTransform;
		public Vector3 Position => transform.position;
		public Vector3 Forward => transform.forward;
		
		[SerializeField] private TeamMask _hitMask;
		public TeamMask HitMask => _hitMask;
		public float AllowedAngle => MaxAngle;
		public float DetectionRange => _detectionRange;

		private void Awake()
		{
			if (!TurretTransform)
				TurretTransform = transform;

			if (!BaseTransform)
				BaseTransform = transform.parent;
		}

		public void Rotate(Vector3 worldDirection)
		{
			WeaponRotator.Rotate(
				rotatingTransform: TurretTransform,
				baseTransform: BaseTransform,
				worldDirection: worldDirection,
				rotationSpeedDeg: RotationSpeed,
				maxAngleDeg: MaxAngle
			);
		}

		public void SetHitMask(TeamMask mask)
		{
			_hitMask = mask;
		}
	}
}