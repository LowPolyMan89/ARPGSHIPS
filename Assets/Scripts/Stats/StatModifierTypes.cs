namespace Ships
{
	/// <summary>
	/// Тип изменения величины.
	/// </summary>
	public enum StatModifierType
	{
		/// <summary>
		/// Плоская прибавка: value = value + X
		/// </summary>
		Flat,

		/// <summary>
		/// Суммарный процент: value = value * (1 + sum)
		/// </summary>
		PercentAdd,

		/// <summary>
		/// Мультипликативный процент: value = value * product(1 + X)
		/// </summary>
		PercentMult,

		/// <summary>
		/// Жёсткая установка: value = X
		/// </summary>
		Set
	}

	/// <summary>
	/// К какой части стата применяется модификатор.
	/// </summary>
	public enum StatModifierTarget
	{
		Current,
		Maximum
	}

	/// <summary>
	/// Периодичность действия модификатора.
	/// </summary>
	public enum StatModifierPeriodicity
	{
		/// <summary>
		/// Действует постоянно, пока явно не удалён.
		/// </summary>
		Permanent,

		/// <summary>
		/// Ограничен количеством тиков.
		/// </summary>
		Timed
	}
}