using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ships
{
#if UNITY_EDITOR
	public sealed class AiDebugOverlay : MonoBehaviour
	{
		private static bool _enabled = false;
		private static readonly Vector2 ScrollStart = Vector2.zero;
		private static Vector2 _scroll = ScrollStart;

		private void Update()
		{
			if (!Application.isPlaying)
				return;

			if (WasTogglePressed())
				_enabled = !_enabled;
		}

		[MenuItem("Tools/ShouAiDebug")]
		private static void ToggleFromMenu()
		{
			_enabled = !_enabled;
		}

		[MenuItem("Tools/ShouAiDebug", true)]
		private static bool ToggleFromMenuValidate()
		{
			Menu.SetChecked("Tools/ShouAiDebug", _enabled);
			return true;
		}

		private void OnGUI()
		{
			if (!Application.isPlaying || !_enabled)
				return;

			var brains = FindObjectsOfType<AiShipBrain>();
			var width = 420f;
			var height = Mathf.Min(Screen.height - 20f, 240f + brains.Length * 20f);

			GUILayout.BeginArea(new Rect(10f, 10f, width, height), GUI.skin.box);
			GUILayout.Label($"AI Debug (F1) - Ships: {brains.Length}");

			_scroll = GUILayout.BeginScrollView(_scroll, false, true);
			for (var i = 0; i < brains.Length; i++)
			{
				var brain = brains[i];
				if (brain == null)
					continue;

				var ship = brain.Ship;
				var target = brain.CurrentTarget;
				var dist = target != null
					? Vector3.Distance(ship.transform.position, target.transform.position)
					: 0f;

				var line = $"{ship.name} [{ship.Class}] " +
				           $"State={brain.CurrentState} " +
				           $"Target={(target != null ? target.name : "None")} " +
				           $"Dist={dist:0.0} " +
				           $"Range={brain.DesiredRange:0.0} " +
				           $"Focus={brain.FocusLimit}";

				GUILayout.Label(line);
			}

			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Create()
		{
			if (Object.FindObjectOfType<AiDebugOverlay>() != null)
				return;

			var go = new GameObject("AiDebugOverlay");
			go.hideFlags = HideFlags.DontSave;
			Object.DontDestroyOnLoad(go);
			go.AddComponent<AiDebugOverlay>();
		}

		private static bool WasTogglePressed()
		{
#if ENABLE_INPUT_SYSTEM
			return Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetKeyDown(KeyCode.F1);
#else
			return false;
#endif
		}
	}
#endif
}
