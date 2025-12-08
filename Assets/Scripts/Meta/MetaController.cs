using Tanks.Ships;

namespace Tanks
{
	using UnityEngine;

	public class MetaController : MonoBehaviour
	{
		public static MetaController Instance;

		public MetaState State { get; private set; }

		[SerializeField] private MetaVisual _metaVisual;

		private TankFitView _tankFitView;
		private InventoryView _inventoryView;
		public TankFitView TankFitView => _tankFitView;
		public MetaVisual MetaVisual => _metaVisual;

		private void Awake()
		{
			if (!Instance)
				Instance = this;
			else
				Destroy(gameObject);
			State = MetaSaveSystem.Load();

			_inventoryView = new InventoryView();
			_tankFitView = new TankFitView();

			_inventoryView.Init(State);
			_tankFitView.Init(State, _inventoryView);
			
			if (State.InventoryModel.InventoryUniqueItems.Count == 0)
			{
				GiveStarterItems();
				MetaSaveSystem.Save(State);
			}

			_metaVisual._tankFitVisual.Init(_tankFitView);
			_metaVisual.InventoryVisual.Init(_inventoryView);
		}
		private void GiveStarterItems()
		{
			var w = ItemGenerator.GenerateWeapon("weapon_p_small_bolter_1.json", "Common");

			State.InventoryModel.InventoryUniqueItems.Add(new InventoryItem
			{
				ItemId = w.ItemId,
				TemplateId = w.TemplateId
			});
		}
	}
}