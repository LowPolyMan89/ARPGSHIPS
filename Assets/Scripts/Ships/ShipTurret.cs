using UnityEngine;

namespace Ships
{
	public class ShipTurret : MonoBehaviour
	{
		[Header("Rotation")]
		public Transform Pivot;          // Что вращаем (Turret top)
		public Transform BaseTransform;  // Относительно чего ограничиваем угол
		public float RotationSpeed = 180f;
		public float MaxAngle = 180f;    // сектор от центра

		private void Awake()
		{
			if (!Pivot) Pivot = transform;
			if (!BaseTransform) BaseTransform = transform.parent;
		}

		public void RotateTowards(Vector3 worldDirection)
		{
			var plane = Battle.Instance != null && Battle.Instance.Plane == Battle.WorldPlane.XY
				? WeaponRotator.AimPlane.XY
				: WeaponRotator.AimPlane.XZ;

			WeaponRotator.Rotate(
				rotatingTransform: Pivot,
				baseTransform: BaseTransform,
				worldDirection: worldDirection,
				rotationSpeedDeg: RotationSpeed,
				maxAngleDeg: MaxAngle,
				aimPlane: plane
			);
		}
	}
}
