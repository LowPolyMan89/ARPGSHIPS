using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ships
{
	public class StartBattle : MonoBehaviour
	{
		public void StartBattleButton()
		{
			var state = MetaController.Instance != null ? MetaController.Instance.State : null;
			if (state == null)
			{
				Debug.LogWarning("[StartBattle] Meta state is missing, cannot start battle");
				return;
			}

			var energy = EnergyCalculator.Calculate(state);
			if (!energy.CanStart)
			{
				Debug.LogWarning($"[StartBattle] Not enough energy: used {energy.Used}/{energy.Max} (base {energy.BaseMax}, bonus {energy.BonusMax})");
				return;
			}

			MetaBattleBridge.LoadFit(state);
			SceneManager.LoadScene("Battle");
		}
	}
}
