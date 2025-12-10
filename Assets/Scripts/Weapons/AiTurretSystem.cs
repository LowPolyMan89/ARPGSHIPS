using UnityEngine;

namespace Tanks
{
	public class AiTurretSystem : MonoBehaviour
	{
		public TankTurret Turret;
		public TeamMask HitMask;

		public float Range = 30f;
		public float Sector = 180f;
		public float AimTolerance = 5f;

		private TargetFinder finder = new();

		private void Update()
		{
			// обновляем список целей
			finder.UpdateTargets(Battle.Instance.AllTanks, HitMask);

			// выбираем лучшую цель
			var target = finder.FindBestTarget(
				Turret.Pivot.position,
				Turret.Pivot.forward,
				Sector,
				Range
			);

			if (target == null)
				return;

			Vector3 dir = target.Transform.position - Turret.Pivot.position;
			dir.y = 0; // 2D-плоскость

			// ВРАЩАЕМ БАШНЮ
			AimDriver.Rotate(Turret.Pivot, dir, Turret.RotationSpeed);

			// Если захочешь — тут можно добавлять стрельбу через WeaponTargeting
		}
	}
}