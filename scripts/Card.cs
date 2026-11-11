using Godot;
using System;

public partial class Card : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export]Label TextLabel;
	[Signal]public delegate void OnHoverEventHandler(int Idx);
	[Signal]public delegate void OnStopHoverEventHandler(int Idx);
	[Signal]public delegate void OnClickEventHandler(int Idx);
	public int IdxList;
	public int Id;
	enum State {Normal,Played,Hover,Destroy};
	State CurrentState = State.Normal;
	public override void _Ready()
	{
		PivotOffset = Size/2;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public override void _Input(InputEvent @event)
	{

		if(@event is InputEventMouseButton eventMouseButton){

			if(eventMouseButton.IsPressed() && eventMouseButton.ButtonIndex == MouseButton.Left && CurrentState == State.Hover){
				if(GlobalPosition.X <= eventMouseButton.Position.X && GlobalPosition.X+Size.X >= eventMouseButton.Position.X)
				{
					if(GlobalPosition.Y <= eventMouseButton.Position.Y && GlobalPosition.Y+Size.Y >= eventMouseButton.Position.Y)
					{
						CurrentState = State.Played;
						EmitSignal(SignalName.OnClick,IdxList);
					}
				}
			}
		}

		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			if(CurrentState == State.Normal)
			{
				if(GlobalPosition.X <= eventMouseMotion.Position.X && GlobalPosition.X+Size.X >= eventMouseMotion.Position.X)
				{
					if(GlobalPosition.Y <= eventMouseMotion.Position.Y && GlobalPosition.Y+Size.Y >= eventMouseMotion.Position.Y)
					{
						EmitSignal(SignalName.OnHover,IdxList);
					}
				}
			}
			if(CurrentState == State.Hover)
			{
				if(GlobalPosition.X > eventMouseMotion.Position.X || GlobalPosition.X+Size.X < eventMouseMotion.Position.X || GlobalPosition.Y > eventMouseMotion.Position.Y || GlobalPosition.Y+Size.Y < eventMouseMotion.Position.Y)
				{
					EmitSignal(SignalName.OnStopHover,IdxList);
				}
			}
		}
	}

	public void Init(int _Id,String Text,int _IdxList)
	{
		Id = _Id;
		TextLabel.Text = Text;
		IdxList = _IdxList;
	}

	public bool IsHover()
	{
		return CurrentState == State.Hover;
	}

	public void SetHover()
	{
		CurrentState = State.Hover;
	}
	public void SetNormal()
	{
		CurrentState = State.Normal;
	}

	public void Destroy()
	{
		Tween tween = CreateTween();
		Vector2 PositionDie = Position;
		PositionDie.Y -= 100;
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Expo);

		tween.TweenProperty(this,"position",PositionDie,1.0f);
		tween.Parallel().TweenProperty(this,"modulate",Colors.Transparent,1.0f);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
