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

			MetaBattleBridge.LoadFit(state);
			SceneManager.LoadScene("Battle");
		}
	}
}
