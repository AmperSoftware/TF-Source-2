using Sandbox;
using Editor;
using Amper.FPS;

namespace TFS2;

/// <summary>
/// Dummy placeholder entity until Facepunch rewrites prop_dynamic in C#.
/// C# and C++ can't interact with each other right now in terms of Hammer I/O system.
/// </summary>
[Library( "tf_prop_dynamic" )]
[Title("Dynamic Prop")]
[Category("Gameplay")]
[Icon("pest_control")]
[Model]
[RenderFields]
[HammerEntity]
partial class PropDynamic : AnimatedEntity
{
	[Property] public bool Solid { get; set; } = true;

	public override void Spawn()
	{
		base.Spawn();

		PhysicsEnabled = false;
		UsePhysicsCollision = true;

		Tags.Add( Solid ? CollisionTags.Solid : CollisionTags.NotSolid );
	}

	[Input( "SetMaterialGroupName" )]
	private void Input_SetMaterialGroupName( string group )
	{
		SetMaterialGroup( group );
	}

	[Input( "SetMaterialGroup" )]
	private void Input_SetMaterialGroup( int group )
	{
		SetMaterialGroup( group );
	}

	[Input( "SetBodyGroup" )]
	private void Input_SetBodyGroup( int group )
	{
		SetBodyGroup( 0, group );
	}

	/// <summary>
	/// Plays a specified animation sequence.
	/// </summary>
	[Input("SetAnimation")]
	private void Input_SetAnimation(string sequence)
	{
		CurrentSequence.Name = sequence;
		CurrentSequence.Time = 0f;
	}

	[Input( "Disable" )]
	private void Input_Disable()
	{
		EnableDrawing = false;
	}

	[Input( "Enable" )]
	private void Input_Enable()
	{
		EnableDrawing = true;
	}
}
