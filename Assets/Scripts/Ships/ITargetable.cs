using UnityEngine;

namespace Ships
{
	public interface ITargetable
	{
		Transform Transform { get; }
		bool IsAlive { get; }
		TargetSize Size { get; }     // Small/Medium/Large
		Vector2 Velocity { get; }    // нужно для lead
	}

}