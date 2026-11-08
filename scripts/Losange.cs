using Godot;
using System;

public partial class Losange : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export]Label label;
	[Export]Timer TimerShow;

	public int LenShow = 1;
	public override void _Ready()
	{
		TimerShow.WaitTime = LenShow;
		TimerShow.Timeout += Hide;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ShowYesOrNo(int res)
	{
		if (res == 1)
		{
			label.Text = "Yes";
		}
		else
		{
			label.Text = "No";
		}
		Show();
		TimerShow.Start();
	}

}
