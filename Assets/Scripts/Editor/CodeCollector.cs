using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class CodeCollector
{
	private const string SourceFolder = "Assets/Scripts";
	private const string OutputFile = "AllSource.txt";

	[MenuItem("Tools/Code Collector/Collect All Scripts")]
	public static void Collect()
	{
		// Находим все .cs файлы
		string[] files = Directory.GetFiles(SourceFolder, "*.cs", SearchOption.AllDirectories);

		if (files.Length == 0)
		{
			Debug.LogWarning("CodeCollector: Не найдено ни одного .cs файла.");
			return;
		}

		// Собираем содержимое
		using (StreamWriter writer = new StreamWriter(OutputFile, false)) // false = перезаписывать каждый раз
		{
			foreach (string file in files)
			{
				writer.WriteLine($"// ---------- {Path.GetFileName(file)} ----------");
				string content = File.ReadAllText(file);
				writer.WriteLine(content);
				writer.WriteLine("\n");
			}
		}

		Debug.Log($"CodeCollector: Готово! Собранно {files.Length} скриптов → {OutputFile}");
		AssetDatabase.Refresh();
	}
}