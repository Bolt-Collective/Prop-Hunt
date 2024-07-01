using Sandbox.UI;

public partial class Scoreboard : Panel
{
	protected override int BuildHash()
	{
		return HashCode.Combine( Input.Down( "Score" ), Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy ).TeamComponent.TeamName );
	}
}
