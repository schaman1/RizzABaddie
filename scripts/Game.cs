using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

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
	[Export] Week WeekBoxContainer;
	[Export]HandDisplay Hand;
	[Export] ColorRectEndGame NodeEndGame;
	[Export]private PackedScene AddValuePackedScene;
	[Export]private PackedScene MainScene;
	[Export]private PackedScene PlayerScene;
	[Export]Losange LosangeYOrNo;
	[Export]CanvasLayerLoading LayerLoading;
	[Export]MultiplayerSpawner Spawner;
	[Export]PlayArea CardPlayArea;
	private Dictionary<long,PlayerContainer> players = new Dictionary<long, PlayerContainer>();
	private Dictionary<long,PlayerCharacter> PlayersCharacter = new  Dictionary<long,PlayerCharacter>();
	private PlayerContainer CurrentActivePlayerContainer = null;
	public PlayersInfoClass players_dico;
	int idx_player_display = 0;
	int LastPlayerIdxAdded = 0;
	int IdxOfManche = -1; //-1 car start manche = set to 0;
	int NbrOfPlayerSpawned = 0;

	enum State {ChooseAction,WaitForOther,Reveal,RevealEndManche};
	State CurrentState = State.ChooseAction;

	enum Actions {Nothing = -1,Draw=0,Quit=1}; //0 = Draw / 1 = Quit
	Actions ActionChoose = Actions.Nothing;
	private long MyId;

	public override async void _Ready()
	{
		//ShowActionPlayerContainer.Visible = false;
		LayerLoading.Show();

		ButtonQuit.Connect("pressed",Callable.From(QuitPressed));
		Spawner.Connect("spawned",Callable.From<Node>(OnPlayerSpawned));
		ButtonDraw.Connect("pressed",Callable.From(DrawPressed));

		TimerShowActions.Timeout += ShowActionPlayer;

		MyId = Multiplayer.GetUniqueId();
		//Networking.Instance.PlayersChanged += UpdatePlayers;
		Networking.Instance.Connect(nameof(Networking.Instance.RoundFinish),Callable.From(RoundFinish));
		Networking.Instance.Connect(nameof(Networking.Instance.MancheFinishShopStart),Callable.From(MancheFinish));
		Networking.Instance.Connect(nameof(Networking.Instance.MancheStart),Callable.From(StartManche));
		Networking.Instance.Connect(nameof(Networking.Instance.PeerPlay),Callable.From<int>(PeerPlay)); //Idk how to do it
		Networking.Instance.Connect(nameof(Networking.Instance.ProbaToSet),Callable.From<float>(SetProbaDie));
		Networking.Instance.Connect(nameof(Networking.Instance.EndGame),Callable.From<bool>(EndGame));
		Networking.Instance.Connect(nameof(Networking.Instance.OnPlayerRemove),Callable.From<long>(RemovePlayer));
		Networking.Instance.Connect(nameof(Networking.Instance.OnHostDisconnected),Callable.From(OnHostDisconnected));
		Networking.Instance.Connect(nameof(Networking.Instance.OnCardPlay),Callable.From<int,Godot.Collections.Array<int>>(PlayCard));
		Networking.Instance.Connect(nameof(Networking.Instance.StartCardGame),Callable.From<Vector2>(StartCardGame));

		if (Multiplayer.IsServer())
		{
			InitPlayers();
		}
		//StartManche(); //Networking don't call it for the 1st time (bcs pbl et doit await mais dur)
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double _delta)
	{
	}
	private async void StartManche()
	{
		await Networking.Instance.RequestPlayersInfo(); //Get values from host

		//await Task.Delay(TimeSpan.FromMilliseconds(1000));
		
		LayerLoading.Hide();
		IdxOfManche+=1;
		CurrentState = State.ChooseAction;
		Animation.Play("Reset");
		ActionChoose = Actions.Nothing;
		Hand.ShowHand();
		
		GD.Print("Start Manche");
		UpdatePlayers();
		WeekBoxContainer.NewManche(IdxOfManche); //COntains the idx of the day  / current manche
		Networking.Instance.RpcId(1,nameof(Networking.Instance.GetDiePourcentage),Multiplayer.GetUniqueId());
		ResetHasPlayerPlayerBeginManche();
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
			Hand.HideHand();
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

	private void OnPlayerSpawned(Node node)
	{
		if(node is PlayerCharacter NewPlayer)
		{
			int Id = NewPlayer.Name.ToString().ToInt();
			PlayersCharacter[Id] = NewPlayer; //add to list
			
			NewPlayer.Container.Init(Id,Networking.Instance.PlayersInfo.PlayerDic[Id].Pseudo,Networking.Instance.PlayersInfo.PlayerDic[Id].Love);
			players[Id] = NewPlayer.Container; //Set the container
			NbrOfPlayerSpawned++;
			if (NbrOfPlayerSpawned == Networking.Instance.PlayersInfo.PlayerDic.Count)
			{
				InitPlayers(); //As all players are here
				Networking.Instance.RpcId(1,nameof(Networking.Instance.SetInManche),Multiplayer.GetUniqueId()); //We are ready to start
			}
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

		if (Multiplayer.IsServer()) //If serveur, spawn each player for everyone
		{
			int sizePlayerX = 200;
			int i = 0;
			foreach(Player player in players_dico.PlayerDic.Values)
			{
				PlayerCharacter NewPlayer = (PlayerCharacter) PlayerScene.Instantiate();
				NewPlayer.Name = player.Id.ToString();
				PlayersCharacter[player.Id] = NewPlayer;

				NewPlayer.Container.Init(player.Id,player.Pseudo,player.Love);
				players[player.Id] = NewPlayer.Container; //Set the container

				//Set the position of the player
				Vector2 NewPosition = Vector2.Zero;
				NewPosition.X = -sizePlayerX*i;
				NewPlayer.Position = NewPosition;
				i++;
				AddChild(NewPlayer);
			}
			Networking.Instance.RpcId(1,nameof(Networking.Instance.SetInManche),Multiplayer.GetUniqueId());
		}

		Hand.Init();
	}

	private void AddPlayer(long Id,String name,int Love)
	{

		if(Id==MyId) {
			MyPlayer.Init(Id,name,Love);
		}

		else
		{
			// PlayerContainer new_player = (PlayerContainer)PlayerPackedScene.Instantiate();
			// new_player.Init(Id,name,Love);
			// players[Id] = new_player;
			// PlayerContainer.AddChild(new_player);

			// if (MyPlayer.Initiate)
			// {
			// 	PlayerContainer.MoveChild(new_player,LastPlayerIdxAdded);
			// 	LastPlayerIdxAdded+=1;
			// }
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
				Hand.StartManche();
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
		if(PlayerInfo.Action == (int)Actions.Draw || PlayerInfo.CardIdDraw == 0) //Bomb bcs with bomb, action = quit but actually has draw.
		{
			LabelDeck.Text = "Draw\n" +PlayerInfo.CardPower.ToString();
			CurrentActivePlayerContainer.SetNewLove(-PlayerInfo.CardPower); // - bcs bomb deals damage, no heal
			AddValue Add = (AddValue)AddValuePackedScene.Instantiate();
			if(PlayerInfo.Id != MyId)
			{	
				players[PlayerInfo.Id].AddChild(Add);
			}
			else
			{
				MyPlayer.AddChild(Add);
			}
			Add.Init(PlayerInfo.CardPower);
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
			Networking.Instance.RpcId(1,nameof(Networking.Instance.GetDiePourcentage),Multiplayer.GetUniqueId());
			//ProbaDieProgressBar.Value += 16;
			//ShowActionPlayerContainer.Visible = false;
			CurrentState = State.ChooseAction;
			Hand.ShowHand();
			Animation.Play("Reset");
			MyPlayer.Reset();
		}
		ResetHasPlayedPlayer();
	}

	public void ResetHasPlayedPlayer(){
	
		foreach(Player player in players_dico.PlayerDic.Values)
		{
			if(player.Id != MyId) //Bcs me in players_dico.PlayerDIc
			{
				if ((int)player.Action != 1)
				{
					players[player.Id].Reset();
				}
			}
		}
	}
	public void ResetHasPlayerPlayerBeginManche()
	{
		foreach(Player player in players_dico.PlayerDic.Values)
		{
			if(player.Id != MyId) //Bcs me in players_dico.PlayerDIc
			{
				if ((int)player.CurrentState != -1) // 1 = 
				{
					players[player.Id].Reset();
				}
			}
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

	public void SetProbaDie(float proba)
	{
		ProbaDieProgressBar.Value = proba;
	}

	private void EndGame(bool HasWin)
	{
		NodeEndGame.SetEndGame(HasWin);
	}

	private void OnHostDisconnected()
	{
		GetTree().ChangeSceneToPacked(MainScene);
	}

	private void RemovePlayer(long Id)
	{
		players[Id].SetColorDisconnected();// Set to black
	}

	private void PlayCard(int CardId,Godot.Collections.Array<int> CardInfo)
	{
		List<int> YesOrNoCard = [0,1];
		if (YesOrNoCard.Contains(CardId))
		{
			LosangeYOrNo.ShowYesOrNo(CardInfo[0]);
		}
	}

	private void StartCardGame(Vector2 NewPosition)
	{
		PlayersCharacter[Multiplayer.GetUniqueId()].SetInCardGame(NewPosition);
		CardPlayArea.StartCardGame();
	}
}