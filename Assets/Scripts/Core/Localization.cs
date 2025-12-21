using System;
using System.Collections.Generic;
using System.IO;

namespace Ships
{
	public static class Localization
	{
		// LangId: 0 - Russian 1 - English
		public static int LangId;
		private static readonly Dictionary<string, LocalizationElement> LocalizationElements =
			new Dictionary<string, LocalizationElement>(StringComparer.OrdinalIgnoreCase);

		public static string GetLoc(string id)
		{
			if (string.IsNullOrEmpty(id))
				return id;

			if (LocalizationElements.TryGetValue(id.ToLowerInvariant(), out var value) &&
			    value?.Values != null &&
			    value.Values.Count > LangId)
				return value.Values[LangId];

			return id;
		}

		public static void LoadLocalizationDataFromConfig()
		{
			LocalizationElements.Clear();

			foreach (var file in ResourceLoader.GetStreamingFiles(PathConstant.LocalizationConfigs, "*.json"))
			{
				if (!ResourceLoader.TryLoadStreamingJson(Path.Combine(PathConstant.LocalizationConfigs, file),
					    out LocalizationConfig config) ||
				    config?.LocalizationElements == null)
					continue;

				for (var i = 0; i < config.LocalizationElements.Count; i++)
				{
					var element = config.LocalizationElements[i];
					if (element == null || string.IsNullOrEmpty(element.Id))
						continue;

					var key = element.Id.ToLowerInvariant();
					element.Id = key;
					LocalizationElements[key] = element;
				}
			}
		}
	}

	[System.Serializable]
	public class LocalizationConfig
	{
		public List<LocalizationElement> LocalizationElements = new List<LocalizationElement>();
	}

	[System.Serializable]
	public class LocalizationElement
	{
		public string Id;
		public List<string> Values = new List<string>();
	}
}
