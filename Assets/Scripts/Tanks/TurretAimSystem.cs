using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
	public class TurretAimSystem
	{
		public TankTurret Turret;

		private Camera _cam;

		public void Init()
		{
			_cam = Camera.main;
		}

		public void Update()
		{
			if (Turret == null)
				return;

			Vector3? worldPos = GetMouseWorldPoint();
			if (worldPos == null)
				return;

			Vector3 dir = worldPos.Value - Turret.TurretTransform.position;
			Turret.Rotate(dir);
		}

		/// <summary>
		/// Возвращает точку на земле (XZ), куда смотрит курсор.
		/// </summary>
		private Vector3? GetMouseWorldPoint()
		{
			Vector2 mousePos = Mouse.current.position.ReadValue();
			Ray ray = _cam.ScreenPointToRay(mousePos);

			Plane ground = new Plane(Vector3.up, Vector3.zero);

			if (ground.Raycast(ray, out float dist))
				return ray.GetPoint(dist);

			return null;
		}
	}
}