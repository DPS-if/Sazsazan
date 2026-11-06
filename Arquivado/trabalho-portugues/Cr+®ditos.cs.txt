using Godot;
using System;

public partial class Créditos : Node2D
{
	public override void _Ready()
	{
		var Btn = GetNode<Button>("Control/voltar_btn");
		Btn.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://title_screen.tscn");
		};
	}
}
