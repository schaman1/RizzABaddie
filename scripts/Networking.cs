using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



public partial class Networking : Node
{
	// Called when the node enters the scene tree for the first time.
	[Signal] public delegate void HostCreatedEventHandler();
	[Signal] public delegate void CodeNotFoundEventHandler();
	[Signal] public delegate void ConnectionSuccessEventHandler();
	[Signal] public delegate void RoundFinishEventHandler();
	[Signal] public delegate void MancheStartEventHandler();
	[Signal] public delegate void MancheFinishShopStartEventHandler();
	[Signal] public delegate void PlayersChangedEventHandler();
	[Signal] public delegate void PlayersInitiatedEventHandler(); //When want to get existing players
	[Signal] public delegate void PeerPlayEventHandler(int PeerId); //When want to get existing players

	public string GameScene = "res://scenes/game.tscn";

	//State
	public string JoinCode = "00000";
	public bool IsHost = false;

	//Const
	private bool USE_STEAM = false;
	private int MAX_PLAYER = 4;

	// Valeurs de l'API Steam (GodotSteam)
	private const int SteamLobbyTypePublic = 2;     // Steam.LOBBY_TYPE_PUBLIC
	private const int SteamResultOk = 1;            // Steam.RESULT_OK
	private const int SteamChatEnterSuccess = 1;    // Steam.CHAT_ROOM_ENTER_RESPONSE_SUCCESS
	private const int SteamComparisonEqual = 0;     // Steam.LOBBY_COMPARISON_EQUAL
	
	//Multiplayer Values :
	private MultiplayerPeer _peer;
	private GodotObject _steam;

	//Infos game 
	// Infos sur le jeu
	public PlayersInfoClass PlayersInfo { get; private set; } = new PlayersInfoClass();
	public string Pseudo { get; set; } = "Pseudo";
	public int IdxBomb { get; private set; } = 0;
	public int NbrCards { get; set; } = 6; //Need something to set it by itself
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
	//With Steam
	private void OnLobbyCreated(long connected,long LobbyId)
	{
		if(connected == SteamResultOk)
		{
			IsHost = true;
			_peer = CreateSteamPeer();
			_peer.Set("server_relay",true);
			_peer.Call("create_host");
			Multiplayer.MultiplayerPeer = _peer;

			JoinCode = GenerateJoinCode();
			_steam.Call("setLobbyData",LobbyId,"join_code",JoinCode);
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
	public void SetPlayersInfo(Godot.Collections.Dictionary<long,Godot.Collections.Array<int>> Infos)
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
	public void FinishDisplayAll(long PeerId)
	{
		PlayersInfo.SetPlayerFInishDisplay(PeerId);
		if (PlayersInfo.HasAllPeerFinishManche())
		{
			StartManche(); //As everyone finish display macneh, start new one
		}
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

	//Game fct
	public void StartGame()
	{
		StartManche(false); //D'ont emit. Why ? Bcs player haven't time to connect things as load take time so needs to wait but difficult = let them initialise for the 1st time
		Rpc(nameof(LoadSceneGame));
		//StartManche(); //For the 1st time, players do it themself
	}
	public void StartManche(bool HasToEmit = true)
	{
		GD.Print("Start Manche");

		SetIdxBomb();
		foreach(Player player in PlayersInfo.PlayerDic.Values)
		{
			player.StartManche(IdxBomb);
		}
		if (HasToEmit)
		{
			Rpc(nameof(EmitStartManche)); //Emit to all player
		}
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
		IdxBomb = Rdm.RandiRange(0,NbrCards-1);
		//IdxBomb = 0;//ToModifie
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
