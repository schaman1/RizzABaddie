using Godot;
using System;
using System.Collections.Generic;

public partial class SpawnArea : Control
{
	// Called when the node enters the scene tree for the first time.
	public List<Vector2> SpawnPositions = new List<Vector2>();
	public override void _Ready()
	{
		foreach(Node node in GetChildren())
		{
			if(node is Marker2D marker)
			{
				if (marker.Name.ToString().IsValidInt())
				{
					SpawnPositions.Add(marker.GlobalPosition);
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
