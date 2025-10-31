using Godot;
using System;

public partial class TelaMorte : Node2D
{
	public override void _Ready()
	{
		var Btn = GetNode<TextureButton>("Control/reset_btn");
		Btn.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://node_2d.tscn");
		};
		var Btn2 = GetNode<TextureButton>("Control/voltar_btn");
		Btn2.Pressed += () =>
		{
			GetTree().ChangeSceneToFile("res://TelaInicial/title_screen.tscn");
		};
	}
}
