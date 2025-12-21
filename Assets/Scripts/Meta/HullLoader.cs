using System.IO;
using UnityEngine;

namespace Ships
{
	public static class HullLoader
	{
		public static HullModel Load(string id)
		{
			var relativePath = Path.Combine(PathConstant.HullsConfigs, id + ".json");
			return ResourceLoader.TryLoadStreamingJson(relativePath, out HullModel model) ? model : null;
		}
	}
}
