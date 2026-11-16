using Godot;
using System;

/// <summary>
/// Menu de pausa centralizado, com a mesma estética dos painéis de
/// configurações e cheats: fundo escuro arredondado, no meio da tela,
/// transição de fade de entrada e saída.
///
/// Sinais:
///   - ConfigRequested: o jogador quer abrir as configurações de controle/vídeo.
/// </summary>
public partial class MenuPause : CanvasLayer
{
	[Signal]
	public delegate void ConfigRequestedEventHandler();

	private Control _raiz;
	private Tween _tweenTransição;

	public override void _Ready()
	{
		// Funciona mesmo com o jogo pausado
		ProcessMode = Node.ProcessModeEnum.WhenPaused;

		CriarPainelDePausa();
	}

	/// <summary>
	/// Mostra o menu com fade de entrada.
	/// </summary>
	public void ShowWithFade()
	{
		Visible = true;

		if (_tweenTransição != null && _tweenTransição.IsValid()) { _tweenTransição.Kill(); }
		_raiz.Modulate = new Color(1f, 1f, 1f, 0f);
		_tweenTransição = CreateTween();
		_tweenTransição.TweenProperty(_raiz, "modulate:a", 1.0f, 0.2f);
	}

	/// <summary>
	/// Fecha o menu com fade de saída e retoma o jogo.
	/// </summary>
	private void RetomarJogo()
	{
		if (_tweenTransição != null && _tweenTransição.IsValid()) { _tweenTransição.Kill(); }
		_tweenTransição = CreateTween();
		_tweenTransição.TweenProperty(_raiz, "modulate:a", 0.0f, 0.15f);
		_tweenTransição.Finished += () =>
		{
			Visible = false;
			GetTree().Paused = false;
		};
	}

	/// <summary>
	/// Abre a tela de configurações (sinal para o dono instanciar ConfigBotões).
	/// </summary>
	private void AbrirConfiguracoes()
	{
		if (_tweenTransição != null && _tweenTransição.IsValid()) { _tweenTransição.Kill(); }
		_tweenTransição = CreateTween();
		_tweenTransição.TweenProperty(_raiz, "modulate:a", 0.0f, 0.15f);
		_tweenTransição.Finished += () =>
		{
			Visible = false;
			EmitSignal(SignalName.ConfigRequested);
		};
	}

	/// <summary>
	/// Volta para a tela inicial (limpa cheats antes).
	/// </summary>
	private void VoltarAoTitulo()
	{
		if (_tweenTransição != null && _tweenTransição.IsValid()) { _tweenTransição.Kill(); }
		_tweenTransição = CreateTween();
		_tweenTransição.TweenProperty(_raiz, "modulate:a", 0.0f, 0.15f);
		_tweenTransição.Finished += () =>
		{
			GetTree().Paused = false;
			Visible = false;
			Global.Instance.ResetCheats();
			SceneTransitions.Instance.ChangeScene("res://Cenas/UI/title_screen.tscn");
		};
	}

	// -------------------------------------------------------------------
	// PAINEL DE PAUSA (mesma estética dos painéis de controles e cheats)
	// -------------------------------------------------------------------
	private void CriarPainelDePausa()
	{
		// Raiz cobrindo a tela inteira: a caixa fica no meio da câmera/viewport
		_raiz = new Control
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		_raiz.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(_raiz);

		CenterContainer centro = new CenterContainer
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		centro.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_raiz.AddChild(centro);

		PanelContainer painel = new PanelContainer();
		StyleBoxFlat estilo = new StyleBoxFlat
		{
			BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f),
			CornerRadiusTopLeft = 12,
			CornerRadiusTopRight = 12,
			CornerRadiusBottomLeft = 12,
			CornerRadiusBottomRight = 12,
			ContentMarginLeft = 28,
			ContentMarginRight = 28,
			ContentMarginTop = 24,
			ContentMarginBottom = 24,
		};
		painel.AddThemeStyleboxOverride("panel", estilo);
		painel.CustomMinimumSize = new Vector2(460, 0);
		centro.AddChild(painel);

		VBoxContainer vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 14);
		painel.AddChild(vbox);

		Label título = new Label
		{
			Text = "PAUSA",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		título.AddThemeFontSizeOverride("font_size", 28);
		título.AddThemeColorOverride("font_color", Colors.White);
		vbox.AddChild(título);

		// --- Botão Resume ---
		Button btnResume = new Button
		{
			Text = "RESUMIR",
			CustomMinimumSize = new Vector2(0, 42),
		};
		btnResume.Pressed += RetomarJogo;
		vbox.AddChild(btnResume);

		// --- Botão Configurações ---
		Button btnConfig = new Button
		{
			Text = "CONFIGURAÇÕES",
			CustomMinimumSize = new Vector2(0, 42),
		};
		btnConfig.Pressed += AbrirConfiguracoes;
		vbox.AddChild(btnConfig);

		// --- Botão Sair (volta ao título) ---
		Button btnSair = new Button
		{
			Text = "SAIR PARA O TÍTULO",
			CustomMinimumSize = new Vector2(0, 42),
		};
		btnSair.Pressed += VoltarAoTitulo;
		vbox.AddChild(btnSair);

		Label ajuda = new Label
		{
			Text = "Pressione ESC para fechar",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		ajuda.AddThemeFontSizeOverride("font_size", 12);
		ajuda.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
		vbox.AddChild(ajuda);
	}
}
