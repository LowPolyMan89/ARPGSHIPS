using UnityEditor;
using UnityEngine;

namespace Tanks
{
	[CustomEditor(typeof(WeaponBase), true)]
	public class WeaponBaseEditor : UnityEditor.Editor
	{
		private bool _showStats;

		public override void OnInspectorGUI()
		{
			if (Application.isPlaying)
				Repaint();
			// Рисуем стандартный инспектор
			DrawDefaultInspector();

			var weapon = (WeaponBase)target;

			EditorGUILayout.Space(10);

			_showStats = EditorGUILayout.Foldout(_showStats, "Stats Debug Info");

			if (_showStats)
			{
				if (weapon.Model == null || weapon.Model.Stats == null)
				{
					EditorGUILayout.HelpBox("Stats not initialized", MessageType.Info);
					return;
				}

				EditorGUILayout.BeginVertical("box");

				foreach (var stat in weapon.Model.Stats.All)
				{
					EditorGUILayout.LabelField(stat.Key.ToString(), $"{stat.Value.Current.ToString()} / {stat.Value.Maximum.ToString()}");
				}
				EditorGUILayout.LabelField($"Reloading", $"{weapon.ReloadFinishTime.ToString()}");
				EditorGUILayout.LabelField($"RateTime", $"{weapon.NextFireTime.ToString()}");
				EditorGUILayout.LabelField($"Ammo", $"{weapon.Ammo.ToString()}");
				EditorGUILayout.EndVertical();
			}
		}
	}
}