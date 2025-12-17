using System;
using System.Collections.Generic;

namespace Ships
{
	public static class GridPlacementUtility
	{
		public static bool CanPlaceRect(
			int gridWidth,
			int gridHeight,
			IReadOnlyList<ShipFitModel.GridPlacement> existing,
			int x,
			int y,
			int width,
			int height,
			string ignoreItemId = null)
		{
			if (width <= 0 || height <= 0)
				return false;

			if (x < 0 || y < 0)
				return false;

			if (x + width > gridWidth || y + height > gridHeight)
				return false;

			for (var i = 0; i < existing.Count; i++)
			{
				var p = existing[i];
				if (!string.IsNullOrEmpty(ignoreItemId) && p.ItemId == ignoreItemId)
					continue;

				if (RectsOverlap(x, y, width, height, p.X, p.Y, p.Width, p.Height))
					return false;
			}

			return true;
		}

		private static bool RectsOverlap(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
		{
			var aRight = ax + aw;
			var aTop = ay + ah;
			var bRight = bx + bw;
			var bTop = by + bh;

			return ax < bRight && aRight > bx && ay < bTop && aTop > by;
		}
	}
}

