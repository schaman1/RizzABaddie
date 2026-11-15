using Godot;
using System;
using System.Runtime.Serialization;

public partial class Baddie : RigidBody2D
{
	// Called when the node enters the scene tree for the first time.
	[Signal] public delegate void OnFoundByPlayerEventHandler(long PeerId);
	[Export]CollisionShape2D DetectionArea;
	[Export]Timer time;
	public override void _Ready()
	{
		time.Timeout+=SendToServTest;
		if (Multiplayer.IsServer())
		{
			//DetectionArea.Connect(DetectionArea.)
			time.Start();
		}
		StartManche();
		
		
		//ToRemove
	}

	private void SendToServTest()
	{
		EmitSignal(SignalName.OnFoundByPlayer,1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void StartManche()
	{
		DetectionArea.Disabled = false;
	}
	public void StartCardGame()
	{
		DetectionArea.Disabled = true;
	}

	private void OnPlayerEnter(Node node)
	{
		//Only server can send this
		if(node is PlayerCharacter player)
		{
			EmitSignal(SignalName.OnFoundByPlayer,player.Id);
		}
	}
}
