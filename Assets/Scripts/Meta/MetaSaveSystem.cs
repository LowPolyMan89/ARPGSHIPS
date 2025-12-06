using System.IO;
using UnityEngine;

namespace Ships
{
	public static class MetaSaveSystem
	{
		private static readonly string _path = 
			Path.Combine(Application.persistentDataPath, "meta.json");

		public static void Save(MetaState state)
		{
			var json = JsonUtility.ToJson(state, true);
			File.WriteAllText(_path, json);
		}

		public static MetaState Load()
		{
			if (!File.Exists(_path))
				return new MetaState();

			var json = File.ReadAllText(_path);
			return JsonUtility.FromJson<MetaState>(json);
		}
	}
}