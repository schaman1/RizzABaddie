using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class HostScene : Node
{
	// Called when the node enters the scene tree for the first time.
	[Export] Label LabelJoinCode;
	[Export] Label LabelInfo;
	[Export] Button ButtonStartGame;
	[Export] PackedScene LabelPackedScene;
	[Export] VBoxContainer PseudoContainer;
	[Export] Label LabelPseudo;

	Dictionary<long,Label> DictLabelPlayers = new Dictionary<long, Label>();
	
	public override async void _Ready()
	{
		LabelJoinCode.Text = "Join Code : " + Networking.Instance.JoinCode;
		ButtonStartGame.Pressed += OnStartGamePressed;

		LabelPseudo.Text = Networking.Instance.PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].Pseudo; //Already here as is the host
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		AddPlayerIfNew();
	}

	public void OnStartGamePressed()
	{
		if (Multiplayer.GetPeers().Length > 0) //0 car ,e te cpmpte pas toi
		{
			Networking.Instance.StartGame();
		}
		else
		{
			LabelInfo.Text = "Need at least 2 players.";
		}
	}

	public void AddPlayerIfNew()
	{
		foreach(long Id in Networking.Instance.PlayersInfo.PlayerDic.Keys)
		{
			if(!DictLabelPlayers.Keys.Contains(Id))
			{
				Label newLabel = (Label)LabelPackedScene.Instantiate();
				newLabel.Text = Networking.Instance.PlayersInfo.PlayerDic[Id].Pseudo;
				DictLabelPlayers[Id] = newLabel;
				PseudoContainer.AddChild(newLabel);
			}
		}
	}
}
