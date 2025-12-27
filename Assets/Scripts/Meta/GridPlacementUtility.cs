using System.Collections.Generic;

namespace Ships
{
	/// <summary>
	/// Утилита для проверки размещения прямоугольников в сетке.
	/// </summary>
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

			if (existing == null)
				return true;

			for (var i = 0; i < existing.Count; i++)
			{
				var p = existing[i];
				if (p == null)
					continue;

				if (!string.IsNullOrEmpty(ignoreItemId) && p.ItemId == ignoreItemId)
					continue;

				var aRight = x + width;
				var aTop = y + height;
				var bRight = p.X + p.Width;
				var bTop = p.Y + p.Height;
				var overlap = x < bRight && aRight > p.X && y < bTop && aTop > p.Y;
				if (overlap)
					return false;
			}

			return true;
		}
	}
}
