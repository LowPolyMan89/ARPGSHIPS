using System;
using UnityEngine;

namespace Ships
{
	public static class Services
	{
		public static bool IsInLayerMask(GameObject obj, LayerMask mask)
		{
			return (mask.value & (1 << obj.layer)) != 0;
		}
		
		public static class UniqueIdGenerator
		{
			private static readonly char[] Chars =
				"abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

			private static readonly System.Random Rnd = new System.Random();

			private static string RandomString(int length)
			{
				var buffer = new char[length];
				for (int i = 0; i < length; i++)
					buffer[i] = Chars[Rnd.Next(Chars.Length)];
				return new string(buffer);
			}
			
			public static string GenerateItemId()
			{
				long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
				string r = RandomString(4);
				return $"item_{timestamp}_{r}";
			}
		}
	}
	public static class InventoryUtils
	{
		public static InventoryItem FindByItemId(PlayerInventoryModel inv, string itemId)
		{
			if (inv == null || string.IsNullOrEmpty(itemId))
				return null;

			var normalized = NormalizeItemId(itemId);
			return inv.InventoryUniqueItems.Find(i =>
				i != null &&
				(string.Equals(NormalizeItemId(i.TemplateId), normalized, StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(NormalizeItemId(i.ItemId), normalized, StringComparison.OrdinalIgnoreCase)));
		}

		public static string ResolveItemId(InventoryItem item)
		{
			if (item == null)
				return null;

			return !string.IsNullOrEmpty(item.TemplateId) ? item.TemplateId : item.ItemId;
		}

		public static void AddOrIncrease(PlayerInventoryModel inv, string templateId, string rarity, int count)
		{
			if (inv == null || string.IsNullOrEmpty(templateId) || count <= 0)
				return;

			templateId = NormalizeItemId(templateId);
			var existing = FindByItemId(inv, templateId);
			if (existing != null)
			{
				existing.Count += count;
				if (!string.IsNullOrEmpty(rarity))
					existing.Rarity = rarity;
				return;
			}

			inv.InventoryUniqueItems.Add(new InventoryItem
			{
				ItemId = templateId,
				TemplateId = templateId,
				Rarity = rarity,
				Count = count
			});
		}

		public static bool TryConsume(PlayerInventoryModel inv, string templateId, int count)
		{
			if (inv == null || string.IsNullOrEmpty(templateId) || count <= 0)
				return false;

			templateId = NormalizeItemId(templateId);
			var item = FindByItemId(inv, templateId);
			if (item == null || item.AvailableCount < count)
				return false;

			item.EquippedCount += count;
			return true;
		}

		public static void ReturnToInventory(PlayerInventoryModel inv, string templateId, int count)
		{
			if (inv == null || string.IsNullOrEmpty(templateId) || count <= 0)
				return;

			templateId = NormalizeItemId(templateId);
			var item = FindByItemId(inv, templateId);
			if (item == null)
				return;

			item.EquippedCount = Mathf.Max(0, item.EquippedCount - count);
		}

		public static void RebuildEquippedCounts(MetaState state)
		{
			if (state?.InventoryModel?.InventoryUniqueItems == null)
				return;

			foreach (var item in state.InventoryModel.InventoryUniqueItems)
			{
				if (item != null)
					item.EquippedCount = 0;
			}

			if (state.Fit?.GridPlacements == null)
				return;

			foreach (var placement in state.Fit.GridPlacements)
			{
				if (placement == null || string.IsNullOrEmpty(placement.ItemId))
					continue;

				var item = FindByItemId(state.InventoryModel, placement.ItemId);
				if (item != null)
					item.EquippedCount += 1;
			}
		}

		private static string NormalizeItemId(string itemId)
		{
			if (string.IsNullOrEmpty(itemId))
				return itemId;

			return itemId.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? itemId.Substring(0, itemId.Length - ".json".Length)
				: itemId;
		}
	}
}
