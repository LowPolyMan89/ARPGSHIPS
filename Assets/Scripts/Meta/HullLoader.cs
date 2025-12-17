using System;
using System.IO;
using UnityEngine;

namespace Ships
{
	using System.IO;
	using UnityEngine;
	using System.Collections.Generic;

	public static class HullLoader
	{
		private static readonly string HullsPath =
			Path.Combine(Application.streamingAssetsPath, "Configs/Hulls");

		public static HullModel Load(string id)
		{
			var file = Path.Combine(HullsPath, id + ".json");
			var json = File.ReadAllText(file);
			return JsonUtility.FromJson<HullModel>(json);
		}

	}
}
