using Godot;
using System;

public partial class CanvasLayerLoading : CanvasLayer
{
	// Called when the node enters the scene tree for the first time.
	[Export]Label LabelLoading;
	[Export]Timer LoadingTimer;
	int NbrPoint = 0;
	int NbrPointMax = 3;
	String InitText = "Loading";
	public override void _Ready()
	{
		LoadingTimer.Timeout+=AddPoint;
		LoadingTimer.Start();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void AddPoint()
	{
		NbrPoint++;
		if(NbrPoint>NbrPointMax){NbrPoint = 0;}
		String text = InitText;
		for(int i = 0; i < NbrPoint; i++)
		{
			text += ".";
		}
		LabelLoading.Text = text;
	}
}
