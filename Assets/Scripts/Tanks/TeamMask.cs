using System;

namespace Tanks
{
	[Flags]
	public enum TeamMask
	{
		None   = 0,
		Player = 1 << 0,
		Enemy  = 1 << 1,
		Ally   = 1 << 2,
		Neutral = 1 << 3,

		All = ~0
	}
}