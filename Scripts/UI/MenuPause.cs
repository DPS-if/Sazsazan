using Godot;
using System;

public partial class MenuPause : CanvasLayer
{
	// Avisa o dono (a Erika) que o jogador quer abrir as configurações.
	// Assim o ConfigBotões aparece como camada sobre o jogo, em vez de
	// trocar de cena e perder o progresso da fase.
	[Signal]
	public delegate void ConfigRequestedEventHandler();

	public override void _Ready()
	{
		Visible = false;
		var resume_btn = GetNode<TextureButton>("Panel/resume_btn");
		var quit_btn = GetNode<TextureButton>("Panel/quit_btn");
		var butão = GetNode<Button>("Panel/butão");
		resume_btn.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		quit_btn.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		butão.ProcessMode = Node.ProcessModeEnum.WhenPaused;
		resume_btn.Pressed += () =>
		{
			GetTree().Paused = false;
			Visible = false;
		};
		quit_btn.Pressed += () =>
		{
			GetTree().Paused = false;
			// Limpa modificações de cheats antes de sair da partida
		Global.Instance.ResetCheats();
		SceneTransitions.Instance.ChangeScene("res://Cenas/UI/title_screen.tscn");
		};
		butão.Pressed += () =>
		{
			// NÃO troca de cena: apenas avisa quem nos instanciou
			EmitSignal(SignalName.ConfigRequested);
		};
	}
}
