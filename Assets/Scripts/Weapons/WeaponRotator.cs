using UnityEngine;

namespace Tanks
{
	public static class WeaponRotator
	{
		/// <summary>
		/// Универсальный 3D ротатор с ограничением сектора относительно baseTransform
		/// </summary>
		public static void Rotate(
			Transform rotatingTransform,
			Transform baseTransform,
			Vector3 worldDirection,
			float rotationSpeedDeg,
			float maxAngleDeg)
		{
			if (!rotatingTransform)
				return;

			// Работает в плоскости XZ
			worldDirection.y = 0;
			if (worldDirection.sqrMagnitude < 0.0001f)
				return;

			worldDirection.Normalize();

			// Куда хотим повернуть
			Quaternion desiredWorldRot = Quaternion.LookRotation(worldDirection, Vector3.up);

			// База (корпус, башня, что угодно)
			Quaternion baseRot = baseTransform ? baseTransform.rotation : Quaternion.identity;

			// Переводим desired в ЛОКАЛЬНОЕ пространство базы
			Quaternion localDesired = Quaternion.Inverse(baseRot) * desiredWorldRot;

			float localYaw = Mathf.DeltaAngle(0f, localDesired.eulerAngles.y);

			// Если есть сектор — ограничиваем
			if (maxAngleDeg < 359f)
			{
				float half = maxAngleDeg * 0.5f;
				localYaw = Mathf.Clamp(localYaw, -half, half);
			}

			// Возвращаем в мир
			Quaternion finalRot = baseRot * Quaternion.Euler(0, localYaw, 0);

			// Плавное доворачивание
			rotatingTransform.rotation = Quaternion.RotateTowards(
				rotatingTransform.rotation,
				finalRot,
				rotationSpeedDeg * Time.deltaTime
			);
		}
	}
}