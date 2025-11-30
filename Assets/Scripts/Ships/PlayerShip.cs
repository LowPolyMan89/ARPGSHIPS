using System.Collections.Generic;

namespace Ships
{
	public class PlayerShip : ShipBase
	{
		
		private void Awake()
		{
			ShipStats = new Stats();
			ShipStats.AddStat(new Stat(StatType.HitPoint, 100));
			ShipStats.AddStat(new Stat(StatType.Shield, 50));
			ShipStats.AddStat(new Stat(StatType.MoveSpeed, 10));
			ShipStats.AddStat(new Stat(StatType.TurnSpeed, 3));
			ShipStats.AddStat(new Stat(StatType.BrakePower, 5));
			ShipStats.AddStat(new Stat(StatType.Acceleration, 8));
			ShipStats.AddStat(new Stat(StatType.KineticResist, 5));
			ShipStats.AddStat(new Stat(StatType.ExplosionResist, 5));
			
			var sh = gameObject.GetComponent<ShieldController>();

			// фронт 90°
			sh.AddSector(new ShieldSector(
				ShieldSide.Front, -45, 45, 
				maxHP: 50, regen: 1));

			// левый
			sh.AddSector(new ShieldSector(
				ShieldSide.Left, 45, 135, 
				maxHP: 30, regen: 0.5f));

			// правый
			sh.AddSector(new ShieldSector(
				ShieldSide.Right, -135, -45, 
				maxHP: 30, regen: 0.5f));

			// задний
			sh.AddSector(new ShieldSector(
				ShieldSide.Rear, 135, -135,
				maxHP: 20, regen: 0.3f));
			
			
			Init();
		}
		
	}
}