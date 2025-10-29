using Godot;
using System;

public partial class Inventário : CanvasLayer
{
	public override void _Ready()
	{
		Visible = false;
		var Sair_btn = GetNode<TextureButton>("Control/sair_btn");
		Sair_btn.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		Sair_btn.Pressed += () =>
		{
			GetTree().Paused = false;
			Visible = false;
		};
	}
}
