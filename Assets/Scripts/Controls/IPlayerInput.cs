using UnityEngine;

namespace Ships
{
	public interface IPlayerInput
	{
		Vector2 Steering { get; }
		float Throttle { get; }
	}
}
