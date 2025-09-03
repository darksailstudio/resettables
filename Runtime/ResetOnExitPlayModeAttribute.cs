using System;

namespace DarkSail.Resettables
{
	/// <summary>
	/// Attribute for marking <see cref="UnityEngine.ScriptableObject"/> derived classes
	/// to be reset by discarding play mode changes.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class ResetOnExitPlayModeAttribute : Attribute
	{
		public bool Inherited { get; set; } = true;
	}
}
