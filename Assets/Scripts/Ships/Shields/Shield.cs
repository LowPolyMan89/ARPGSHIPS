using UnityEngine;

namespace Ships
{
	[System.Serializable]
	public class Shield
	{
		public float MaxHP;
		public float Regen;
		public float RestoreDelayTime;
		public Stat ShieldHP;
		public Stat ShieldRegen;
		public Stat RestoreDelay;
		public Collider Collider;
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
			Visual.Init();
			Visual.SetCharge(ShieldHP.Current / ShieldHP.Maximum);
		}
		public void InitFromConfig(float hp, float regen, float restoreDelay)
		{
			ShieldHP = new Stat(StatType.Shield, hp);
			ShieldRegen = new Stat(StatType.ShieldRegen, regen);
			RestoreDelay = new Stat(StatType.ShieldRestoreDelay, restoreDelay);
			Visual.Init();
			Visual.SetCharge(ShieldHP.Current / ShieldHP.Maximum);
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

			// Если щит упал — запускаем задержку
			if (ShieldHP.Current <= 0 && !IsRestoring)
			{
				CurrentRestoreTime = RestoreDelay.Current;
				IsRestoring = true;
			}

			// Обрабатываем задержку
			if (IsRestoring)
			{
				CurrentRestoreTime -= 1f;

				if (CurrentRestoreTime <= 0)
				{
					IsRestoring = false;
					ShieldHP.AddToCurrent(ShieldHP.Maximum * 0.1f);
				}
				GameEvent.UiUpdate();
				return;
			}

			// Нормальная регенерация раз в секунду
			if (ShieldHP.Current < ShieldHP.Maximum)
			{
				ShieldHP.AddToCurrent(ShieldRegen.Current);
			}
			GameEvent.UiUpdate();
		}


	}
}
