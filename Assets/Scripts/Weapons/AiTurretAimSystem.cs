using System.Reflection;
using Tanks;
using UnityEngine;

public class AiTurretAimSystem : TurretAimSystem
{
	private UniversalTargetingSystem targeting = new();
	private IAimOrigin origin;

	public float Range = 25f;
	public float Sector = 180f;

	public override void Init(TankBase tankBase)
	{
		// устанавливаем маску враждебности
		if (tankBase.SideType == SideType.Enemy)
			tankBase.Turret.SetHitMask(TeamMask.Player);
		else if (tankBase.SideType == SideType.Player)
			tankBase.Turret.SetHitMask(TeamMask.Enemy);
		else
			tankBase.Turret.SetHitMask(TeamMask.All);

		targeting.Init(tankBase.Turret);
	}
	

	public override void Update()
	{
		targeting.UpdateTargets(Battle.Instance.AllTanks);

		var target = targeting.GetTarget();
		if (target == null)
			return;

		Turret.Rotate(targeting.GetAimDirection(target));
	}
}