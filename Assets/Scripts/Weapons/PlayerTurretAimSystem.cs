using UnityEngine;
using UnityEngine.InputSystem;

namespace Ships
{
	public class PlayerTurretAimSystem : TurretAimSystem
	{
		private Camera _cam;
		private Plane _aimPlane;

		/// <summary>
		/// Возвращает точку на земле (XZ), куда смотрит курсор.
		/// </summary>
		public override void Init(ShipBase shipBase)
		{
			_cam = Camera.main;

			var worldPlane = Battle.Instance != null ? Battle.Instance.Plane : Battle.WorldPlane.XZ;
			_aimPlane = worldPlane == Battle.WorldPlane.XY
				? new Plane(Vector3.forward, new Vector3(0f, 0f, Turret ? Turret.transform.position.z : 0f))
				: new Plane(Vector3.up, Vector3.zero);
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

			if (_aimPlane.Raycast(ray, out var dist))
				return ray.GetPoint(dist);

			return null;
		}
	}
}
