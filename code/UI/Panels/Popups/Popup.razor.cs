using Sandbox.UI;

public partial class Popup : Panel
{
	public string Title;
	public string Text;
	public Color TitleColor;
	public float Duration = 8f;

	public TimeSince TimeSincePanelCreated = 0f;

	public override void Tick()
	{
		base.Tick();
		if ( TimeSincePanelCreated > Duration )
		{
			Delete(false);
		}
	}

	protected override int BuildHash()
	{
		return HashCode.Combine( Title, Text );
	}
}
