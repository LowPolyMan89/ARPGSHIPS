using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class ShipFitVisual : MonoBehaviour
	{
		private ShipFitView _view;

		[Header("Grid UI")]
		public List<ShipGridVisual> Grids = new();

		public void Init(ShipFitView view)
		{
			_view = view;

			foreach (var grid in Grids)
			{
				if (grid != null)
					grid.Init(view);
			}
		}
	}
}