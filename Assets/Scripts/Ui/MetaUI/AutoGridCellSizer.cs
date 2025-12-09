namespace Tanks
{
	using UnityEngine;
	using UnityEngine.UI;

	[RequireComponent(typeof(GridLayoutGroup))]
	[ExecuteInEditMode]
	public class AutoGridCellSizer : MonoBehaviour
	{
		[SerializeField] private RectTransform _rect;
		[SerializeField] private GridLayoutGroup _grid;

		public int columns = 5; // число колонок

		private void Update()
		{
			if(!_rect && !_grid)
				return;
			float width = _rect.rect.width;
			float spacing = _grid.spacing.x * (columns - 1);
			float padding = _grid.padding.left + _grid.padding.right;

			float cellSize = (width - spacing - padding) / columns;

			_grid.cellSize = new Vector2(cellSize, cellSize);
		}
	}

}