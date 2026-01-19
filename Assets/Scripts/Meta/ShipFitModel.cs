using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	// Grid type for ship fitting (meta).
	public enum ShipGridType
	{
		WeaponGrid = 0,
		ModuleGrid = 1
	}

	[Serializable]
	public class ShipFitModel
	{
		public string ShipId;

		// Grid-based fit (meta).
		public List<GridPlacement> GridPlacements = new();

		[Serializable]
		public class GridPlacement
		{
			public string GridId;
			public ShipGridType GridType;
			public string ItemId;

			// Старые значения в клетках грида (оставлены для совместимости и импортов).
			public int X;
			public int Y;
			public int Width = 1;
			public int Height = 1;

			// Новая запись: позиция и поворот внутри грида (локальные единицы грида, от нижнего левого угла).
			public Vector2 Position;
			public float RotationDeg;

			// 3D pose relative to the ship root.
			public Vector3 LocalPosition;
			public Vector3 LocalEuler;
			public bool HasLocalPose;
		}
	}

}
