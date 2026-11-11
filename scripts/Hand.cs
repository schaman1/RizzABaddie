using Godot;
using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Linq;


public partial class Hand : Node
{
    [Signal] public delegate void CardAddedEventHandler(int id);
    [Signal] public delegate void CardRemovedEventHandler(int id);
    List<int> IdCards = new List<int>();
    public Godot.Collections.Array<int> IdCardsRemaining = new Godot.Collections.Array<int>();

	public override void _Ready()
	{
	}

    public void InitCards(long PeerId)
    {
        IdCards.Clear();
        IdCardsRemaining.Clear();
        //IdCards.Add(0);
        IdCards.Add(1);
    }

    public void StartManche(long PlayerId)
    {
        IdCardsRemaining.Clear();
        foreach(int id in IdCards)
        {
            IdCardsRemaining.Add(id);
            // if (PlayerId==1) //If player is Host
            // {
            // }
            AddCardServ(id,PlayerId);
        }
    }
/*
    public void ExpandIdCardsRemaining(Godot.Collections.Array<int> RemainingCards)
    {
        GD.Print("Serveur : ",RemainingCards," Client : ",IdCardsRemaining);
        for(int i = 0;i<IdCardsRemaining.Count;i++)
        {
            if (!RemainingCards.Contains(IdCardsRemaining[i]))
            {
                int Id = IdCardsRemaining[i];
                IdCardsRemaining.Remove(IdCardsRemaining[i]);
                EmitSignal(SignalName.CardRemoved,Id);
            }
        }
        foreach(int i in RemainingCards)
        {
            if (!IdCardsRemaining.Contains(i))
            {
                IdCardsRemaining.Add(i);
                EmitSignal(SignalName.CardAdded,i);
            }
        }
    }

    public Godot.Collections.Array<int> CompressIdCardsRemaining(){
        Godot.Collections.Array<int> res = new Godot.Collections.Array<int>();
        foreach(int i in IdCardsRemaining)
        {
           res.Add(i); 
        }
        GD.Print("Send : ",res,"serv : ",Networking.Instance.Multiplayer.GetUniqueId());
        return res;
    }*/

    public void RemoveCard(int CardId,long MyId)
    {
        if(MyId!=1 && IdCardsRemaining.Contains(CardId)) //Serveur already removed it
        {
            IdCardsRemaining.Remove(CardId);
        }
        EmitSignal(SignalName.CardRemoved,CardId);
    }

    public void AddCard(int CardId, long MyId)
    {
        if (MyId != 1)
        {
            IdCardsRemaining.Add(CardId);
        }
        //IdCards.Add(CardId);
        EmitSignal(SignalName.CardAdded,CardId);
    }

    public void AddCardServ(int CardId,long PeerId)//Only adds to hand
    {
        Networking.Instance.RpcId(PeerId,nameof(Networking.Instance.AddCard),CardId);
    }

    public Godot.Collections.Array<int> PlayCard(int IdCard,Deck deck,long PeerId)
    {
        Godot.Collections.Array<int> resultat = new Godot.Collections.Array<int>();
        if (IdCardsRemaining.Contains(IdCard))
        {
            resultat.Add(IdCard);
            IdCardsRemaining.Remove(IdCard);
            Networking.Instance.RpcId(PeerId,nameof(Networking.Instance.RemoveCard),IdCard);
            if (IdCard == 0)
            {
                if (deck.IsBombIn(3))
                {
                    resultat.Add(1);
                }
                else
                {
                    resultat.Add(0);
                }
            }
            if (IdCard == 1)
            {
                if (deck.IsBombIn(2))
                {
                    resultat.Add(1);
                }
                else
                {
                    resultat.Add(0);
                }
            }
            return resultat;
        }
        return null;

    }

}