using UnityEditor;
using UnityEngine;

namespace Ships.Editor
{
	public class ItemGeneratorEditor
	{
		[MenuItem("Tools/Generate Item ID")]
		public static void Generate()
		{
			var id = Services.UniqueIdGenerator.GenerateItemId();

			// копируем в буфер обмена
			EditorGUIUtility.systemCopyBuffer = id;

			// показываем всплывающее окно
			EditorUtility.DisplayDialog(
				"Item ID Generated",
				$"ID: {id}\n\nСкопировано в буфер обмена.",
				"OK"
			);

			Debug.Log($"[ItemID] Generated: {id}");
		}
	}
}