using Godot;
using System;
using System.Security.Cryptography.X509Certificates;

public partial class AnimPlayerFoundBaddie : Control
{
	// Called when the node enters the scene tree for the first time.
	[Export]RichTextLabel text;
	[Export]Sprite2D sprite;
	[Export]PathFollow2D path;
	[Export]Timer time;
	[Export]TextureRect Texture;

	int PositionI = 0;

	public override void _Ready()
	{
		time.Timeout += StartAnim;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void StartAnimationPlayerFoundBaddie(String PlayerName,Vector2 NewPlayerPosition,System.Action<Vector2> ChangePlayerPos,System.Action SetNewCamera, System.Action ShowCardContainer)
	{
		text.Text = PlayerName + " found the girl !";
		time.Start();
		Tween newTween = CreateTween();
		newTween.TweenProperty(Texture,"modulate",new Color(0,0,0,1),time.WaitTime + 2);
		newTween.TweenCallback(Callable.From(() => ChangePlayerPos(NewPlayerPosition)));
		newTween.TweenCallback(Callable.From(SetNewCamera));
		newTween.TweenCallback(Callable.From(ShowCardContainer));
		newTween.TweenProperty(Texture,"modulate",new Color(0,0,0,0),0.3);
	}
	public void StartAnim(){
		Show();
		
		Tween newTween = CreateTween();
		path.Loop = false;
		newTween.TweenProperty(path,"progress_ratio",0.45,1).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		newTween.TweenProperty(path,"progress_ratio",0.55,1).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.InOut);
		newTween.TweenProperty(path,"progress_ratio",1,0.3).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
		newTween.TweenCallback(Callable.From(Reset));
		// tweenShowHide.SetTrans(Tween.TransitionType.Expo);
		// tweenShowHide.SetEase(Tween.EaseType.Out);
		// tweenShowHide.TweenProperty(this,"position",NewPosition,1.0f);
	}

	public void Reset()
	{
		Hide();
		path.ProgressRatio = 0;
	}
}
