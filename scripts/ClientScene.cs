using Godot;
using System;

public partial class ClientScene : Node2D
{
	// Called when the node enters the scene tree for the first time.
	[Export] Label LabelPseudo;
	bool set = false;
	public override void _Ready()
	{
		LabelPseudo.Text = "Incoming";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		SetPseudo();
	}
	public void SetPseudo()
	{
		if (!set)
		{
			if (Networking.Instance.PlayersInfo.PlayerDic.Keys.Contains(Multiplayer.GetUniqueId()))
			{
				set = true;
				LabelPseudo.Text = Networking.Instance.PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].Pseudo;
			}
		}
	}
	
}
