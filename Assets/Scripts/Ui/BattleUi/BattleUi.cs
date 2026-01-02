using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ships
{
	public class BattleUi : MonoBehaviour
	{
		[FormerlySerializedAs("_playerTankStatsContainer")]
		[SerializeField] private PlayerShipStatsContainerUi _playerShipStatsContainer;
		[SerializeField] private float _updateRatio = 0.01f;
		[SerializeField] private WeaponsUi _weaponsUi;
		private void Awake()
		{
			_playerShipStatsContainer = GetComponentInChildren<PlayerShipStatsContainerUi>();
		}

		private void Start()
		{
			GameEvent.OnUiUpdate += OnUpdate;
			StartCoroutine(InitWeaponSlotsRoutine());
		}

		private void OnDestroy()
		{
			GameEvent.OnUiUpdate -= OnUpdate;
		}

		private void OnUpdate()
		{
			if (_playerShipStatsContainer)
				_playerShipStatsContainer.OnUpdate();
			foreach (var weaponSlot in _weaponsUi.ActiveSlots)
			{
				weaponSlot.OnUpdate();
			}
		}

		private IEnumerator InitWeaponSlotsRoutine()
		{
			while (Battle.Instance == null || Battle.Instance.Player == null)
				yield return null;

			BuildWeaponSlots(Battle.Instance.Player);
		}

		private void BuildWeaponSlots(PlayerShip player)
		{
			if (_weaponsUi == null || _weaponsUi.UiWeaponSlotPrefab == null || _weaponsUi._weaponsRoot == null || player == null)
				return;

			_weaponsUi.ActiveSlots.Clear();
			for (int i = _weaponsUi._weaponsRoot.childCount - 1; i >= 0; i--)
			{
				var child = _weaponsUi._weaponsRoot.GetChild(i);
				if (child != null)
					Destroy(child.gameObject);
			}

			var weapons = player.GetComponentsInChildren<WeaponBase>(true);
			for (int i = 0; i < weapons.Length; i++)
			{
				var weapon = weapons[i];
				if (weapon == null)
					continue;

				var slot = Instantiate(_weaponsUi.UiWeaponSlotPrefab, _weaponsUi._weaponsRoot);
				slot.Init(weapon);
				_weaponsUi.ActiveSlots.Add(slot);
			}
		}
	}

	[System.Serializable]
	public class WeaponsUi
	{
		public Transform _weaponsRoot;
		public BattleUiWeaponSlot UiWeaponSlotPrefab;
		public List<BattleUiWeaponSlot> ActiveSlots = new List<BattleUiWeaponSlot>();
	}
	
}
