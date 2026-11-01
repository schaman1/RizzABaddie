using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Linq;


public partial class Deck : Node
{
	// Called when the node enters the scene tree for the first time.
	public List<CardInfo> Cards =  new List<CardInfo>();
	public List<CardInfo> RemainingCards =  new List<CardInfo>();

	private RandomNumberGenerator Rdm = new RandomNumberGenerator();

	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void InitDeck() //Not beautifull but i did it myself suuh !!!
	{	
		String FilePath = "data/data_card_start.json";
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
			foreach(Godot.Collections.Dictionary TextCard in json_data){
				CardInfo NewCard = new CardInfo((int)TextCard["Id"],(int)TextCard["Power"],(String)TextCard["Title"]);
				Cards.Add(NewCard);
			}
		}
	}

	public float GetProbaBomb()
	{
		//GD.Print("Probqa : ",(RemainingCards.Count-1)*100/Cards.Count);
		return 100 - (RemainingCards.Count-1)*100/Cards.Count;//% out of 100
	}

	public int GetNbrOfCardsTot()
	{
		return Cards.Count;
	}

	/*public void InitDeck2()
	{
		string fileName = "res://data/data_card_start.json";
		using var file  = FileAccess.Open(fileName,FileAccess.ModeFlags.Read);
		string jsonString = file.GetAsText();
		Cards = JsonSerializer.Deserialize<List<CardInfo>>(jsonString);
	}*/ //Not by me but i redo it by myself suuuh ! 

	public void StartManche(int idxBomb)
	{
		RemainingCards = new List<CardInfo>(Cards.Count);//Copy the list
		foreach(CardInfo card in Cards)
		{
			CardInfo newCard = new CardInfo(card.Id,card.Power,card.Title);
			RemainingCards.Add(newCard);
		}
		RandomizeDeck();
		SetBombPosition(idxBomb);
	}

	private void SetBombPosition(int idxBomb)
	{
		for(int i = 0; i < RemainingCards.Count; i++)
		{
			if (RemainingCards[i].Title == "Bomb")
			{
				CardInfo Tmp = RemainingCards[i];
				RemainingCards[i] = RemainingCards[idxBomb];
				RemainingCards[idxBomb] = Tmp;
			}
		}	
	}

	public void RandomizeDeck()
	{
		for(int i = 0;i<RemainingCards.Count;i++)
		{
			int newPos = Rdm.RandiRange(0,RemainingCards.Count-1);
			CardInfo TmpCard = RemainingCards[i];
			RemainingCards[i] = RemainingCards[newPos];
			RemainingCards[newPos] = TmpCard;
		}
	}

	public CardInfo DrawCard()
	{
		int pos = 0; //0 as not random as we already randomize the deck
		CardInfo card = RemainingCards[pos];
		RemainingCards.RemoveAt(pos);

		return card;
	}

	public void Restart()
	{
		Cards.Clear();
		InitDeck();
	}

}
