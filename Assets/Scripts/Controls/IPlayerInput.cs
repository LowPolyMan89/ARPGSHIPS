using UnityEngine;

namespace Tanks
{
	public interface IPlayerInput
	{
		Vector2 Steering { get; }
		float Throttle { get; }
	}
}