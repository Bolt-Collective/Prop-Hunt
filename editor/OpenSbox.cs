using System.Diagnostics;
using Editor;

public static class OpenSbox
{
	[Menu("Editor", "Prop Hunt Editor/Open s&box")]
	public static void OpenMyMenu()
	{
		Process.Start("C:\\Program Files (x86)\\Steam\\steamapps\\common\\sbox\\sbox.exe");
	}
}
