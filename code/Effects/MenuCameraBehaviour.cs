using Sandbox;
using Sandbox.Utility;

[Category("Screen Effects")]
public sealed class MenuCameraBehaviour : Component
{
	[Property]
	GameObject LookAt { get; set; }

	protected override void OnUpdate()
	{
		float noise = Noise.Perlin( Time.Now * 20 ) * 0.7f;
		float noise2 = Noise.Perlin( Time.Now * 20 + 5000f ) * 0.7f;


		// experimental
		// Transform.Rotation = Rotation.LookAt( LookAt.Transform.Position - Transform.Position );
		Transform.Rotation = Rotation.From( MathF.Sin( noise ), Transform.Rotation.Yaw(), MathF.Sin( noise2 ) );
	}
}
