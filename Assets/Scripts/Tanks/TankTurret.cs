using UnityEngine;

namespace Tanks
{
	public class TankTurret : MonoBehaviour
	{
		public float RotationSpeed = 180f;
		public float MaxAngle = 360f;

		public Transform TurretTransform;
		public Transform BaseTransform;

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
	}
}