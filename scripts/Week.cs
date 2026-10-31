using Godot;
using System;
using System.Collections.Generic;

public partial class Week : VBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	[Export] Color ColorHidden;
	[Export] Color ColorVisible;
	List<Label> DaysOfWeek = new List<Label>();
	int IdxDay = -1;
	public override void _Ready()
	{
		//Add all days to the list
		foreach(Node child in GetChildren())
		{
			if(child is Label Day)
			{
				DaysOfWeek.Add(Day);
			}
		}
		NewManche(-1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void NewManche(int NewIdxDay)
	{
		IdxDay = NewIdxDay;
		for(int i = 0; i < DaysOfWeek.Count; i++)
		{
			if (i == IdxDay)
			{
				DaysOfWeek[i].Modulate = ColorVisible;
			}
			else
			{
				DaysOfWeek[i].Modulate = ColorHidden;
			}
		}
	}
}
