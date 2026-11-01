using Godot;
using System;
using System.ComponentModel;

public partial class ColorRectEndGame : ColorRect
{
	// Called when the node enters the scene tree for the first time.
	[Export] Label LabelWinLose;
	[Export]Button ButtonQuit;
	[Export]Button ButtonRestart;
	[Export] PackedScene HostScene;
	[Export] PackedScene ClientScene;
	[Export] PackedScene MainScene;
	public override void _Ready()
	{
		Visible = false;
		ButtonQuit.Pressed+=QuitGame;
		ButtonRestart.Pressed+=RestartGame;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void SetEndGame(bool HasWin)
	{
		if (HasWin)
		{
			LabelWinLose.Text = "Win";
		}
		else
		{
			LabelWinLose.Text = "Lose";
		}
		Visible = true;
	}

	public void QuitGame()
	{//Needs to deco from the network
		GetTree().ChangeSceneToPacked(MainScene);
	}
	public void RestartGame()
	{
		if (Networking.Instance.IsHost)
		{
			GetTree().ChangeSceneToPacked(HostScene);
		}
		else
		{
			GetTree().ChangeSceneToPacked(ClientScene);
		}
	}
}
