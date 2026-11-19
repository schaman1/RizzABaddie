using Godot;

public partial class PlayerCharacter : CharacterBody2D
{
	public const float Speed = 200.0f;
	[Export]Camera2D camera;
	[Export]public PlayerContainer Container;
	[Export]PackedScene CardDeckPackedScene;

	public enum State {LookingForBaddie,InCardGame}
	public State CurrentState = State.LookingForBaddie;

	public int Id = 0;
	private Vector2 InitPos;

    public override void _EnterTree()
	{
		Id = Name.ToString().ToInt();
		SetMultiplayerAuthority(Name.ToString().ToInt(),true); 
		if (!IsMultiplayerAuthority())
		{
			camera.Enabled = false;//disable the camera
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if(!IsMultiplayerAuthority() || CurrentState == State.InCardGame){return;}//No move

		Vector2 velocity = Velocity;

		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity = direction * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed); //To complete the mouv
			velocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public bool IsLookingForBaddie()
	{
		return CurrentState == State.LookingForBaddie;
	}

	public void SetInCardGame(Vector2 NewSelfPosition)
	{
		GlobalPosition = NewSelfPosition;
		camera.Enabled = false;//During game, use the cardgameCamera
		CurrentState = State.InCardGame;
	}

	public void Reset()
	{
		Container.Reset();
		if(IsMultiplayerAuthority())
		{
			camera.Enabled = true;
			camera.MakeCurrent();
		}
		CurrentState = State.LookingForBaddie;
	}

	public void MoveToCenterToShowCard(PlayArea CardPlayArea)
	{
		InitPos = GlobalPosition;
		Tween NewTween = CreateTween();
		NewTween.TweenProperty(this,"global_position",CardPlayArea.GlobalPosition,1);
	}
	public void MoveAfterCardShow(PlayArea CardPlayArea,int CardId)
	{
		if (CardId == 0) //Means bomb
		{
			Tween NewTween = CreateTween();
			NewTween.TweenMethod(Callable.From<float>(t => SetNewPosWhenLeave(t,CardPlayArea)),0.0f,1.0f,2);
		}
		else //Return to initPosition
		{
			Tween NewTween = CreateTween();
			NewTween.TweenProperty(this,"global_position",InitPos,1);
		}
	}
	public void SetNewPosWhenLeave(float RatioPosition,PlayArea CardPlayArea)
	{
		CardPlayArea.PathFollowToLeave.ProgressRatio = RatioPosition;
		GlobalPosition = CardPlayArea.PathFollowToLeave.GlobalPosition;
	}

	public void UpdateLove(int NewLove)
	{
		Container.Love = NewLove;
		Container.DisplayValues();
	}

	/*public void PlayCard(Player PlayerInfo)
	{
		if(!IsMultiplayerAuthority()){
			Container.SetNewLove(-PlayerInfo.CardPower); // - bcs bomb deals damage, no heal
			return;
		}
		//Draw the current action of the player
		// {
		InitPos = GlobalPosition;
		Tween NewTween = CreateTween();
		//NewTween.TweenProperty(this,"global_position",CardPlayArea.GlobalPosition,1);
		if(PlayerInfo.Action == (int)Actions.Draw || PlayerInfo.CardIdDraw == 0)
		{ //Bomb bcs with bomb, action = quit but actually has draw.
			NewTween.TweenCallback(Callable.From(() => ShowCardDraw(PlayerInfo.CardIdDraw,PlayerInfo.CardPower)));
			PlayerReactToCard(PlayerInfo.CardIdDraw,PlayerInfo.CardPower);
		}
		else
		{
			NewTween.TweenCallback(Callable.From(() => PlayerReactToCard(-1,-1)));
		}
		//}
		// else if(PlayerInfo.Action == (int)Actions.Quit)
		// {
		// 	LabelDeck.Text = "Quit";
		// }
	}

	public void PlayEffectCard(long CardId,int CardPower)
	{
		Container.SetNewLove(-CardPower); // - bcs bomb deals damage, no heal
		AddValue Add = (AddValue)AddValuePackedScene.Instantiate();
		Add.Init(CardPower);
		Container.AddChild(Add);
	}

	public void ShowCardDraw(long CardId,int CardPower)
	{
		CardDeck ShowCard = (CardDeck)CardDeckPackedScene.Instantiate();
		ShowCard.Name = Name;
		ShowCard.InitCard(CardId,CardPower);
		ShowCard.GlobalPosition = Vector2.Zero;

		AddChild(ShowCard);
	}

	public void PlayerReactToCard(long CardId,int CardPower)
	{
		//CardPlayArea.PathFollowToLeave.ProgressRatio = 0; //Reset the pathfollow
		Tween NewTween = CreateTween();
		if(CardId == 0 || CardId==-1)//Means Bomb/Quit
		{
			//NewTween.TweenMethod(Callable.From<float>(t => SetNewPosWhenLeave(t,CardPlayArea)),0.0f,1.0f,2);
		}
		else
		{
			NewTween.TweenProperty(this,"global_position",InitPos,1);
		}
	}*/

}
