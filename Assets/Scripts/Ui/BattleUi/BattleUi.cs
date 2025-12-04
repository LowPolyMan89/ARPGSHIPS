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
			StartCoroutine(UpdateTick());
		}

		private IEnumerator UpdateTick()
		{
			while (gameObject.activeInHierarchy)
			{
				yield return new WaitForSeconds(_updateRatio);
				OnUpdate();
			}
		}
		private void OnUpdate()
		{
			if (_playerShipStatsContainer)
				_playerShipStatsContainer.OnUpdate();
		}
	}
}