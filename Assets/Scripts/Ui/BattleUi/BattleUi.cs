using System;
using System.Collections;
using UnityEngine;

namespace Ships
{
	public class BattleUi : MonoBehaviour
	{
		[SerializeField] private PlayerShipStatsContainerUi _playerShipStatsContainer;
		[SerializeField] private float _updateRatio = 0.01f;
		private void Awake()
		{
			_playerShipStatsContainer = GetComponentInChildren<PlayerShipStatsContainerUi>();
		}

		private void Start()
		{
			GameEvent.OnUiUpdate += OnUpdate;
		}

		private void OnDestroy()
		{
			GameEvent.OnUiUpdate -= OnUpdate;
		}

		private void OnUpdate()
		{
			if (_playerShipStatsContainer)
				_playerShipStatsContainer.OnUpdate();
		}
	}
}