using Godot;
using System;

public partial class AddValue : Label
{
	// Called when the node enters the scene tree for the first time.
	[Export] AnimationPlayer anim;
	
	public override void _Ready()
	{
		anim.AnimationFinished += Destroy;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		this.PivotOffset = this.Size/2; //To always scale with the good center
	}

	public void Init(int Value)
	{
		if (Value > 0)
		{
			this.Text = "+" + Value.ToString();
		}
		else
		{
			this.Text = "-" + (-Value).ToString();
		}

		anim.Play("Start");
	}

	private void Destroy(StringName animName)
	{
		QueueFree();
	}
}
