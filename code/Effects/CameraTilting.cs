using Sandbox;
using Sandbox.Utility;

[Category("Screen Effects")]
public sealed class CameraTilting : Component
{
	protected override void OnUpdate()
	{
		float noise = Noise.Perlin( Time.Now * 20 ) * 0.7f;
		float noise2 = Noise.Perlin( Time.Now * 20 + 5000f ) * 0.7f;


		Transform.Rotation = Rotation.From( MathF.Sin( noise ), Transform.Rotation.Yaw(), MathF.Sin( noise2 ) );
	}
}
