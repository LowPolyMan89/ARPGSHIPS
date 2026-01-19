using System.Collections.Generic;
using UnityEngine;

namespace Ships
{
	public class ShipFitSlotsController : MonoBehaviour
	{
		[SerializeField] private ShipFitSlotUi _slotPrefab;
		[SerializeField] private Transform _slotsRoot;

		private readonly List<ShipFitSlotUi> _slots = new();
		private ShipSocketVisual[] _sockets;
		private MetaState _state;
		private PlayerShip _ship;
		private Transform _shipRoot;

		public void BindShip(GameObject shipGo, MetaState state)
		{
			_state = state;
			_ship = shipGo != null ? shipGo.GetComponent<PlayerShip>() : null;
			_shipRoot = shipGo != null ? shipGo.transform : null;
			_sockets = shipGo != null ? shipGo.GetComponentsInChildren<ShipSocketVisual>(true) : null;

			BuildSlots();
			RefreshSlots();
		}

		private void BuildSlots()
		{
			if (_slotPrefab == null || _slotsRoot == null)
				return;

			ClearSlots();

			if (_sockets == null || _sockets.Length == 0)
				return;

			for (var i = 0; i < _sockets.Length; i++)
			{
				var socket = _sockets[i];
				if (socket == null)
					continue;

				var slot = Instantiate(_slotPrefab, _slotsRoot);
				var root = _shipRoot != null ? _shipRoot : socket.transform.root;
				slot.Bind(socket, _state, _ship, root);
				_slots.Add(slot);
			}
		}

		public void RefreshSlots()
		{
			for (var i = 0; i < _slots.Count; i++)
			{
				var slot = _slots[i];
				if (slot != null)
					slot.Refresh();
			}
		}

		private void ClearSlots()
		{
			for (var i = 0; i < _slots.Count; i++)
			{
				var slot = _slots[i];
				if (slot == null)
					continue;

				if (Application.isPlaying)
					Destroy(slot.gameObject);
				else
					DestroyImmediate(slot.gameObject);
			}

			_slots.Clear();
		}
	}
}
