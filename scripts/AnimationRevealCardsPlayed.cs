using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AnimationRevealCardsPlayed : Node
{
    [Signal]public delegate void MovePlayerToCenterEventHandler();
    [Signal]public delegate void MakePlayerReactEventHandler(int CardId ,int Love);
    [Signal]public delegate void EndRoundEventHandler(int CardId);
    Timer TimerBetweenPlayers;
    Timer TimerBeforeShowCard;
    Timer TimerBeforePlayerReactToCard;
    PlayersInfoClass PlayerInfo;

    public static AnimationRevealCardsPlayed Instance {get;set;}

    int IdxPlayerShow = 0;
    long CurrentPeerId;
    bool IsEndManche = false;

    public override void _Ready()
    {
        TimerBetweenPlayers = new Timer {OneShot = false,WaitTime = 5};
        TimerBetweenPlayers.Timeout += MoveTargetPlayerToCenter;
        AddChild(TimerBetweenPlayers);
        TimerBeforeShowCard = new Timer {OneShot = true,WaitTime = 2};
        TimerBeforeShowCard.Timeout += ShowCard;
        AddChild(TimerBeforeShowCard);
        TimerBeforePlayerReactToCard = new Timer {OneShot = true,WaitTime = 1};
        TimerBeforePlayerReactToCard.Timeout += MakePlayerReactToCard;
        AddChild(TimerBeforePlayerReactToCard);

        Instance = this;
    }

	public void ShowCardPlayedByPlayers(PlayersInfoClass NewPlayerInfo,bool NewIsEndManche)
	{
        PlayerInfo = NewPlayerInfo;
        IsEndManche = NewIsEndManche;
        IdxPlayerShow = 0;
        TimerBetweenPlayers.Start();
        MoveTargetPlayerToCenter();
	}

    public void MoveTargetPlayerToCenter()
    {
        TimerBeforeShowCard.Start();

        if (IdxPlayerShow >= PlayerInfo.PlayerDic.Count)
        {
            TimerBetweenPlayers.Stop();
            if (!IsEndManche)
            {
                Rpc(nameof(EmitEndRound));
            }
            else
            {
                Networking.Instance.FinishDisplayAll();
            }
        }

        else
        {
            CurrentPeerId = PlayerInfo.PlayerDic.Keys.ToList()[IdxPlayerShow];
            RpcId(CurrentPeerId,nameof(FctEmitSignalMovePlayerToCenter));
            IdxPlayerShow ++; //So no more reliable
        }
    }

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
    public void FctEmitSignalMovePlayerToCenter()
    {
        EmitSignal(SignalName.MovePlayerToCenter);
    }

    public void ShowCard()
    {
        TimerBeforePlayerReactToCard.Start();
        //...
    }
    public void MakePlayerReactToCard()
    {
        Networking.Instance.CreatePopup(CurrentPeerId,PlayerInfo.PlayerDic[CurrentPeerId].CardPower);
        RpcId(CurrentPeerId,nameof(FctEmitSignalPlayerReact),PlayerInfo.PlayerDic[CurrentPeerId].CardIdDraw,PlayerInfo.PlayerDic[CurrentPeerId].Love);
    }
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
    public void FctEmitSignalPlayerReact(int CardIdDraw,int NewLove)
    {
        EmitSignal(SignalName.MakePlayerReact,CardIdDraw,NewLove);
    }
	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode =MultiplayerPeer.TransferModeEnum.Reliable)]
    public void EmitEndRound()
    {
        EmitSignal(SignalName.EndRound);
    }
}