using Godot;
using System;
using System.Collections.Generic;

public partial class PlayArea : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export]public Marker2D CameraPosition;
	[Export]Marker2D BaddiePosition;
	[Export]Baddie baddie;
	[Export]Camera2D camera;
	List<Vector2> Vector2PositionsPlayers = new List<Vector2>();
	public override void _Ready()
	{
		camera.Enabled = false;
		baddie.Connect(nameof(baddie.OnFoundByPlayer),Callable.From<long>(OnPlayerFoundBaddie));
		foreach(Node node in GetChildren())
		{
			if(node is Marker2D marker)
			{
				if (marker.Name.ToString().IsValidInt())
				{
					Vector2PositionsPlayers.Add(marker.GlobalPosition); //Only adds marker which avec a int as a name
				}
			}
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public Vector2 ReturnPosition(int IdxNewPosition)
	{
		return Vector2PositionsPlayers[IdxNewPosition];
	}

	public void OnPlayerFoundBaddie(long PeerId)
	{
		Networking.Instance.PlayerFoundBaddie(PeerId);
	}
	public void StartCardGame()
	{
		camera.Enabled = true;
		camera.MakeCurrent();
		baddie.StopCollision();
	}

	public void StopCardGame()
	{
		camera.Enabled = false;
		baddie.StartCollision();
	}
}
