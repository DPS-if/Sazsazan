using Godot;
using System;

public partial class TelaMorte : Node2D
{
	public override void _Ready()
	{
		var Btn = GetNode<TextureButton>("Control/reset_btn");
		Btn.Pressed += () =>
		{
			// Volta para a última fase em que a Erika morreu
			// (registrada pelo autoload SceneTransitions quando ela morreu)
			SceneTransitions.Instance.ChangeScene(SceneTransitions.LastLevelPath);
		};
		var Btn2 = GetNode<TextureButton>("Control/voltar_btn");
		Btn2.Pressed += () =>
		{
			SceneTransitions.Instance.ChangeScene("res://Cenas/UI/title_screen.tscn");
		};
	}
}
