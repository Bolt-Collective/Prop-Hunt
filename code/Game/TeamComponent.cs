﻿
public enum Team
{
	Props,
	Hunters,
	Unassigned
}

public class TeamComponent : Component
{
	/// <summary>
	/// An action (for ActionGraph, or other components) to listen for team changes
	/// </summary>
	[Property, Group( "Actions" )] public Action<Team, Team> OnTeamChanged { get; set; }

	/// <summary>
	/// The team this player is on.
	/// </summary>
	[Property, Group( "Setup" ), HostSync, Change( nameof( OnTeamPropertyChanged ) )]
	public Team Team { get; set; }

	/// <summary>
	/// Called when <see cref="Team"/> changes across the network.
	/// </summary>
	/// <param name="before"></param>
	/// <param name="after"></param>
	private void OnTeamPropertyChanged( Team before, Team after )
	{
		OnTeamChanged?.Invoke( before, after );
	}

	public void GetRandomTeam()
	{
		Team = GetRandom( 0, 1 ) == 0 ? Team.Props : Team.Hunters;
	}
	int GetRandom(int min, int max)
	{
		return Random.Shared.Int(min, max);
	}
}

public static class TeamExtensions
{
	/// <summary>
	/// Accessor to get the team of someone/something.
	/// </summary>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public static Team GetTeam( this GameObject gameObject )
	{
		var comp = gameObject.Components.Get<TeamComponent>( FindMode.EverythingInSelfAndAncestors );
		return !comp.IsValid() ? Team.Unassigned : comp.Team;
	}

	//
	// For all of this, maybe the gamemode should be controlling it, and not some global methods
	//

	/// <summary>
	/// Are we friendly with this other team?
	/// </summary>
	/// <param name="teamOne"></param>
	/// <param name="teamTwo"></param>
	/// <returns></returns>
	private static bool IsFriendly( Team teamOne, Team teamTwo )
	{
		if ( teamOne == Team.Unassigned || teamTwo == Team.Unassigned ) return false;
		return teamOne == teamTwo;
	}

	/// <summary>
	/// Are these two GameObjects friends with eachother?
	/// </summary>
	/// <param name="self"></param>
	/// <param name="other"></param>
	/// <returns></returns>
	public static bool IsFriendly( this GameObject self, GameObject other )
	{
		return IsFriendly( self.GetTeam(), other.GetTeam() );
	}

	/// <summary>
	/// Are these two <see cref="Player"/>s friends with each other?
	/// </summary>
	public static bool IsFriendly( this Player self, Player other )
	{
		return IsFriendly( self.TeamComponent.Team, other.TeamComponent.Team );
	}

	public static string GetName( this Team team )
	{
		return team switch
		{
			Team.Props => "Props",
			Team.Hunters => "Hunters",
			_ => "Unassigned",
		};
	}

	public static Color GetColor( this Team team )
	{
		return team switch
		{
			Team.Hunters => Color.Parse( "#0592EB" ) ?? default,
			Team.Props => Color.Parse( "#e9be5c" ) ?? default,
			_ => Color.Parse( "white" ) ?? default
		};
	}

	public static string GetIconPath( this Team team )
	{
		return team switch
		{
			Team.Hunters => "/ui/teams/hunters.png",
			Team.Props => "/ui/teams/props.png",
			_ => ""
		};
	}

	public static Team GetOpponents( this Team team )
	{
		return team switch
		{
			Team.Hunters => Team.Props,
			Team.Props => Team.Hunters,
			_ => Team.Unassigned
		};
	}
}
