using System;
using System.Collections;
using UnityEngine;

namespace Tanks
{
	public class BattleUi : MonoBehaviour
	{
		[SerializeField] private PlayerTankStatsContainerUi _playerTankStatsContainer;
		[SerializeField] private float _updateRatio = 0.01f;
		private void Awake()
		{
			_playerTankStatsContainer = GetComponentInChildren<PlayerTankStatsContainerUi>();
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
			if (_playerTankStatsContainer)
				_playerTankStatsContainer.OnUpdate();
		}
	}
}