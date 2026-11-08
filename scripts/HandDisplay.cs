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

	int IdxCardHover = -1;

	public override void _Ready()
	{
	}
	public void Init(){
		InitDataCard();
		Networking.Instance.PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyHand.CardAdded += AddCard;
		Networking.Instance.PlayersInfo.PlayerDic[Multiplayer.GetUniqueId()].MyHand.CardRemoved += RemoveCard;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
		GD.Print("AddCard Hand Display");
		Card NewCard = (Card)CardScene.Instantiate();
		AddChild(NewCard);
		NewCard.Init(id,TextCards[id],CardsInHand.Count);
		NewCard.OnHover += CardHover;
		NewCard.OnStopHover += CardStopHover;
		NewCard.OnClick += PlayCard;
		CardsInHand.Add(NewCard);
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
			CardsInHand[i].Destroy();
			CardsInHand.RemoveAt(i);
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
		if (!IsOneCardHover())
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
}
