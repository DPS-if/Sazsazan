using Godot;
using System;

public partial class ConfigBotões : CanvasLayer
{
	// Avisa que o jogador quer voltar para o menu de pausa
	[Signal]
	public delegate void VoltarPressedEventHandler();

	public override void _Ready()
	{
		var q_btn = GetNode<TextureButton>("TextureRect/q_btn");
		// O jogo fica pausado enquanto esta camada está visível,
		// então o botão precisa processar input mesmo pausado
		q_btn.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		q_btn.Pressed += () =>
		{
			EmitSignal(SignalName.VoltarPressed);
		};
	}

	// Mostra a camada com uma transição suave de opacidade
	public void ShowWithFade()
	{
		Visible = true;

		var rect = GetNode<Control>("TextureRect");
		rect.Modulate = new Color(1, 1, 1, 0);

		Tween tween = CreateTween();
		tween.TweenProperty(rect, "modulate:a", 1.0f, 0.25f);
	}
}
