using Godot;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;

public partial class PlayerContainer : Godot.MarginContainer
{
	// Called when the node enters the scene tree for the first time.
	[Export]Label LabelName;
	[Export]Label LabelLove;
	//[Export]Label LabelPower;
	//[Export]public Color ColorActive;
	//[Export]public Color ColorPassive;
	[Export]public Color DisconnectedColor;
	[Export]ColorRect HasPlayedColorRect;

	public string name = "Player";
	public int Love = 0;
	public int power = 0;
	//public bool Initiate = false;
	public override void _Ready()
	{
		HasPlayedColorRect.Visible = false;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void DisplayValues()
	{
		LabelLove.Text = Love.ToString();
		//LabelPower.Text = "Power = "+ power.ToString();
		LabelName.Text = name;
	}

	public void Init(long _Id,String _name,int _Love,int new_power = 0)
	{
		//Initiate = true;
		name = _name;
		Love = _Love;
		power = new_power;
		DisplayValues();
	}

	public void SetNewLove(int Damage)
	{
		Love -= Damage;
		DisplayValues();
	}

	public void SetColorDisconnected()
	{
		LabelName.Set("theme_override_colors/font_color",DisconnectedColor);
	}

	public void Active()
	{
		return;
	}
	public void Passive()
	{
		return;
	}

	public void Reset()
	{
		HasPlayedColorRect.Visible = false;
	}

	public void SetPlay()
	{
		HasPlayedColorRect.Visible = true;
	}

}
