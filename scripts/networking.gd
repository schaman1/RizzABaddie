extends Node

signal host_created()
signal join_code_ready()
signal code_not_found()
signal connection_success()

signal players_info_initiated()
signal round_finish()

signal get_player_pseudo
signal players_changed

var game_scene:= "res://scenes/game.tscn"

var is_host = false
var JoinCode = "00000"

const LOBBY_TYPE := Steam.LobbyType.LOBBY_TYPE_PUBLIC
const MAX_PLAYER := 4;

const USE_STEAM = false;

#var peer : SteamMultiplayerPeer
var peer

#Info sur le jeu : 
var players_info : Dictionary
var pseudo = "Pseudo"
var idx_bomb :=0
var nbr_cards = 10
var Rdm = RandomNumberGenerator.new()

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	
	if(USE_STEAM):
		Steam.initRelayNetworkAccess()
		Steam.lobby_created.connect(on_lobby_created)
		Steam.lobby_joined.connect(on_lobby_joined)
		Steam.join_requested.connect(on_join_requested)
		Steam.lobby_match_list.connect(on_lobby_match_list) #Utile
		
	#Remove else : 
	# ENet : le client est prévenu quand la connexion aboutit
	multiplayer.connected_to_server.connect(func(): connection_success.emit())
	multiplayer.connection_failed.connect(func(): code_not_found.emit())
	multiplayer.peer_connected.connect(on_peer_connected)
	
	start_round()
	start_manche()

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
	if(USE_STEAM):
		Steam.run_callbacks()
	
func host_lobby() ->void:
	#This will call : Lobby_create & lobby_joined signals
	if(USE_STEAM):
		Steam.createLobby(LOBBY_TYPE,MAX_PLAYER)
	else:
		_host_enet()
		
	players_info[multiplayer.get_unique_id()] = {"Name": pseudo, "Hp": 100, "Power":0,"Action" : -1,"IdCard":0,"PowerCard":0} #Create player in players_info
	
func _host_enet() -> void:
	is_host = true
	peer = ENetMultiplayerPeer.new()
	peer.create_server(9999, MAX_PLAYER) #9999 = port
	multiplayer.multiplayer_peer = peer
	host_created.emit()
	JoinCode = "LOCAL"        # code bidon, juste pour l'affichage
	join_code_ready.emit()
		
func on_lobby_created(connected : int, lobby_id : int):
	
	if connected==Steam.RESULT_OK:
		
		is_host = true
		
		peer = SteamMultiplayerPeer.new()
		peer.server_relay = true
		peer.create_host()
		multiplayer.multiplayer_peer = peer
		
		host_created.emit()
			
		JoinCode = generated_join_code()
		Steam.setLobbyData(lobby_id,"join_code",JoinCode) #Store it in order to have : join_by_code work
		join_code_ready.emit()

func on_lobby_joined(lobby_id : int,_permission : int, _locked : bool, response : int):
	
	if response == Steam.CHAT_ROOM_ENTER_RESPONSE_SUCCESS:
		
		if is_host :
			return #c notre propre lobby on ignore

		peer = SteamMultiplayerPeer.new()
		peer.server_relay = true
		peer.create_client(Steam.getLobbyOwner(lobby_id))
		multiplayer.multiplayer_peer = peer
		connection_success.emit() #Change scene to client scene

func on_join_requested(lobby_id:int, _steam_id:int):
	Steam.joinLobby(lobby_id)
	
func on_peer_connected(id : int):
	
	if(is_host):
		get_peer_id_pseudo.rpc_id(id)
		await get_player_pseudo
		
func create_player(player_pseudo,id):
		var new_player_pseudo = return_unique_pseudo(player_pseudo)
		players_info[id] = {"Name": new_player_pseudo, "Hp": 100, "Power":0,"Action" : -1,"IdCard":0,"PowerCard":0} #Add player in player_info

@rpc("reliable","any_peer","call_local")
func get_peer_id_pseudo():

	set_player_pseudo.rpc_id(0,pseudo,multiplayer.get_unique_id())

@rpc("reliable","any_peer","call_local")
func set_player_pseudo(player_pseudo,id):
	create_player(player_pseudo,id)

func return_unique_pseudo(pseudo_given):
	var unique_pseudo = pseudo_given #In order to have a unique pseudo
	var is_ok = false
	var add = -1
	while is_ok == false:
		add+=1
		is_ok = true
		for player in players_info.values():
			if(add!=0):
				unique_pseudo = pseudo+str(add)

			if(player["Name"]==unique_pseudo):
				is_ok = false
				
				break
	return unique_pseudo

func generated_join_code(length:int = 5) ->String:
	const CHARS := "123456789"
	var code := ""

	for i in length : 
		code += CHARS[randi_range(0,CHARS.length()-1)]
	
	return code

func join_by_code(code:String):
	if(USE_STEAM):
		Steam.addRequestLobbyListStringFilter("join_code", code, Steam.LOBBY_COMPARISON_EQUAL)
		Steam.addRequestLobbyListResultCountFilter(1)
		Steam.requestLobbyList()
	else:
		_join_enet()

func _join_enet() -> void:
	is_host = false
	peer = ENetMultiplayerPeer.new()
	peer.create_client("127.0.0.1", 9999) #9999 = ENET_POrt
	multiplayer.multiplayer_peer = peer
	# connection_success sera émis par multiplayer.connected_to_server
	
#Appele avec Steam.requestLobbyList
func on_lobby_match_list(lobbies: Array) -> void:
	if lobbies.is_empty():
		push_error("Aucune partie trouvée avec ce code")
		code_not_found.emit()
		return
	Steam.joinLobby(lobbies[0])

func start_game():
	#Change all player scene to game scene/ only host can do this bcs : authority !
	load_scene_game.rpc()
	start_round()
	
func start_round():
	
	for player in players_info.values() :
		player["Action"] = -1

func start_manche():
	set_values_start_round.rpc(idx_bomb,players_info)
	idx_bomb = Rdm.randi_range(0,nbr_cards-1);

@rpc("authority","call_local","reliable")
func set_values_start_round(new_idx_bomb,new_players_info):
	idx_bomb = new_idx_bomb
	players_info = new_players_info
	
@rpc("authority","call_local","reliable")
func load_scene_game():
	get_tree().change_scene_to_file(game_scene)

func request_player_infos_init(): #Send to serv i want the infos about the players
	if(not is_host):
		send_to_player_players_info.rpc_id(1,multiplayer.get_unique_id())
		await players_changed
		players_info_initiated.emit()
		
	else:
		players_info_initiated.emit()
	
@rpc("reliable","any_peer","call_local") #Okay the peer_id want the infos about the players, so we send them
func send_to_player_players_info(peer_id:int):
	set_players_info.rpc_id(peer_id,players_info)

@rpc("reliable","authority","call_local") #Receive the infos from the serv
func set_players_info(players_info_receive):
	players_info = players_info_receive
	players_changed.emit()
	
#Play when player chose action
@rpc("reliable","any_peer","call_local")
func peer_choose_action(action:int,peer_id:int,Id:int,Power:int):
	players_info[peer_id]["Action"] = action;
	players_info[peer_id]["CardId"] = Id;
	players_info[peer_id]["PowerCard"] = Power;

	if(has_all_peer_play()):
		send_action_to_all_peer()

func has_all_peer_play():
	print(players_info)
	for player in players_info.values() :
		if player["Action"]==-1:
			return false;
	return true;

func send_action_to_all_peer():
	print("Send action to all player");
	add_power_to_all_peer()
	for peer_id in players_info.keys():
		
		start_show_action_round.rpc_id(peer_id,players_info)
		
	start_round()
		
@rpc("reliable","any_peer","call_local")
func start_show_action_round(new_players_info):
	players_info = new_players_info
	round_finish.emit()

func add_power_to_all_peer():
	for player in players_info.values():
		player["Power"] += player["PowerCard"]
