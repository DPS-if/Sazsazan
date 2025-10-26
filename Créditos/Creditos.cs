using Godot;
using System;

public partial class Creditos : Node2D
{
	public override void _Ready()
	{
		var Btn = GetNode<Button>("créditos/voltar_btn");
		Btn.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://TelaInicial/title_screen.tscn");
		};
	}
} 
