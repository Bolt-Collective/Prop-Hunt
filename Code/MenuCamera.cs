using Sandbox;
using Sandbox.Utility;

public sealed class MenuCamera : Component
{
	[Property]
	private Vector3 TargetPosition { get; set; }
	
	protected override void OnUpdate()
	{
		float noise = Noise.Perlin( Time.Now * 20 ) * 0.7f;
		float noise2 = Noise.Perlin( Time.Now * 20 + 5000f ) * 0.7f;


		// experimental
		// Transform.Rotation = Rotation.LookAt( LookAt.Transform.Position - Transform.Position );
		WorldRotation = Rotation.From( MathF.Sin( noise ), WorldRotation.Yaw(), MathF.Sin( noise2 ) );
	}
}
