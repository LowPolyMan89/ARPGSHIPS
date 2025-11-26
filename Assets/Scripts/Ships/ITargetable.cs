using UnityEngine;

namespace Ships
{
	public interface ITargetable : IStatsProvider
	{
		Transform Transform { get; }
		bool IsAlive { get; }
		TargetSize Size { get; }
		Vector2 Velocity { get; }
	}

}