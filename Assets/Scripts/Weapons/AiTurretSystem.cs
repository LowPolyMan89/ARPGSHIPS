using UnityEngine;

namespace Ships
{
	public class AiTurretSystem : MonoBehaviour
	{
		public ShipTurret Turret;
		public TeamMask HitMask;
		[SerializeField] private LayerMask _obstacle;
		public float Range = 30f;
		public float Sector = 180f;
		public float AimTolerance = 5f;

		private TargetFinder finder = new();

		private void Update()
		{
			// обновляем список целей
			finder.UpdateTargets(Battle.Instance.AllShips, HitMask);

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

			if(!LineOfSightUtility.HasLOS(Turret.Pivot.position, target.Transform.position, _obstacle))
				return;
			// ВРАЩАЕМ БАШНЮ
			AimDriver.Rotate(Turret.Pivot, dir, Turret.RotationSpeed);

			// Если захочешь — тут можно добавлять стрельбу через WeaponTargeting
		}
	}
}
