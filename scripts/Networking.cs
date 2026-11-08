using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



public partial class Networking : Node
{
	// Called when the node enters the scene tree for the first time.
	[Signal] public delegate void EndGameEventHandler(bool HasWin);
	[Signal] public delegate void OnHostDisconnectedEventHandler();
	[Signal] public delegate void OnPlayerRemoveEventHandler(long Id);
	[Signal] public delegate void HostCreatedEventHandler();
	[Signal] public delegate void CodeNotFoundEventHandler();
	[Signal] public delegate void ConnectionSuccessEventHandler();
	[Signal] public delegate void AllPeerLoadGameSceneEventHandler();
	[Signal] public delegate void RoundFinishEventHandler();
	[Signal] public delegate void MancheStartEventHandler();
	[Signal] public delegate void MancheFinishShopStartEventHandler();
	[Signal] public delegate void PlayersChangedEventHandler();
	[Signal] public delegate void ReceivedCardsEventHandler();
	[Signal] public delegate void PlayersInitiatedEventHandler(); //When want to get existing players
	[Signal] public delegate void PeerPlayEventHandler(int PeerId); //When want to get existing players
	[Signal] public delegate void ProbaToSetEventHandler(float proba); //When want to get existing players
	[Signal] public delegate void OnCardPlayEventHandler(int CardId,Godot.Collections.Array<int> CardInfo); //When want to get existing players

	public string GameScene = "res://scenes/game.tscn";
	private int CurrentManche = 0;
	private int NbrMaxManchePerGame = 3;

	//State
	public string JoinCode = "00000";
	public bool IsHost = false;

	//Const
	private bool USE_STEAM = false;
	private int MAX_PLAYER = 4;

	// Valeurs de l'API Steam (GodotSteam)
	private const int SteamLobbyTypePublic = 2;     // Steam.LOBBY_TYPE_PUBLIC
	private const int SteamLobbyTypePrivate = 0;     // Steam.LOBBY_TYPE_PRIVATE
	private const int SteamResultOk = 1;            // Steam.RESULT_OK
	private const int SteamChatEnterSuccess = 1;    // Steam.CHAT_ROOM_ENTER_RESPONSE_SUCCESS
	private const int SteamComparisonEqual = 0;     // Steam.LOBBY_COMPARISON_EQUAL
	private long LobbyId;
	
	//Multiplayer Values :
	private MultiplayerPeer _peer;
	private GodotObject _steam;

	//Infos game 
	// Infos sur le jeu
	public PlayersInfoClass PlayersInfo { get; private set; } = new PlayersInfoClass();
	public string Pseudo { get; set; } = "Pseudo";
	public int IdxBomb { get; private set; } = 0;
	RandomNumberGenerator Rdm = new RandomNumberGenerator();

	//Static !important
	public static Networking Instance {get;set;}

	public override void _Ready()
	{
		if (USE_STEAM)
		{
			_steam = Engine.GetSingleton("Steam");
			_steam.Call("initRelayNetworkAccess");
			_steam.Connect("lobby_created",Callable.From<long,long>(OnLobbyCreated));
			_steam.Connect("lobby_joined",Callable.From<long,long,bool,long>(OnLobbyJoined));
			_steam.Connect("join_requested",Callable.From<long,long>(OnJoinRequest));
			_steam.Connect("lobby_match_list",Callable.From<Godot.Collections.Array>(OnLobbyMatchList));
		}

		Multiplayer.ConnectedToServer += () =>EmitSignal(SignalName.ConnectionSuccess);
		Multiplayer.ConnectionFailed += () =>EmitSignal(SignalName.CodeNotFound);
		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;

		Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (USE_STEAM)
		{
			_steam.Call("run_callbacks");
		}
	}

	public void HostLobby()
	{
		if (USE_STEAM)
		{
			_steam.Call("createLobby",SteamLobbyTypePublic,MAX_PLAYER);
		}
		else
		{
			HostEnet();
		}
		PlayersInfo.AddPlayer(Multiplayer.GetUniqueId(),Pseudo);
		SetLobbyPublic(); //Not necessary as only called at the beginning = already public
	}

	public void RestartGame()
	{
		SetLobbyPublic();
	}

	//Without Steam
	private void HostEnet()
	{
		IsHost = true;
		ENetMultiplayerPeer enet = new ENetMultiplayerPeer();
		enet.CreateServer(9999,MAX_PLAYER);
		_peer = enet;
		Multiplayer.MultiplayerPeer = _peer;
		JoinCode = "LOCAL";
		EmitSignal(SignalName.HostCreated);
	}

	private void SetLobbyPrivate()
	{
		if (USE_STEAM)
		{
			_steam.Call("setLobbyType",LobbyId,SteamLobbyTypePrivate);
		}
		else
		{
			_peer.RefuseNewConnections = true;
		}
	}
	private void SetLobbyPublic()
	{
		if (USE_STEAM)
		{
			_steam.Call("setLobbyType",LobbyId,SteamLobbyTypePublic);
		}
		else
		{
			_peer.RefuseNewConnections = false;
		}
		
	}
	//With Steam
	private void OnLobbyCreated(long connected,long _LobbyId)
	{
		if(connected == SteamResultOk)
		{
			IsHost = true;
			_peer = CreateSteamPeer();
			_peer.Set("server_relay",true);
			_peer.Call("create_host");
			Multiplayer.MultiplayerPeer = _peer;

			JoinCode = GenerateJoinCode();
			_steam.Call("setLobbyData",_LobbyId,"join_code",JoinCode);
			LobbyId = _LobbyId;
			EmitSignal(SignalName.HostCreated);
		}
	}
	private static MultiplayerPeer CreateSteamPeer(){
		return ClassDB.Instantiate("SteamMultiplayerPeer").As<MultiplayerPeer>();
	}

	//----Client
	public void OnLobbyJoined(long LobbyId,long Permission, bool _locked, long response)
	{
		if (response == SteamChatEnterSuccess)
		{
			if(IsHost){return;}
			_peer = CreateSteamPeer();
			_peer.Set("server_relay",true);
			_peer.Call("create_client",_steam.Call("getLobbyOwner",LobbyId));
			Multiplayer.MultiplayerPeer = _peer;
			//EmitSignal(SignalName.ConnectionSuccess);
		}
	}

	public void OnJoinRequest(long LobbyId,long SteamId)
	{
		_steam.Call("joinLobby",LobbyId);
	}

	public void JoinByCode(String Code)
	{
		if (USE_STEAM)
		{
			_steam.Call("addRequestLobbyListStringFilter","join_code",Code,SteamComparisonEqual);
			_steam.Call("addRequestLobbyListResultCountFilter",1);
			_steam.Call("requestLobbyList");
		}
		else
		{
			JoinEnet();
		}
	}
	public void JoinEnet()
	{
		ENetMultiplayerPeer enet = new ENetMultiplayerPeer();
		enet.CreateClient("127.0.0.1",9999); //9999 = enet port
		_peer = enet;
		Multiplayer.MultiplayerPeer = _peer;
	}
	private void OnLobbyMatchList(Godot.Collections.Array Lobbies)
	{
		if (Lobbies.Count == 0)
		{
			EmitSignal(SignalName.CodeNotFound);
			return;
		}
		else
		{
			_steam.Call("joinLobby",Lobbies[0]);
		}
	}

	public void OnPeerConnected(long Id)
	{
		if (IsHost)
		{
			RpcId(Id, nameof(GetPeerIdPseudo));
		}
	}

	public void OnPeerDisconnected(long Id)
	{
		GD.Print("Player disconnected");
		if(Id == 1){//Means Host
			EmitSignal(SignalName.OnHostDisconnected);
			PlayersInfo.PlayerDic.Clear();
		}
		else
		{
			PlayersInfo.PlayerDic[Id].SetDisconnected();
			if (IsHost)
			{
				RpcId(1,nameof(SendActionToHost),(int)Actions.Quit,Id);
				Rpc(nameof(SetPeerHasPlay),Id);
				EmitSignal(SignalName.PeerPlay,Id);//Server has to do it itself as calllocal = false;
			}
			EmitSignal(SignalName.OnPlayerRemove,Id);
		}
	}

	//Rpc
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void GetPeerIdPseudo() //GetPlayerPseudo + Call create player 
	{
		RpcId(1,nameof(CreatePlayerFromPseudo),Pseudo,Multiplayer.GetUniqueId());
		//Pseudo = ReturnUniquePseudo(Pseudo); //Set the pseudo to the unique pseudo = need to call this only 1 time foreach player
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void CreatePlayerFromPseudo(String Pseudo,long Id) //Get Unique pseudo + Call Create player to all peer 
	{
		Pseudo = ReturnUniquePseudo(Pseudo); //Set the pseudo to the unique pseudo = need to call this only 1 time foreach player	
		Rpc(nameof(CreatePlayer),Pseudo,Id);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void CreatePlayer(String Pseudo,int Id)
	{
		PlayersInfo.AddPlayer(Id,Pseudo);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void LoadSceneGame()
	{
		foreach(Player player in PlayersInfo.PlayerDic.Values)
		{
			if (player.HasBeenDisconnected())
			{
				PlayersInfo.PlayerDic.Remove(player.Id);
			}
			else
			{
				player.Restart();
			}
		}
		GetTree().ChangeSceneToFile(GameScene);
	}

	//The 2  : When connect tell the serv want existing players = get it
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SendPlayerAlreadyConnecteed(int PeerId)
	{
		RpcId(PeerId,nameof(InitiatePlayers),PlayersInfo.ReturnExistingPlayers());
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void InitiatePlayers(Godot.Collections.Dictionary<long,String> Datas)
	{
		foreach(long Id in Datas.Keys)
		{
			PlayersInfo.AddPlayer(Id,Datas[Id]);
		}
		EmitSignal(SignalName.PlayersInitiated);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SendPlayersInfo(int PeerId)
	{
		RpcId(PeerId,nameof(SetPlayersInfo),PlayersInfo.CompressDatasPlayer()); //Needs to compress bcs with rpc can only pass Godot.Variant type
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetPlayersInfo(Godot.Collections.Dictionary<long,Godot.Collections.Array<int>>Infos)
	{
		PlayersInfo.ExpandDatasPlayer(Infos);
		EmitSignal(SignalName.PlayersChanged);
	}

	//Set actions
	public void SetPeerAction(int Action)
	{
		int MyId = Multiplayer.GetUniqueId();
		RpcId(1,nameof(SendActionToHost),Action,MyId);
		Rpc(nameof(SetPeerHasPlay),MyId);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = false,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetPeerHasPlay(int PeerId)
	{
		EmitSignal(SignalName.PeerPlay,PeerId);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SendActionToHost(int Action,int PeerId){
		PlayersInfo.SetPeerAction(PeerId,Action);
		if (PlayersInfo.HasAllPeerPlay())
		{
			PlayersInfo.DrawCardForAllPeer();

			if (PlayersInfo.IsEndManche())
			{
				GD.Print("Manche Finish");
				PlayersInfo.EndManche();
				Rpc(nameof(EmitMancheFinish));
			}
			else
			{
				GD.Print("Round Finish");
				PlayersInfo.EndRound();
				Rpc(nameof(EmitRoundFinish));
			}
		}
	}
	//Emit
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void EmitRoundFinish()
	{
		EmitSignal(SignalName.RoundFinish);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void EmitStartManche()
	{
		EmitSignal(SignalName.MancheStart);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void EmitMancheFinish()
	{
		EmitSignal(SignalName.MancheFinishShopStart);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void EmitEndGame(bool HasWin)
	{
		EmitSignal(SignalName.EndGame,HasWin);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetInManche(long PeerId)
	{
		PlayersInfo.SetInManche(PeerId);
		if (PlayersInfo.IsAllPlayerInManche())
		{
			StartGame();
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void FinishDisplayAll(long PeerId)
	{
		PlayersInfo.SetPlayerFInishDisplay(PeerId);
		if (PlayersInfo.HasAllPeerFinishManche())
		{
			if (CurrentManche == NbrMaxManchePerGame)
			{
				PlayersInfo.SetWinner();

				foreach(Player player in PlayersInfo.PlayerDic.Values)
				{
					RpcId(player.Id,nameof(EmitEndGame),PlayersInfo.Winners.Contains(player));
				}
			}
			else
			{
				StartManche(); //As everyone finish display manche, start new one
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void GetDiePourcentage(int PeerId)
	{
		float proba = PlayersInfo.PlayerDic[PeerId].MyDeck.GetProbaBomb();
		RpcId(PeerId,nameof(SetDiePourcentage),proba);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void SetDiePourcentage(float proba)
	{
		EmitSignal(SignalName.ProbaToSet,proba);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void PlayCard(int IdCard,long PeerId)
	{
		Godot.Collections.Array<int> res = PlayersInfo.PlayerDic[PeerId].MyHand.PlayCard(IdCard,PlayersInfo.PlayerDic[PeerId].MyDeck,PeerId);
		if (res != null) //Means if the card exist
		{
			RpcId(PeerId,nameof(ActiveCard),res);
		}
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void RemoveCard(int CardId)
	{
		PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyHand.RemoveCard(CardId,Multiplayer.GetUniqueId());
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void AddCard(int CardId)
	{
		PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyHand.AddCard(CardId,Multiplayer.GetUniqueId());
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
	public void ActiveCard(Godot.Collections.Array<int> res)
	{
		EmitSignal(SignalName.OnCardPlay,res[0],res[1..].ToArray<int>());
	}

	//Utils
	private String ReturnUniquePseudo(String Pseudo)
	{
		int nbr = -1;
		String add = "";
		while (PlayersInfo.IsPseudoTaken(Pseudo + add))
		{
			nbr++;
			add = nbr.ToString();
		}
		return Pseudo+add;
	}

	private String GenerateJoinCode(int length = 5)
	{
		String Code = "";
		for(int i = 0; i < length; i++)
		{
			int k = Rdm.RandiRange(0,9);
			Code += k.ToString();
		}

		return Code;
	} 

	public void ChangeToGameScene()
	{
		SetLobbyPrivate();
		Rpc(nameof(LoadSceneGame));
		//When player in game_scene, call networking.SetInManche
		//THen check, if everyplayer in manche, send startManche
	}

	//Game fct
	public void StartGame()
	{
		CurrentManche = 0;
		foreach(Player player in PlayersInfo.PlayerDic.Values)
		{
			player.Restart();
		}
		StartManche();
	}
	public void StartManche()
	{
		GD.Print("Start Manche");
		CurrentManche+=1;

		SetIdxBomb();
		foreach(Player player in PlayersInfo.PlayerDic.Values)
		{
			player.StartManche(IdxBomb);
		}
		Rpc(nameof(EmitStartManche)); //Emit to all player
	}
	public void StartRound()
	{
		foreach(Player player in PlayersInfo.PlayerDic.Values)
		{
			player.ResetAction();
		}
	}
	public void SetIdxBomb()
	{
		int NbrOfCards = PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyDeck.GetNbrOfCardsTot();
		IdxBomb = Rdm.RandiRange(0,NbrOfCards-1);
	}

	public async Task RequestPlayersInfo()
	{
		if (!IsHost)
		{
			RpcId(1,nameof(SendPlayersInfo),Multiplayer.GetUniqueId());
			await ToSignal(this,SignalName.PlayersChanged);
		}
	}

	public async Task RequestPlayerAlreadyConnected(){

		if (!IsHost) //Prevention but not necessary bcs host never call it
		{
			RpcId(1,nameof(SendPlayerAlreadyConnecteed),Multiplayer.GetUniqueId());
		}
		else
		{
			EmitSignal(SignalName.PlayersInitiated);
		}

		await ToSignal(this,SignalName.PlayersInitiated);

	}

}
