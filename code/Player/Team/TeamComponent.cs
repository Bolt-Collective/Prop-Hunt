/// <summary>
/// A component that has a team on it.
/// </summary>
public interface ITeam : IValid
{
	public Team Team { get; set; }
}

public enum Team
{
	Unassigned = 0,

	Props,
	Seekers
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
		if ( gameObject.Root.Components.Get<Player>() is { IsValid: true } pawn )
		{
			return pawn.Client.Team;
		}

		if ( gameObject.Root.Components.Get<Client>() is { IsValid: true } state )
		{
			return state.Team;
		}

		return Team.Unassigned;
	}

	//
	// For all of this, maybe the gamemode should be controlling it, and not some global methods
	//

	private static bool IsFriendly( Team teamOne, Team teamTwo )
	{
		if ( teamOne == Team.Unassigned || teamTwo == Team.Unassigned ) return false;
		return teamOne == teamTwo;
	}

	public static bool IsFriendly( this GameObject self, GameObject other )
	{
		if ( !self.IsValid() || !other.IsValid() ) return false;
		return IsFriendly( self.GetTeam(), other.GetTeam() );
	}

	public static bool IsFriendly( this Player self, Player other )
	{
		if ( !self.IsValid() || !other.IsValid() ) return false;
		return IsFriendly( self.Client.Team, other.Client.Team );
	}

	public static bool IsFriendly( this Client self, Client other )
	{
		if ( !self.IsValid() || !other.IsValid() ) return false;
		return IsFriendly( self.Team, other.Team );
	}

	public static string GetName( this Team team )
	{
		return team switch
		{
			Team.Props => "Anarchists",
			Team.Seekers => "Security",
			_ => "Unassigned",
		};
	}

	private static Dictionary<Team, Color> teamColors = new()
	{
		{ Team.Props, new Color32( 5, 146, 235 ) },
		{ Team.Seekers, new Color32( 233, 190, 92 ) },
		{ Team.Unassigned, new Color32( 255, 255, 255 ) },
	};

	public static Color GetColor( this Team team )
	{
		return teamColors[team];
	}

	public static string GetIconPath( this Team team )
	{
		return team switch
		{
			Team.Seekers => "/ui/teams/operators_logo.png",
			Team.Props => "/ui/teams/anarchists_logo.png",
			_ => ""
		};
	}

	public static string GetBannerPath( this Team team )
	{
		return team switch
		{
			Team.Seekers => "/ui/teams/operators_logo_banner.png",
			Team.Props => "/ui/teams/anarchists_logo_banner.png",
			_ => ""
		};
	}

	public static Team GetOpponents( this Team team )
	{
		return team switch
		{
			Team.Seekers => Team.Props,
			Team.Props => Team.Seekers,
			_ => Team.Unassigned
		};
	}
}
