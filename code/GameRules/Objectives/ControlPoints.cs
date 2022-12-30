using Sandbox;
using System.Linq;
using System.Collections.Generic;
using System;
using Amper.FPS;

namespace TFS2;

partial class TFGameRules
{
	[Net] public bool MapHasControlPoints { get; set; }

	/// <summary>
	/// Are points allowed to be captured right now?
	/// </summary>
	public bool PointsMayBeCaptured() => AreObjectivesActive();

	/// <summary>
	/// Can this team capture this point right now?
	/// </summary>
	public bool TeamMayCapturePoint( TFTeam team, ControlPoint point )
	{
		var prevPoint = point.GetPreviousPointForTeam( team );

		// we can't set ourselves as previous point, assume null.
		if ( prevPoint == point ) 
			prevPoint = null;

		if ( prevPoint != null && prevPoint.OwnerTeam != team ) 
			return false;

		// can't cap if it's locked
		if ( point.Locked )
			return false;

		return true;
	}

	/// <summary>
	/// Can this player capture the point right now?
	/// </summary>
	public bool PlayerMayCapturePoint( TFPlayer player, ControlPoint point )
	{
		// TODO: if invisible

		// if invulnerable
		if ( player.InCondition( TFCondition.Invulnerable ) ) 
			return false;

		// TODO: if disguised as enemy team

		return true;
	}

	/// <summary>
	/// May this player block this point right now?
	/// </summary>
	public bool PlayerMayBlockPoint( TFPlayer team, ControlPoint point ) => AreObjectivesActive();

	public virtual int GetCaptureValueForPlayer( TFPlayer player )
	{
		var val = 1;
		if ( player.PlayerClass != null )
		{
			val = player.PlayerClass.Abilities.CaptureValue;
		}

		return val;
	}

	public void ControlPointCaptured( ControlPoint point, TFTeam oldteam, TFTeam newteam, IClient[] cappers )
	{
		PlaySoundToAll( "ui.scored", SoundBroadcastChannel.Generic );

		// print to console about this.
		Log.Info( $"\"{point.PrintName}\" was captured by {cappers.Count()} player(s) from {newteam} team." );

		EventDispatcher.InvokeEvent( new ControlPointCapturedEvent
		{
			Point = point,
			NewTeam = newteam,
			Cappers = cappers
		} );

		foreach ( var ply in cappers.Select( c => c.Pawn as TFPlayer ) )
			ply.Captures += 2;
	}

	/// <summary>
	/// Will the round end if a team captures all the control points?
	/// </summary>
	/// <returns></returns>
	public bool TeamOwnsAllControlPointsCausesRoundEnd()
	{
		// yea, unless we play koth.
		return !IsPlayingKingOfTheHill;
	}

	public bool TeamOwnsAllControlPoints( TFTeam team )
	{
		var points = ControlPoint.All;
		
		// team can't own all points if there is none.
		if ( points.Count == 0 ) 
			return false;

		// points that by default dont belong to us.
		var enemyPoints = points.Where( x => x.GetDefaultTeamOwner() != team );

		// if we don't have any enemy points we own all points.
		if ( enemyPoints.Count() == 0 )
			return false;

		// team owns all the points if no other team owns all the points.
		return !enemyPoints.Where( x => x.OwnerTeam != team ).Any();
	}

	/// <summary>
	/// Returns the first point that belongs to this team by default.
	/// </summary>
	/// <param name="team"></param>
	/// <returns></returns>
	public ControlPoint GetFirstDefaultOwnedControlPoint( TFTeam team )
	{
		if ( !team.IsPlayable() )
			return null;

		var teamPoints = ControlPoint.All.Where( x => x.GetDefaultTeamOwner() == team );

		// this team doesn't own any points, return null.
		if ( teamPoints.Count() == 0 )
			return null;

		// find the points with no previous points set.
		switch ( team )
		{
			case TFTeam.Red:
				teamPoints = teamPoints.Where( x => x.PreviousRedPoint == null ); break;

			case TFTeam.Blue:
				teamPoints = teamPoints.Where( x => x.PreviousBluePoint == null ); break;
		}

		// If we have multiple "first" points, that means the cp layout is asymetrical. Return null.
		if ( teamPoints.Count() > 1 )
			return null;

		// now we should have just a single control point.
		// return the first one.
		return teamPoints.FirstOrDefault();
	}

	/// <summary>
	/// Returns the first owned point by the team that doesn't have any previous points set.
	/// </summary>
	/// <param name="team"></param>
	/// <returns></returns>
	public ControlPoint GetFirstOwnedControlPoint( TFTeam team )
	{
		if ( !team.IsPlayable() )
			return null;

		var teamPoints = ControlPoint.All.Where( x => x.OwnerTeam == team );

		// this team doesn't own any points, return null.
		if ( teamPoints.Count() == 0 )
			return null;

		// find the points with no previous points set.
		switch ( team )
		{
			case TFTeam.Red:
				teamPoints = teamPoints.Where( x => x.PreviousRedPoint == null ); break;

			case TFTeam.Blue:
				teamPoints = teamPoints.Where( x => x.PreviousBluePoint == null ); break;
		}

		// If we have multiple "first" points, that means the cp layout is asymetrical. Return null.
		if ( teamPoints.Count() > 1 )
			return null;

		// now we should have just a single control point.
		// return the first one.
		return teamPoints.FirstOrDefault();
	}

	public ControlPoint GetFarthestOwnedControlPoint( TFTeam team )
	{
		// get the first point that we own.
		var point = GetFirstOwnedControlPoint( team );

		// If team doesnt own a single point, that means that they cant possibly have a "farthest" control point.
		if ( point == null )
			return null;

		// we can't possibly have more iterations in the loop than we have points on the map.
		// use this as loop limit. in reality we should almost never reach this index.
		var count = ControlPoint.All.Count;

		for ( int i = 0; i < count; i++ )
		{
			var nextPoint = point.GetNextControlPointForTeam( team );

			// next point doesn't exist or belongs to some other team.
			if ( nextPoint == null || nextPoint.OwnerTeam != team )
				break;

			point = nextPoint;
		}

		return point;
	}

	/// <summary>
	/// Returns the farthest owned control point with associated respawn rooms.
	/// </summary>
	/// <param name="team"></param>
	/// <returns></returns>
	public ControlPoint GetFarthestOwnedControlPointWithRespawnRoom( TFTeam team )
	{
		// get the first point that we own.
		var finalPoint = GetFirstDefaultOwnedControlPoint( team );

		// If team doesnt own a single point, that means that they cant possibly have a "farthest" control point.
		if ( finalPoint == null )
			return null;

		// we can't possibly have more iterations in the loop than we have points on the map.
		// use this as loop limit. in reality we should almost never reach this index.
		var count = ControlPoint.All.Count;

		var teamRespawns = All.OfType<RespawnRoom>().Where( x => x.TeamOption.Is( team ) );
		var point = finalPoint;

		for ( int i = 0; i < count; i++ )
		{
			point = point.GetNextControlPointForTeam( team );

			// next point doesn't exist or belo	ngs to some other team.
			if ( point == null || point.OwnerTeam != team )
				break;

			if ( !teamRespawns.Any( x => x.ControlPoint == point ) )
				continue;

			finalPoint = point;
		}

		return finalPoint;
	}

	public IEnumerable<ControlPoint> GetControlPointRouteForTeam( TFTeam team )
	{
		var point = GetFirstDefaultOwnedControlPoint( team );
		if ( point == null )
			yield break;

		// we can't possibly have more iterations in the loop than we have points on the map.
		// use this as loop limit. in reality we should almost never reach this index.
		var count = ControlPoint.All.Count;

		for ( int i = 0; i < count; i++ )
		{
			yield return point;

			var nextPoint = point.GetNextControlPointForTeam( team );

			// next point doesn't exist
			if ( nextPoint == null )
				break;

			point = nextPoint;
		}
	}

	[ConCmd.Server( "sv_spewcontrolpoints" )]
	public static void Command_SpewControlPoints()
	{
		foreach ( TFTeam team in Enum.GetValues( typeof( TFTeam ) ) )
		{
			if ( !team.IsPlayable() )
				continue;

			Log.Info( $"{team}" );

			var first = Current.GetFirstOwnedControlPoint( team );
			var farthest = Current.GetFarthestOwnedControlPoint( team );
			Log.Info( $" - First Owned: {first}" );
			Log.Info( $" - Farthest Owned: {farthest}" );
		}
	}
}
