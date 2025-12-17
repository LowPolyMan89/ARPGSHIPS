using System;
using System.Collections.Generic;

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

			public int X;
			public int Y;
			public int Width = 1;
			public int Height = 1;
		}
	}

}
