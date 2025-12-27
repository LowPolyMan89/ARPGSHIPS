using UnityEngine;

namespace Ships
{
	/// <summary>
	/// Создаёт/обновляет элементы статов для меты (энергия, щит, корпус, скорость).
	/// </summary>
	public class MetaStatsController : MonoBehaviour
	{
		[SerializeField] private Color _energyColor = Color.yellow;
		[SerializeField] private Color _shieldColor = Color.cyan;
		[SerializeField] private Color _hpColor = Color.green;
		[SerializeField] private Color _speedColor = Color.magenta;

		private MetaVisual _metaVisual;
		private ShipUiMetaStatVisual _energyUi;
		private ShipUiMetaStatVisual _shieldUi;
		private ShipUiMetaStatVisual _hpUi;
		private ShipUiMetaStatVisual _speedUi;

		private void Awake()
		{
			_metaVisual = GetComponent<MetaVisual>();
		}

		private void Start()
		{
			Refresh();
		}

		public void Refresh()
		{
			var meta = MetaController.Instance;
			if (meta == null)
				return;

			var state = meta.State;
			if (state == null || _metaVisual == null || _metaVisual.StatPrefab == null || _metaVisual.StatRoot == null)
				return;

			var hull = HullLoader.Load(state.SelectedShipId);

			// Удаляем старые.
			DestroyIfExists(_energyUi);
			DestroyIfExists(_shieldUi);
			DestroyIfExists(_hpUi);
			DestroyIfExists(_speedUi);

			// Энергия
			var energyReport = EnergyCalculator.Calculate(state);
			var energyCurrent = Mathf.Max(0f, energyReport.Max - energyReport.Used);
			var energyStat = new Stat(StatType.Energy, energyReport.Max, energyCurrent);
			_energyUi = Instantiate(_metaVisual.StatPrefab, _metaVisual.StatRoot);
			_energyUi.InitFromStat(energyStat, _energyColor, _energyColor);
			_energyUi.SetText($"Energy {energyCurrent}/{energyReport.Max}");

			// Щит
			var shieldMax = hull?.Shield?.Hp ?? 0f;
			var shieldVal = shieldMax;
			var shieldStat = new Stat(StatType.Shield, shieldMax, shieldVal);
			_shieldUi = Instantiate(_metaVisual.StatPrefab, _metaVisual.StatRoot);
			_shieldUi.InitFromStat(shieldStat, _shieldColor, _shieldColor);
			_shieldUi.SetText($"Shield {shieldVal}");

			// Корпус (HP)
			var hpMax = hull?.stats?.HitPoint ?? 0f;
			var hpVal = hpMax;
			var hpStat = new Stat(StatType.HitPoint, hpMax, hpVal);
			_hpUi = Instantiate(_metaVisual.StatPrefab, _metaVisual.StatRoot);
			_hpUi.InitFromStat(hpStat, _hpColor, _hpColor);
			_hpUi.SetText($"HP {hpVal}");

			// Скорость
			var speedVal = hull?.stats?.MoveSpeed ?? 0f;
			var speedStat = new Stat(StatType.MoveSpeed, speedVal, speedVal);
			_speedUi = Instantiate(_metaVisual.StatPrefab, _metaVisual.StatRoot);
			_speedUi.InitFromStat(speedStat, _speedColor, _speedColor);
			_speedUi.SetText($"Speed {speedVal}");
		}

		private void DestroyIfExists(Component c)
		{
			if (c == null)
				return;
			if (Application.isPlaying)
				Destroy(c.gameObject);
			else
				DestroyImmediate(c.gameObject);
		}
	}
}
