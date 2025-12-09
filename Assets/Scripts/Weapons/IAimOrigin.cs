using UnityEngine;

namespace Tanks
{
	public interface IAimOrigin
	{
		Vector3 Position { get; }
		Vector3 Forward { get; }
		TeamMask HitMask { get; }
		float AllowedAngle { get; }
		float DetectionRange { get; }
	}

}