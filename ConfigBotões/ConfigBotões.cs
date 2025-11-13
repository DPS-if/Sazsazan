using Godot;
using System;

public partial class ConfigBotões : CanvasLayer
{
	public override void _Ready()
	{
		var q_btn = GetNode<TextureButton>("TextureRect/q_btn");
		q_btn.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://Menudepausa/MenuPause.tscn");
		};
	}
}
