using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Markup;

public partial class HandDisplay : HBoxContainer
{
	// Called when the node enters the scene tree for the first time.
	[Export] PackedScene CardScene;

	String FilePath = "data/data_card_text.json";

	Dictionary<int,String> TextCards = new Dictionary<int, String>();

	List<Card> CardsInHand = new List<Card>();

	enum State {Normal,Block};
	State CurrentState = State.Normal;

	int IdxCardHover = -1;
	Vector2 InitPosition;

	public override void _Ready()
	{
		InitPosition = Position;
	}
	public void Init(){
		InitDataCard();
		int i = Multiplayer.GetUniqueId();
		Player play = Networking.Instance.PlayersInfo.PlayerDic[i];

		play.MyHand.Connect(nameof(play.MyHand.CardAdded),Callable.From<int>(AddCard));
		play.MyHand.Connect(nameof(play.MyHand.CardRemoved),Callable.From<int>(RemoveCard));
	}

	// public void DisposeEvent()
	// {
	// 	Networking.Instance.PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyHand.CardAdded-= AddCard;
	// 	Networking.Instance.PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyHand.CardRemoved-= RemoveCard;
	// }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void StartManche()
	{
		int i = 0;
		while (i < CardsInHand.Count)
		{
			Card card = CardsInHand[i];
			CardsInHand.RemoveAt(i);
			//RemoveChild(card);
			card.Destroy();
			i++;
		}
	}

	private void InitDataCard()
	{
		var dataFile = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
		Json json = new Json();
		var parse_res = json.Parse(dataFile.GetAsText());
		if(parse_res != Error.Ok)
		{
			GD.PushError("Can't parse data file");
		}
		else
		{
			Godot.Collections.Array json_data = (Godot.Collections.Array)json.Data;
			foreach(Godot.Collections.Dictionary InfoCard in json_data){
				//CardInfo NewCard = new CardInfo((int)TextCard["Id"],(int)TextCard["Power"],(String)TextCard["Title"]);
				TextCards[(int)InfoCard["Id"]] = (String)InfoCard["Text"];
			}
		}
	}

	public void AddCard(int id)
	{
		Card NewCard = (Card)CardScene.Instantiate();
		AddChild(NewCard);
		NewCard.Init(id,TextCards[id],CardsInHand.Count);
		NewCard.OnHover += CardHover;
		NewCard.OnStopHover += CardStopHover;
		NewCard.OnClick += PlayCard;
		CardsInHand.Add(NewCard);
		GD.Print("Added card");
	}

	public void RemoveCard(int id)
	{
		int i = 0;
		while(i<CardsInHand.Count && CardsInHand[i].Id!=id)
		{
			i++;
		}
		if (i < CardsInHand.Count)
		{
			Card card = CardsInHand[i];
			CardsInHand.RemoveAt(i);
			//RemoveChild(card);
			card.Destroy();
		}
		for(int j = i; j < CardsInHand.Count; j++)
		{
			CardsInHand[j].IdxList -= 1;
		}
	}

	private void InitCardsFromNetwork()
	{
		foreach(int id in Networking.Instance.PlayersInfo.PlayerDic[1].MyHand.IdCardsRemaining)
		{
			AddCard(id);
		}
	}

	private void CardHover(int Idx)
	{
		if (!IsOneCardHover() && CurrentState==State.Normal)
		{
			//Then hover this card
			IdxCardHover = Idx;
			CardsInHand[Idx].SetHover();
			for(int i = 0; i < CardsInHand.Count; i++)
			{
				if (i < Idx)
				{
					CardsInHand[i].SetPosition(GetPositionWhenNotHover(-1,CardsInHand[i]));
				}
				else if (i == Idx)
				{
					CardsInHand[i].SetPosition(GetPositionWhenHover(-1,CardsInHand[i]));
					CardsInHand[i].Scale = new Vector2(1.2f,1.2f);
				}
				else if (i > Idx)
				{
					CardsInHand[i].SetPosition(GetPositionWhenNotHover(1,CardsInHand[i]));
				}
			}
		}
	}
	private void CardStopHover(int Idx)
	{
		if (IdxCardHover != -1)
		{
			CardsInHand[Idx].SetNormal();
			for(int i = 0; i < CardsInHand.Count; i++)
			{
				if (i < Idx)
				{
					CardsInHand[i].SetPosition(GetPositionWhenNotHover(1,CardsInHand[i]));
				}
				else if (i == Idx)
				{
					CardsInHand[i].SetPosition(GetPositionWhenHover(1,CardsInHand[i]));
					CardsInHand[i].Scale = new Vector2(1.0f,1.0f);
				}
				else if (i > Idx)
				{
					CardsInHand[i].SetPosition(GetPositionWhenNotHover(-1,CardsInHand[i]));
				}
			}
			IdxCardHover = -1;
		}
		
	}

	private Vector2 GetPositionWhenNotHover(int Direction,Card card){

		Vector2 NewPosition = card.Position;
		NewPosition.X += 50*Direction;

		return NewPosition;
	}
	private Vector2 GetPositionWhenHover(int Direction,Card card){

		Vector2 NewPosition = card.Position;
		NewPosition.Y += 50*Direction;

		return NewPosition;
	}

	private bool IsOneCardHover()
	{
		for(int i = 0; i < CardsInHand.Count; i++)
		{
			if (CardsInHand[i].IsHover())
			{
				return true;
			}
		}
		return false;
	}

	private void PlayCard(int Idx)
	{
		Networking.Instance.RpcId(1,nameof(Networking.Instance.PlayCard),CardsInHand[Idx].Id,Multiplayer.GetUniqueId());
	}

	public void HideHand()
	{
		Vector2 NewPosition = Position;
		NewPosition.Y+=100+10; //+10 to make it less visible (at the end where it is slow)
		Tween tweenShowHide = CreateTween();
		tweenShowHide.SetTrans(Tween.TransitionType.Expo);
		tweenShowHide.SetEase(Tween.EaseType.Out);
		tweenShowHide.TweenProperty(this,"position",NewPosition,1.0f);
		CurrentState = State.Block;
	}
	public void ShowHand()
	{
		Tween tweenShowHide = CreateTween();
		tweenShowHide.SetTrans(Tween.TransitionType.Expo);
		tweenShowHide.SetEase(Tween.EaseType.Out);
		tweenShowHide.TweenProperty(this,"position",InitPosition,1.0f);
		CurrentState = State.Normal;
	}
}
