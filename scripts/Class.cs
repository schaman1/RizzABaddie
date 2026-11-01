using Godot;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
enum Actions {Draw=0,Quit=1,Nothing=-1}; //0 = Draw / 1 = Quit
public enum State {InManche=0,FinishDisplayManche=1}; //Only use when end manche, wait for eveyrone one to be ready to restart

public partial class CardInfo : Node
{
    public int Id {get;set;}
    public int Power {get;set;}
    public string Title {get;set;}
    public CardInfo(int Id,int Power,string Title)
    {
        this.Id = Id;
        this.Power = Power;
        this.Title = Title;
    }

}

public partial class PlayersInfoClass : Node
{
    
    public Dictionary<long,Player> PlayerDic = new Dictionary<long,Player>();
    public List<Player> Winners = new List<Player>();
    private int DefaultLove = 0;

    public Player GetPlayer(int Id)
    {
        return PlayerDic[Id];
    }

    public Player AddPlayer(long _id,string _name)
    {
        Player NewPlayer = new Player(_id,_name,DefaultLove,0,-1);
        PlayerDic[_id] = NewPlayer;
        return NewPlayer;
    }

    public bool IsPseudoTaken(String Pseudo)
    {
        foreach(Player player in PlayerDic.Values)
        {
            if (player.Pseudo == Pseudo)
            {
                return true;
            }
        }
        return false;
    }

    public void SetPeerAction(int Id,int Action)
    {
        Player player = GetPlayer(Id);
        if (player!= null)
        {
            player.Action = Action;
            player.ReceivedAction = true;
        }
    }

    public bool HasAllPeerPlay()
    {
        foreach(Player player in PlayerDic.Values){

            if(player.ReceivedAction == false)
            {
                return false;
            }
        }
        return true;
    }

    public bool IsEndManche()
    {   
        foreach(Player player in PlayerDic.Values){
            if(player.Action!= (int)Actions.Quit) //Quit or draw bomb
            {
                return false;
            }
        }
        return true;
    }

    public Godot.Collections.Dictionary<long,Godot.Collections.Array<int>> CompressDatasPlayer()
    {
        Godot.Collections.Dictionary<long,Godot.Collections.Array<int>> CompactDatas = new Godot.Collections.Dictionary<long,Godot.Collections.Array<int>>();

        foreach(Player player in PlayerDic.Values)
        {
            CompactDatas[player.Id] = [player.Love,player.Power,player.Action,player.CardIdDraw,player.CardPower];
        } 
        return CompactDatas;
    }

    public void ExpandDatasPlayer(Godot.Collections.Dictionary<long,Godot.Collections.Array<int>> datas)
    {
        foreach(long Id in datas.Keys)
        {
            if (PlayerDic.ContainsKey(Id))
            {
                PlayerDic[Id].SetValues(datas[Id]);
            }
            //Never create other player as all already created
        }
    }

    public Godot.Collections.Dictionary<long,String> ReturnExistingPlayers() //Return existing players
    {
        Godot.Collections.Dictionary<long,String> Datas = new Godot.Collections.Dictionary<long,String>();
        foreach(long Id in PlayerDic.Keys)
        {
            Datas[Id] = PlayerDic[Id].Pseudo;
        }
        return Datas;
    }

    public void DrawCardForAllPeer()
    {
        foreach(Player player in PlayerDic.Values)
        {
            if(player.Action == (int)Actions.Quit)
            {
                player.CardPower = 0;
                player.CardIdDraw = -1; //No card draw
            }
            else
            {
                CardInfo CardDraw = player.MyDeck.DrawCard();
                player.CardPower = CardDraw.Power;
                player.CardIdDraw = CardDraw.Id;
                if (player.CardIdDraw == 0) //Bomb
                {
                    player.Action = (int)Actions.Quit; //Do here for all !!! Do not do this in another place !!!
                }
            }
        }
    }

    public void EndRound() //Set values to restart a round
    {
        foreach(Player player in PlayerDic.Values)
        {
            player.ResetAction();

            player.Love += player.CardPower; //
        }
    }

    public void EndManche()
    {
        foreach(Player player in PlayerDic.Values)
        {
            player.EndManche();
            player.Love += player.CardPower; //Damage dealt by the bomb
        }
    }

    public void SetPlayerFInishDisplay(long PeerId) //End manche, wait for all player to display all to start next
    {
        PlayerDic[PeerId].CurrentState = State.FinishDisplayManche;
    }
    public bool HasAllPeerFinishManche()
    {
        foreach(Player player in PlayerDic.Values)
        {
            if (player.CurrentState != State.FinishDisplayManche)
            {
                return false;
            }
        }
        return true;
    }

    public void SetWinner()
    {
        int max = PlayerDic[1].Love;
        foreach(Player player in PlayerDic.Values)
        {
            if (player.Love >= max)
            {
                if (player.Love == max)
                {
                    Winners.Add(player);
                }
                else
                {
                    Winners.Clear();
                    Winners.Add(player);
                }
                max = player.Love;
            }
        }
    }
}

public partial class Player: Node
{
    public long Id = 0;
    public string Pseudo = "Pseudo";
    public int Love = 0;
    public int Power = 0; //Let's say power = gold so usefull to have it
    public int Action = -1;
    public int CardIdDraw = 0;
    public int CardPower = 0;
    public bool ReceivedAction = false;
    public State CurrentState = State.InManche;
    public Deck MyDeck = new Deck();

    public Player(long _id,string _name,int _Love, int _power, int _action)
    {
        Id = _id;
        Pseudo = _name;
        Love = _Love;
        Power = _power;
        Action = _action;

        if (Networking.Instance.IsHost)
        {
            MyDeck.InitDeck();
        }
    }

    public void SetValues(Godot.Collections.Array<int> datas)
    {
        Love = datas[0];
        Power = datas[1];
        Action = datas[2];
        CardIdDraw = datas[3];
        CardPower = datas[4];
    }

    public void ResetAction()
    {
        if(Action != (int)Actions.Quit)
        {
            //Action = (int)Actions.Nothing; // Don't reset as players use this to display the right thing ! 
            ReceivedAction = false; //Means need info from player
        }
    }
    public void StartManche(int IdxBomb)
    {
        MyDeck.StartManche(IdxBomb);
        Action = (int)Actions.Nothing; //Reset
        CurrentState = State.InManche;
        ReceivedAction = false;
        CardIdDraw = -1;
        CardPower = 0;
    }

    public void EndManche()
    {
        //Put all in startManche good ? Idk
    }

    public void Restart() //Call at each begin of game
    {
        MyDeck.Restart();
    }
}