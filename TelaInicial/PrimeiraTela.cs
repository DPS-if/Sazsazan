using Godot;
using System;

public partial class PrimeiraTela : Control
{
	public override void _Ready()
	{
		var quitBtn = GetNode<TextureButton>("MarginContainer/HBoxContainer/VBoxContainer/quit_btn");
		quitBtn.Pressed += () =>
		{
			GetTree().Quit();
		};
		var creditsBtn = GetNode<TextureButton>("MarginContainer/HBoxContainer/VBoxContainer/credits_btn");
		creditsBtn.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://Créditos/créditos.tscn");
		};
		var Btn2 = GetNode<TextureButton>("MarginContainer/HBoxContainer/VBoxContainer/start_btn");
		Btn2.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://CutscenePlayer.tscn");
		};
	}
}
