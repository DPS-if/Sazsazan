using Godot;
using System;

public partial class MenuPause : CanvasLayer
{
	public override void _Ready()
	{
		Visible = false;
		var resume_btn = GetNode<Button>("Panel/resume_btn");
		var quit_btn = GetNode<Button>("Panel/quit_btn");
		resume_btn.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		quit_btn.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		resume_btn.Pressed += () =>
		{
			GetTree().Paused = false;
			Visible = false;
		};
		quit_btn.Pressed += () =>
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile("res://TelaInicial/title_screen.tscn");
		};
	}
}
