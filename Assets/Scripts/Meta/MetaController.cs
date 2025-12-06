namespace Ships
{
	using UnityEngine;

	public class MetaController : MonoBehaviour
	{
		public static MetaController Instance;

		public MetaState State { get; private set; }
		public PlayerMetaModel PlayerMetaModel { get; private set; }

		[SerializeField] private MetaVisual _metaVisual;

		private ShipFitView _shipFitView;
		private InventoryView _inventoryView;

		private void Awake()
		{
			if (!Instance)
				Instance = this;
			else
				Destroy(gameObject);

			PlayerMetaModel = new PlayerMetaModel();
			State = MetaSaveSystem.Load();

			_inventoryView = new InventoryView();
			_shipFitView = new ShipFitView();

			_inventoryView.Init(State);
			_shipFitView.Init(State, _inventoryView);

			_metaVisual.ShipFitVisual.Init(_shipFitView);
			_metaVisual.InventoryVisual.Init(_inventoryView);
		}
	}
}