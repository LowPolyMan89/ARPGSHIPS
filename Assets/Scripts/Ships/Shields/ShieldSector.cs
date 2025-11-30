using UnityEngine;

namespace Ships
{
	[System.Serializable]
	public class ShieldSector
	{
		public ShieldSide Side;

		public float StartAngle;
		public float EndAngle;

		public Stat ShieldHP;
		public Stat ShieldRegen;

		public float DamageResist = 0f; // 0.2 = 20%

		public ShieldSector(ShieldSide shieldSide, float start, float end, float maxHP, float regen, float resist = 0f)
		{
			Side = shieldSide;
			StartAngle = start;
			EndAngle = end;

			ShieldHP = new Stat(StatType.Shield, maxHP);
			ShieldRegen = new Stat(StatType.ShieldRegen, regen);

			DamageResist = resist;
		}

		public bool ContainsAngle(float angle)
		{
			if (StartAngle <= EndAngle)
			{
				return angle >= StartAngle && angle <= EndAngle;
			}
			else
			{
				return angle >= StartAngle || angle <= EndAngle;
			}
		}

		public float Absorb(float damage)
		{
			if (ShieldHP.Current <= 0)
				return damage;

			damage *= (1f - DamageResist);

			float taken = Mathf.Min(damage, ShieldHP.Current);
			ShieldHP.AddToCurrent(-taken);

			return damage - taken;
		}

		public void Tick()
		{
			ShieldHP.AddToCurrent(ShieldRegen.Current);
		}
	}
}