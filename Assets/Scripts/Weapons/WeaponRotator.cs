using UnityEngine;

namespace Ships
{
	public static class WeaponRotator
	{
		public enum AimPlane
		{
			XZ = 0,
			XY = 1
		}

		/// <summary>
		/// Универсальный 3D ротатор с ограничением сектора относительно baseTransform
		/// </summary>
		public static void Rotate(
			Transform rotatingTransform,
			Transform baseTransform,
			Vector3 worldDirection,
			float rotationSpeedDeg,
			float maxAngleDeg,
			AimPlane aimPlane = AimPlane.XZ)
		{
			if (!rotatingTransform)
				return;

			// Плоскость наведения
			switch (aimPlane)
			{
				case AimPlane.XZ:
					worldDirection.y = 0f;
					break;
				case AimPlane.XY:
					worldDirection.z = 0f;
					break;
				default:
					worldDirection.y = 0f;
					break;
			}

			if (worldDirection.sqrMagnitude < 0.0001f)
				return;

			worldDirection.Normalize();

			// Куда хотим повернуть
			Quaternion desiredWorldRot = aimPlane == AimPlane.XY
				? Quaternion.LookRotation(Vector3.forward, worldDirection)
				: Quaternion.LookRotation(worldDirection, Vector3.up);

			// База (корпус, башня, что угодно)
			Quaternion baseRot = baseTransform ? baseTransform.rotation : Quaternion.identity;

			// Переводим desired в ЛОКАЛЬНОЕ пространство базы
			Quaternion localDesired = Quaternion.Inverse(baseRot) * desiredWorldRot;

			float localYaw;
			if (aimPlane == AimPlane.XY)
				localYaw = Mathf.DeltaAngle(0f, localDesired.eulerAngles.z);
			else
				localYaw = Mathf.DeltaAngle(0f, localDesired.eulerAngles.y);

			// Если есть сектор — ограничиваем
			if (maxAngleDeg < 359f)
			{
				float half = maxAngleDeg * 0.5f;
				localYaw = Mathf.Clamp(localYaw, -half, half);
			}

			// Возвращаем в мир
			Quaternion finalRot = aimPlane == AimPlane.XY
				? baseRot * Quaternion.Euler(0f, 0f, localYaw)
				: baseRot * Quaternion.Euler(0f, localYaw, 0f);

			// Плавное доворачивание
			rotatingTransform.rotation = Quaternion.RotateTowards(
				rotatingTransform.rotation,
				finalRot,
				rotationSpeedDeg * Time.deltaTime
			);
		}
	}
}
