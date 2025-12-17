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
			return inv.InventoryUniqueItems.Find(i => i.ItemId == itemId);
		}
	}
}
