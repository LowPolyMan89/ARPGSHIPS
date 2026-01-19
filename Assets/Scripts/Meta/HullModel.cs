using System.Collections.Generic;

namespace Ships
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	[Serializable]
	public class HullModel
	{
		public string id;
		public string name;
		public int cost;
		public string BattlePrefab;
		public string MetaPrefab;
		public string shipClass;
		public int Energy;
		public StatContainer stats;
		public List<StatEffectModel> StatEffects;
		public ShieldModel Shield;

		// Grid definitions for meta fitting + in-battle weapon/module placement.
		public float gridCellSize = 0.40f;
		public List<HullGridModel> grids;
		// Socket definitions for the new socket-based fitting.
		public List<HullSocketModel> sockets;
		public List<EffectModel> uniqueEffects;
		public List<LevelModel> leveling;
	}

	[Serializable]
	public class HullGridModel
	{
		public string id;
		public ShipGridType type = ShipGridType.WeaponGrid;
		public int width = 1;
		public int height = 1;

		// Local-space (ship) origin for cell (0,0) bottom-left.
		public Vector2 origin;

		// Optional extra rotation of the grid relative to ship (degrees around Z).
		public float rotationDeg = 0f;
	}

	[Serializable]
	public class HullSocketModel
	{
		public string id;
		public ShipGridType type = ShipGridType.WeaponGrid;
		public string size;
		public Vector2 position;
		public float rotationDeg = 0f;
	}
	[Serializable]
	public class StatContainer
	{
		public float HitPoint;
		public float Shield;
		public float ShieldRegen;
		public float MoveSpeed;
		public float TurnSpeed;
		public float Acceleration;
		public float BrakePower;
		public float MoveSpeedRear;
		public float Evasion;
		public float KineticDamageBonus;
		public float KineticResist;
		public float ThermalDamageBonus;
		public float EnergyDamageBonus;
		public float ThermalResist;
		public float EnergyResist;
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
		public float Hp;
		public float Regen;
		public float RegenDelay;
	}

}
