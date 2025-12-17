namespace Ships
{
	public interface IOnHitEffect
	{
		void Apply(ITargetable target, float damage, WeaponBase sourceWeapon);
	}
}
