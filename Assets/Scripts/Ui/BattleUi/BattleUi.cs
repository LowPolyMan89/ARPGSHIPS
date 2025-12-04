using System;
using UnityEngine;

namespace Ships
{
	public class BattleUi : MonoBehaviour
	{
		[SerializeField] private PlayerShipStatsContainerUi _playerShipStatsContainer;
		private void Awake()
		{
			_playerShipStatsContainer = GetComponentInChildren<PlayerShipStatsContainerUi>();
		}

		private void Update()
		{
			if (_playerShipStatsContainer)
				_playerShipStatsContainer.Update();

		}
	}
}