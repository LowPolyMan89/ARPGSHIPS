using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
	public class PlayerTurretAimSystem : TurretAimSystem
	{
		private Camera _cam;

		/// <summary>
		/// Возвращает точку на земле (XZ), куда смотрит курсор.
		/// </summary>
		public override void Init(TankBase tankBase)
		{
			_cam = Camera.main;
		}

		public override void Update()
		{
			if (Turret == null)
				return;

			Vector3? worldPos = GetMouseWorldPoint();
			if (worldPos == null)
				return;

			Vector3 dir = worldPos.Value - Turret.transform.position;
			Turret.RotateTowards(dir);
		}

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