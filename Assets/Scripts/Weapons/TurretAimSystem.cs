using UnityEngine;
using UnityEngine.InputSystem;

namespace Ships
{
	public abstract class TurretAimSystem
	{
		public ShipTurret Turret;

		

		public abstract void Init(ShipBase shipBase);

		public abstract void Update();
	}
}
