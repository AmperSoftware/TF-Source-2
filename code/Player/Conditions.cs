using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TFS2;

public enum TFCondition
{
	Invulnerable,
	InvulnerableWearoff,
	CritBoosted,
	Burning,

	// NOT IMPLEMENTED YET
	Cloaked,
	CloakedBlink,
	Taunting
}

partial class TFPlayer
{
	public const int PermanentCondition = -1;

	[Net, Change] public IList<TFCondition> ActiveConditions { get; set; }
	[Net] public IDictionary<TFCondition, Entity> ConditionProviders { get; set; }
	[Net] public IDictionary<TFCondition, float> ConditionExpireTime { get; set; }


	/// <summary>
	/// Called on both client and server when we add the condition bit.
	/// </summary>
	public void OnConditionTick( TFCondition cond )
	{
		if ( ConditionTickSubscriptions.TryGetValue( cond, out var callback ) )
			callback?.Invoke();
	}

	/// <summary>
	/// Called on both client and server when we add the condition bit.
	/// </summary>
	public void OnConditionChanged( TFCondition cond )
	{
		if ( ConditionChangedSubscriptions.TryGetValue( cond, out var callback ) )
			callback?.Invoke();
	}

	/// <summary>
	/// Called on both client and server when we add the condition bit.
	/// </summary>
	public void OnConditionAdded( TFCondition cond )
	{
		if ( ConditionAddedSubscriptions.TryGetValue( cond, out var callback ) )
			callback?.Invoke();
	}

	/// <summary>
	/// Called on both client and server when we remove the condition bit.
	/// </summary>
	public void OnConditionRemoved( TFCondition cond )
	{
		if ( ConditionRemovedSubscriptions.TryGetValue( cond, out var callback ) )
			callback.Invoke();
	}

	/// <summary>
	/// Runs server-only condition think.
	/// If a player needs something updated related to conditions, do it here.
	/// </summary>
	public virtual void ConditionServerTick( TFCondition cond )
	{
		if ( ConditionChangedSubscriptions.TryGetValue( cond, out var callback ) )
			callback?.Invoke();
	}

	/// <summary>
	/// Adds the condition bit.
	/// </summary>
	public void AddCondition( TFCondition cond, float duration = PermanentCondition, Entity provider = null )
	{
		if ( !Game.IsServer )
			return;

		// If we're dead, don't take on any new conditions
		if ( !IsAlive )
			return;

		if ( duration != PermanentCondition )
		{
			// If our current condition is permanent or we're trying to set a new time that's
			// less our current time remaining, use our current time instead

			var remainingTime = GetConditionRemainingTime( cond );
			if ( remainingTime == PermanentCondition || duration < remainingTime )
				duration = remainingTime;
		}

		// Add condition to the list of active conditions.
		if ( !ActiveConditions.Contains( cond ) )
			ActiveConditions.Add( cond );

		ConditionProviders[cond] = provider;
		ConditionExpireTime[cond] = duration;

		OnConditionAdded( cond );
		OnConditionChanged( cond );
	}


	/// <summary>
	/// Removes the condition bit.
	/// </summary>
	public void RemoveCondition( TFCondition cond )
	{
		if ( !Game.IsServer )
			return;

		if ( !InCondition( cond ) )
			return;

		// Remove the condition bit
		ActiveConditions.Remove( cond );

		OnConditionRemoved( cond );
		OnConditionChanged( cond );

		ConditionExpireTime.Remove( cond );
		ConditionProviders.Remove( cond );
	}

	/// <summary>
	/// Returns the amount of time that is left until this condition expires.<br/>
	/// - Value &gt; 0 is the remaining time,<br/>
	/// - Value = 0 means condition is expired,<br/>
	/// - Value &lt; 0 means condition is Permanent
	/// </summary>
	public float GetConditionRemainingTime( TFCondition cond )
	{
		if ( !InCondition( cond ) )
			return 0;

		if ( ConditionExpireTime.TryGetValue( cond, out var expireTime ) )
			return expireTime;

		return 0;
	}

	/// <summary>
	/// Retrieves the provider entity for a condition.
	/// </summary>
	public Entity GetConditionProvider( TFCondition cond )
	{
		if ( !InCondition( cond ) )
			return null;

		if ( ConditionProviders.TryGetValue( cond, out var provider ) )
			return provider;

		return null;
	}

	/// <summary>
	/// Removes all conditions.
	/// </summary>
	public void RemoveAllConditions()
	{
		for ( var i = ActiveConditions.Count - 1; i >= 0; i-- )
		{
			var cond = ActiveConditions[i];
			RemoveCondition( cond );
		}
	}

	/// <summary>
	/// Returns true if the player is currently in this condition, false otherwise.
	/// </summary>
	public bool InCondition( TFCondition cond )
	{
		return ActiveConditions.Contains( cond );
	}

	private void TickConditions()
	{
		for ( var i = ActiveConditions.Count - 1; i >= 0; i-- )
		{
			var cond = ActiveConditions[i];
			OnConditionTick( cond );

			if ( Game.IsServer )
			{
				// Condition doesn't contain expiration time, 
				// remove it right away.
				if ( !ConditionExpireTime.ContainsKey( cond ) )
				{
					RemoveCondition( cond );
					continue;
				}

				if ( ConditionExpireTime[cond] == PermanentCondition )
					continue;

				ConditionExpireTime[cond] -= Time.Delta;

				// Condition has expired.
				if ( ConditionExpireTime[cond] <= 0 )
					RemoveCondition( cond );
			}
		}
	}

	public void OnActiveConditionsChanged( IList<TFCondition> oldList, IList<TFCondition> newList )
	{
		foreach ( var cond in newList.Except( oldList ) )
		{
			OnConditionAdded( cond );
			OnConditionChanged( cond );
		}

		foreach ( var cond in oldList.Except( newList ) )
		{
			OnConditionRemoved( cond );
			OnConditionChanged( cond );
		}
	}

	[ConCmd.Admin( "addcond" )]
	private static void Command_AddCond( TFCondition cond, float duration = PermanentCondition )
	{
		(ConsoleSystem.Caller.Pawn as TFPlayer)?.AddCondition( cond, duration );
	}

	[ConCmd.Admin( "removecond" )]
	private static void Command_RemoveCond( TFCondition cond )
	{
		(ConsoleSystem.Caller.Pawn as TFPlayer)?.RemoveCondition( cond );
	}
}
