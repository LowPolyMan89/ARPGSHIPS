using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ships
{
	public class ShipFitVisual : MonoBehaviour
	{
		private ShipFitView _view;
		public TMP_Text _energyText;

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

			if (_view != null)
			{
				_view.OnFitChanged -= RefreshEnergy;
				_view.OnFitChanged += RefreshEnergy;
				RefreshEnergy();
			}
		}

		private void OnDestroy()
		{
			if (_view != null)
				_view.OnFitChanged -= RefreshEnergy;
		}

		public void RefreshEnergy()
		{
			if (_view == null || _energyText == null)
				return;

			var energy = _view.CalculateEnergy();
			_energyText.text = $"{Mathf.RoundToInt(energy.Used)}/{Mathf.RoundToInt(energy.Available)}";
			_energyText.color = energy.Used > energy.Available ? Color.red : Color.white;
		}
	}
}
