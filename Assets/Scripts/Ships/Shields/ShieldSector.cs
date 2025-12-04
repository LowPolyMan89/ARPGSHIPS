using UnityEngine;

namespace Ships
{
	[System.Serializable]
	public class ShieldSector
	{
		public ShieldSide Side;

		public float StartAngle;
		public float EndAngle;
		public float MaxHP;
		public float Regen;
		public float RestoreDelayTime;
		public Stat ShieldHP;
		public Stat ShieldRegen;
		public Stat RestoreDelay;
		public Collider2D Collider;
		public bool IsRestoring;
		public float CurrentRestoreTime;
		public float DamageResist = 0f;
		public ShieldSectorVisual Visual;
		public float CurrentHp;// 0.2 = 20%

		public void InitFromPrefab()
		{
			ShieldHP = new Stat(StatType.Shield, MaxHP);
			ShieldRegen = new Stat(StatType.ShieldRegen, Regen);
			RestoreDelay = new Stat(StatType.ShieldRestoreDelay, RestoreDelayTime);
		}
		public void InitFromConfig(float hp, float regen, float restoreDelay)
		{
			ShieldHP = new Stat(StatType.Shield, hp);
			ShieldRegen = new Stat(StatType.ShieldRegen, regen);
			RestoreDelay = new Stat(StatType.ShieldRestoreDelay, restoreDelay);
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
			{
				return damage;
			}
			damage *= (1f - DamageResist);
			float taken = Mathf.Min(damage, ShieldHP.Current);
			ShieldHP.AddToCurrent(-taken);
			return damage - taken;
		}

		public void Tick()
		{
			CurrentHp = ShieldHP.Current;
			Collider.enabled = ShieldHP.Current > 0;
			if (ShieldHP.Current <= 0 && !IsRestoring)
			{
				CurrentRestoreTime = RestoreDelay.Current;
				IsRestoring = true;
			}
			if (IsRestoring)
			{
				CurrentRestoreTime --;
				if (CurrentRestoreTime <= 0)
				{
					IsRestoring = false;
					CurrentRestoreTime = RestoreDelay.Current;
					ShieldHP.AddToCurrent(ShieldRegen.Current);
				}
			}
			else
				ShieldHP.AddToCurrent(ShieldRegen.Current);
		}
	}
}