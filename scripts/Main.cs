using Godot;
using System;

public partial class Main : Node2D
{
	// Called when the node enters the scene tree for the first time.
	
	[Export] PackedScene HostScene;
	[Export] PackedScene ClientScene;
	[Export] LineEdit LineEditPseudo;
	[Export] LineEdit LineEditCode;
	[Export] Button ButtonHost;
	[Export] Button ButtonJoin;
	[Export] Label InfoLabel;

	public override void _Ready()
	{
		Networking.Instance.HostCreated += OnHostCreate;
		Networking.Instance.CodeNotFound += OnCodeNotFound;
		Networking.Instance.ConnectionSuccess += OnConnectionSuccess;
		ButtonHost.Pressed += OnHostButtonPressed;
		ButtonJoin.Pressed += OnJoinButtonPressed;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void OnHostButtonPressed()
	{
		Networking.Instance.Pseudo = LineEditPseudo.Text;
		Networking.Instance.HostLobby();
	}
	private void OnJoinButtonPressed()
	{
		Networking.Instance.Pseudo = LineEditPseudo.Text;
		String code = LineEditCode.Text.StripEdges(); //Remove spaces
		Networking.Instance.JoinByCode(code);
	}

	private void OnHostCreate()
	{
		GetTree().ChangeSceneToPacked(HostScene);
	}
	private void OnCodeNotFound()
	{
		InfoLabel.Text = "Session not found.";
	}
	private async void OnConnectionSuccess()
	{
		await Networking.Instance.RequestPlayerAlreadyConnected(); //Get values from networking of previous player + True bcs can create new player
		GetTree().ChangeSceneToPacked(ClientScene);
	}
}
