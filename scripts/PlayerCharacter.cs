using Godot;
using System;

public partial class PlayerCharacter : CharacterBody2D
{
	public const float Speed = 200.0f;
	//public const float JumpVelocity = -400.0f; //Dash
	[Export]Camera2D camera;
	[Export]public PlayerContainer Container;

	public enum State {LookingForBaddie,InCardGame}
	public State CurrentState = State.LookingForBaddie;

	public int Id = 0;

    public override void _EnterTree()
	{
		Id = Name.ToString().ToInt();
		SetMultiplayerAuthority(Name.ToString().ToInt());
		if (!IsMultiplayerAuthority())
		{
			camera.Enabled = false;//disable the camera
		}
	}


	public override void _PhysicsProcess(double delta)
	{
		if(!IsMultiplayerAuthority() || CurrentState == State.InCardGame){return;}//No move

		Vector2 velocity = Velocity;

		// No gravity
		// if (!IsOnFloor())
		// {
		// 	velocity += GetGravity() * (float)delta;
		// }

		// Handle Jump. //No jump, implement dash instead
		// if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		// {
		// 	velocity.Y = JumpVelocity;
		// }

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
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
}
