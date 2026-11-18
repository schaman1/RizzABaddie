using Godot;
using System;

public partial class CardDeck : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export]AnimationPlayer anim;
	[Export]ColorRect color;
	[Export]Label CardPowerLabel;
	[Signal]public delegate void OnAnimFinishEventHandler();

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(Name.ToString().ToInt());
    }

	public override void _Ready()
	{
		if (IsMultiplayerAuthority())
		{
			//PivotOffset = Size/2; //Center the offset
			anim.AnimationFinished += AnimationFinished;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void InitCard(long CardId,int CardPower)
	{
		CardPowerLabel.Text = CardPower.ToString();
		anim.Play("Enter");
	}

	public void AnimationFinished(StringName AnimationName)
	{
		if(AnimationName == "Enter")
		{
			EmitSignal(SignalName.OnAnimFinish);
			anim.Play("Exit");
		}
		else
		{
			QueueFree(); //Destroy
		}
	}
}
