using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks
{
	public abstract class TurretAimSystem
	{
		public TankTurret Turret;

		

		public abstract void Init(TankBase tankBase);

		public abstract void Update();
	}
}