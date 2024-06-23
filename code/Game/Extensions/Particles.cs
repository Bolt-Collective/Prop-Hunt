using System;
using System.Threading.Tasks;

public static class Particles
{
	public static LegacyParticleSystem Create( string specParticle, Vector3 pos, Rotation rot, float decay = 5f )
	{
		var gameObject = Game.ActiveScene.CreateObject();
		gameObject.Transform.Position = pos;
		gameObject.Transform.Rotation = rot;

		var particle = gameObject.Components.Create<LegacyParticleSystem>();
		particle.Particles = ParticleSystem.Load( specParticle );
		gameObject.Transform.ClearInterpolation();

		Log.Info( "creating particle" );


		// Clear off in a suitable amount of time.
		gameObject.DestroyAsync( decay );

		return particle;
	}

}
