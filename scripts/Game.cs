using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

/*To know 
// s'exécute chez toi uniquement
network.Call("set_Love", id, 15);

// envoie la demande à l'hôte (peer 1), qui l'exécute avec SES valeurs
network.RpcId(1, "request_set_Love", id, 15);

// envoie à tout le monde
network.Rpc("nom_du_rpc", args);
*/

public partial class Game : Node2D
{
	//[Signal] public delegate void MancheFinishAllTriggerEventHandler();
	// Called when the node enters the scene tree for the first time.
	[Export] TextureButton ButtonQuit;
	[Export] TextureButton ButtonDraw;
	[Export] AnimationPlayer Animation;
	[Export] Label LabelActionChoose;
	[Export] Label LabelDeck;
	[Export] HBoxContainer PlayerContainer;
	[Export] PackedScene PlayerPackedScene;
	[Export] PlayerContainer MyPlayer;
	[Export] Timer TimerShowActions;
	[Export] ProgressBar ProbaDieProgressBar;
	[Export]Week WeekBoxContainer;

	private Dictionary<long,PlayerContainer> players = new Dictionary<long, PlayerContainer>();
	private PlayerContainer CurrentActivePlayerContainer = null;
	public PlayersInfoClass players_dico;
	int idx_player_display = 0;
	int LastPlayerIdxAdded = 0;
	int IdxOfManche = -1; //-1 car start manche = set to 0;

	enum State {ChooseAction,WaitForOther,Reveal,RevealEndManche};
	State CurrentState = State.ChooseAction;

	enum Actions {Nothing = -1,Draw=0,Quit=1}; //0 = Draw / 1 = Quit
	Actions ActionChoose = Actions.Nothing;

	private long MyId;

	public override async void _Ready()
	{
		//ShowActionPlayerContainer.Visible = false;

		ButtonQuit.Pressed += QuitPressed;
		ButtonDraw.Pressed += DrawPressed;

		TimerShowActions.Timeout += ShowActionPlayer;

		MyId = Multiplayer.GetUniqueId();
		//Networking.Instance.PlayersChanged += UpdatePlayers;
		Networking.Instance.RoundFinish += RoundFinish;
		Networking.Instance.MancheFinishShopStart += MancheFinish;
		Networking.Instance.MancheStart += StartManche;
		Networking.Instance.PeerPlay += PeerPlay; //Idk how to do it
		
		InitPlayers();
		StartManche(); //Networking don't call it for the 1st time (bcs pbl et doit await mais dur)
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private async void StartManche()
	{
		IdxOfManche+=1;
		//Reset
		//ShowActionPlayerContainer.Visible = false;
		CurrentState = State.ChooseAction;
		Animation.Play("Reset");
		ActionChoose = Actions.Nothing;
		UpdatePlayers();
		ProbaDieProgressBar.Value = 16;
		WeekBoxContainer.NewManche(IdxOfManche); //COntains the idx of the day  / current manche
	}

	private async void MancheFinish() //Not good az don't throw the signal at the end
	{
		await Networking.Instance.RequestPlayersInfo(); //Get values from host

		//Reset values / Set
		players_dico = Networking.Instance.PlayersInfo;
		TimerShowActions.Start();
		CurrentState = State.RevealEndManche;
		//ShowActionPlayerContainer.Visible = true;
		idx_player_display = 0;
		ShowActionPlayer();	
	}
	private void QuitPressed()
	{
		SetTextQuitOrDraw(Actions.Quit);
	}
	private void DrawPressed()
	{
		SetTextQuitOrDraw(Actions.Draw);
	}
	private void SetTextQuitOrDraw(Actions Action)
	{
		if(CurrentState == State.ChooseAction)
		{
			CurrentState = State.WaitForOther;
			ActionChoose = Action;

			if (Action == Actions.Draw)
			{
				LabelActionChoose.Text = "Draw";
			}
			else
			{
				LabelActionChoose.Text = "Quit";
			}
			//In order to rpcid networking.instance.fctname il faut faire networking.instance.rpcid() le lancer depuis networking
			Networking.Instance.SetPeerAction((int)Action);
			MyPlayer.SetPlay();
			Animation.Play("Choose");

			return;
		}
	}

	private void UpdatePlayers()
	{
		foreach(Player player in players_dico.PlayerDic.Values)
		{
			PlayerContainer UpdateP;
			if(player.Id != MyId)
			{
				UpdateP = players[player.Id];
			}
			else
			{
				UpdateP = MyPlayer;
			}
			UpdateP.Love = player.Love;
			UpdateP.power = player.Power;
			UpdateP.DisplayValues();
		}
	}

	private void InitPlayers()
	{
		players_dico = Networking.Instance.PlayersInfo;

		foreach(Player player in players_dico.PlayerDic.Values)
		{
			String name = player.Pseudo;
			int Love = player.Love;
			AddPlayer(player.Id,name,Love);
		}
	}

	private void AddPlayer(long Id,String name,int Love)
	{

		if(Id==MyId) {
			MyPlayer.Init(Id,name,Love);
		}

		else
		{
			PlayerContainer new_player = (PlayerContainer)PlayerPackedScene.Instantiate();
			new_player.Init(Id,name,Love);
			players[Id] = new_player;
			PlayerContainer.AddChild(new_player);

			if (MyPlayer.Initiate)
			{
				PlayerContainer.MoveChild(new_player,LastPlayerIdxAdded);
				LastPlayerIdxAdded+=1;
			}
		}
	}

	//Called by networking when everyone has choose his action
	private async void RoundFinish()
	{
		await Networking.Instance.RequestPlayersInfo(); //Get values from host

		//Reset values / Set
		players_dico = Networking.Instance.PlayersInfo;
		TimerShowActions.Start();
		CurrentState = State.Reveal;
		//ShowActionPlayerContainer.Visible = true;
		idx_player_display = 0;
		ShowActionPlayer();
	}

	private void ShowActionPlayer()
	{
		// For each player update its power
		if(idx_player_display>= players.Keys.ToList().Count +1)//+1 car a le joueur qui est pas dans players.Keys
		{
			TimerShowActions.Stop();
			LabelDeck.Text = "Deck";
			CurrentActivePlayerContainer.Passive();
			if(CurrentState == State.RevealEndManche)
			{
				Networking.Instance.RpcId(1,nameof(Networking.Instance.FinishDisplayAll),Multiplayer.GetUniqueId());
			}
			else
			{
				EndRound();
			}
			return;
		}
		else
		{
			long id = players_dico.PlayerDic.Keys.ToList()[idx_player_display];
			if (id == Multiplayer.GetUniqueId())
			{
				ChangeCurrentActivePlayerContainer(MyPlayer);
			}
			else
			{
				ChangeCurrentActivePlayerContainer(players[id]);
			}

			ActivateCard(players_dico.PlayerDic[id]); //Deals damage to all other player
		
			idx_player_display++;
		}
	}

	public void ActivateCard(Player PlayerInfo)
	{

		//Draw the current action of the player
		if(PlayerInfo.Action == (int)Actions.Draw || PlayerInfo.CardIdDraw == 0) //Bomb and 1st bcs else draw quit
		{
			LabelDeck.Text = "Draw\n" +PlayerInfo.CardPower.ToString();
			CurrentActivePlayerContainer.SetNewLove(-PlayerInfo.CardPower); // - bcs bomb deals damage, no heal
		}
		else if(PlayerInfo.Action == (int)Actions.Quit)
		{
			LabelDeck.Text = "Quit";
		}
	}

	private void EndRound()
	{
		if(ActionChoose == Actions.Quit)
		{
			//Don't reset
			LabelActionChoose.Text = "Quit";
			LabelDeck.Text = "Wait for the others";
		}
		else
		{
			//Reset
			ProbaDieProgressBar.Value += 16;
			//ShowActionPlayerContainer.Visible = false;
			CurrentState = State.ChooseAction;
			Animation.Play("Reset");
		}

		foreach(PlayerContainer player in players.Values)
		{
			player.Reset();
		}
		MyPlayer.Reset();
	}

	private bool CheckDrawBomb(Player player)
	{
		if(player.CardIdDraw == 0 && player.Id == Multiplayer.GetUniqueId())
		{
			return true;
		}
		return false;
	}

	private void ChangeCurrentActivePlayerContainer(PlayerContainer player)
	{
		// In order to display the light of the player container
		if(CurrentActivePlayerContainer!= null)
		{
			CurrentActivePlayerContainer.Passive();
		}

		CurrentActivePlayerContainer = player;
		player.Active();
		// --End
	}

	private void PeerPlay(int PeerId)
	{
		players[PeerId].SetPlay();	
	}
}