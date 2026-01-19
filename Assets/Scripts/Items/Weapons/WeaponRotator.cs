using UnityEngine;

namespace Ships
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

			// Наведение в плоскости XZ (Y вверх)
			worldDirection.y = 0f;

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
			Quaternion finalRot = baseRot * Quaternion.Euler(0f, localYaw, 0f);

			// Плавное доворачивание
			rotatingTransform.rotation = Quaternion.RotateTowards(
				rotatingTransform.rotation,
				finalRot,
				rotationSpeedDeg * Time.deltaTime
			);
		}

		public static void RotatePitch(
			Transform barrelTransform,
			Transform yawTransform,
			Vector3 worldDirection,
			float rotationSpeedDeg,
			float minPitchDeg,
			float maxPitchDeg,
			Quaternion restLocalRotation)
		{
			if (!barrelTransform)
				return;

			if (worldDirection.sqrMagnitude < 0.0001f)
				return;

			worldDirection.Normalize();

			var yawRotation = yawTransform ? yawTransform.rotation : Quaternion.identity;
			var localDir = Quaternion.Inverse(yawRotation) * worldDirection;

			if (localDir.sqrMagnitude < 0.0001f)
				return;

			localDir.Normalize();

			var restForward = restLocalRotation * Vector3.forward;
			var pitchAxis = restLocalRotation * Vector3.right;
			var pitch = Vector3.SignedAngle(restForward, localDir, pitchAxis);
			pitch = Mathf.Clamp(pitch, minPitchDeg, maxPitchDeg);

			var desiredLocal = restLocalRotation * Quaternion.AngleAxis(pitch, Vector3.right);

			barrelTransform.localRotation = Quaternion.RotateTowards(
				barrelTransform.localRotation,
				desiredLocal,
				rotationSpeedDeg * Time.deltaTime
			);
		}
	}
}
