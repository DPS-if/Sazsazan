using Godot;
using System;

public partial class Inventario : CanvasLayer
{
	public override void _Ready()
	{
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;
		SetProcessInput(true);
	}
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.E)
		{
			Visible = !Visible;
			GetTree().Paused = Visible;
		}
	}
}
