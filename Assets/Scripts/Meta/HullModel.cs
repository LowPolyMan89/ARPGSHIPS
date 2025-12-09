using System.Collections.Generic;

namespace Tanks
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class HullModel
	{
		public string id;
		public string name;
		public int cost;

		public StatContainer stats;
		public List<ShieldModel> shields;
		public List<WeaponSlotModel> weaponSlots;
		public List<EffectModel> uniqueEffects;
		public List<LevelModel> leveling;
	}
	[Serializable]
	public class StatContainer
	{
		public float HitPoint;
		public float Armor;
		public float Shield;
		public float ShieldRegen;
		public float MoveSpeed;
		public float TurnSpeed;
		public float Acceleration;
		public float BrakePower;
		public float MoveSpeedRear;
	}
	
	[Serializable]
	public class EffectModel
	{
		public string id;
		public float value;
	}
	
	[Serializable]
	public class LevelModel
	{
		public int level;
		public int xpRequired;
	}
	
	[Serializable]
	public class ShieldModel
	{
		public string id;
		public float value;
		public float restoreTime;
		public float regeneration;
	}

	[Serializable]
	public class WeaponSlotModel
	{
		public string id;
		public string size;
		public float rotationLimitDeg;
	}

}