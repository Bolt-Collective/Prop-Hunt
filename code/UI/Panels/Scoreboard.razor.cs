using Sandbox.UI;

public partial class Scoreboard : Panel
{
	protected override int BuildHash()
	{
		return HashCode.Combine(Input.Down("Score"));
	}
}
