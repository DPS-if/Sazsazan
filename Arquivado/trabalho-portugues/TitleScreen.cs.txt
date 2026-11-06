using Godot;
using System;

public partial class TitleScreen : Control
{
	public override void _Ready()
	{
		var quitBtn = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/quit_btn");
		quitBtn.Pressed += () =>
		{
			GetTree().Quit();
		};
		var creditsBtn = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/credits_btn");
		creditsBtn.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://créditos.tscn");
		};
	}
} 
