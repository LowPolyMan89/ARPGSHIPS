using UnityEngine;

namespace Ships
{
	public interface ITargetable : IStatsProvider
	{
		Transform Transform { get; }
		bool IsAlive { get; }
		TargetSize Size { get; }
		Vector3 Velocity { get; }
		TeamMask Team { get; }
		void TakeDamage(CalculatedDamage calculatedDamage);
	}

}
