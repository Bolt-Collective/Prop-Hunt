using Sandbox;
using Sandbox.Network;
namespace Kicks;
[Icon( "handshake" )]
public sealed class Interactor : Component
{
	PhysicsBody grabbedBody;
	Transform grabbedOffset;
	[Property] GameObject PhysicsGameObject;
	public bool IsGrabbing => grabbedBody is not null;
	public Player PlayerController;

	protected override void OnStart()
	{
		PlayerController = Scene.GetAllComponents<Player>().FirstOrDefault( x => !x.IsProxy );
	}
	protected override void OnUpdate()
	{
		if ( IsProxy || PlayerController is null || PlayerController.FreeLooking ) return;
		Transform aimTransform = Scene.Camera.Transform.World;
		var ray = Scene.Camera.ScreenNormalToRay( 0.5f );
		var tr = Scene.Trace.Ray( ray, 500 ).WithoutTags( "player" ).Run();
		if ( Input.Pressed( "flashlight" ) && tr.Body is not null )
		{
			if ( tr.Hit && tr.Body is not null )
			{
				grabbedBody = tr.Body;
				grabbedOffset = aimTransform.ToLocal( tr.Body.Transform );
				PhysicsGameObject = tr.GameObject;
				PhysicsGameObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
				PhysicsGameObject.Network.TakeOwnership();
			}
			else
			{
				return;
			}
		}
		if ( PhysicsGameObject is null ) return;
		if ( grabbedBody is null ) return;
		if ( Input.Down( "flashlight" ) && grabbedBody is not null && PhysicsGameObject is not null )
		{
			if ( grabbedBody is null ) return;
			if ( GameNetworkSystem.IsActive )
			{
				PhysicsGameObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
				PhysicsGameObject.Network.TakeOwnership();
			}
			var targetTx = aimTransform.ToWorld( grabbedOffset );
			grabbedBody.SmoothMove( targetTx, Time.Delta * 10, Time.Delta );
			return;
		}
		else
		{
			if ( grabbedBody is null ) return;
			if ( GameNetworkSystem.IsActive )
			{
				foreach ( var gb in PhysicsGameObject.GetAllObjects( false ) )
				{
					if ( gb is not null )
					{
						gb.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
						gb.Network.DropOwnership();
					}
				}
			}
			grabbedBody = null;
			PhysicsGameObject = null;
		}
	}
}
