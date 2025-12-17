using UnityEngine;
using UnityEngine.InputSystem;

namespace Ships
{
	public class PlayerTurretAimSystem : TurretAimSystem
	{
		private Camera _cam;

		/// <summary>
		/// Возвращает точку на земле (XZ), куда смотрит курсор.
		/// </summary>
		public override void Init(ShipBase shipBase)
		{
			_cam = Camera.main;
		}

		public override void Update()
		{
			if (Turret == null)
				return;

			var worldPos = GetMouseWorldPoint();
			if (worldPos == null)
				return;

			var dir = worldPos.Value - Turret.transform.position;
			Turret.RotateTowards(dir);
		}

		private Vector3? GetMouseWorldPoint()
		{
			var mousePos = Mouse.current.position.ReadValue();
			var ray = _cam.ScreenPointToRay(mousePos);

			var ground = new Plane(Vector3.up, Vector3.zero);

			if (ground.Raycast(ray, out var dist))
				return ray.GetPoint(dist);

			return null;
		}
	}
}
