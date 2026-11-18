using Godot;
using System;
using System.Runtime.Serialization;

public partial class Baddie : RigidBody2D
{
	// Called when the node enters the scene tree for the first time.
	[Signal] public delegate void OnFoundByPlayerEventHandler(long PeerId);
	[Export]CollisionShape2D CollisionShapeDetection;
	[Export]Area2D AreaDetection;

	[Export]Timer timerBeforeCanCollide;

	public override void _Ready()
	{
		if (Multiplayer.IsServer())
		{
			AreaDetection.Connect("body_entered",Callable.From<Node>(OnPlayerEnter));
		}
		StartCollision();
		timerBeforeCanCollide.Timeout += SetCanCollide;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	private void OnPlayerEnter(Node node)
	{
		//Only server can send this
		if(node is PlayerCharacter player)
		{
			if (player.IsLookingForBaddie())
			{
				EmitSignal(SignalName.OnFoundByPlayer,player.Id);
			}
		}
	}

	public void StartCollision()
	{
		timerBeforeCanCollide.Start();
	}
	public void SetCanCollide()
	{
		CollisionShapeDetection.Disabled = false;
	}
	public void StopCollision()
	{
		CallDeferred(nameof(CallDefferedStopCollision));
	}
	public void CallDefferedStopCollision()
	{
		CollisionShapeDetection.Disabled = true;
	}
}
