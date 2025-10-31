using Godot;
using System;

public partial class StaminaDrainArea : Area2D
{
	[Export(PropertyHint.Range, "1.0, 50.0, 0.5")]
	public float StaminaDrainRate = 15.0f;
	private Erika _playerInside = null;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (_playerInside != null)
		{
			_playerInside.DrainStamina(StaminaDrainRate * (float)delta);
		}
	}
	private void OnBodyEntered(Node2D body)
	{
		if (body is Erika erika)
		{
			_playerInside = erika;
		}
	}
	private void OnBodyExited(Node2D body)
	{
		if (body is Erika erika && _playerInside == erika)
		{
			_playerInside = null;
		}
	}
}
