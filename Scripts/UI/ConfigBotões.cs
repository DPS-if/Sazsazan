using Godot;
using System.Collections.Generic;

public partial class ConfigBotões : CanvasLayer
{
	// Avisa que o jogador quer voltar para o menu de pausa
	[Signal]
	public delegate void VoltarPressedEventHandler();

	// Nomes amigáveis exibidos ao lado de cada ação remapeável
	private static readonly string[] NomesAções =
	{
		"Andar para a esquerda",
		"Andar para a direita",
		"Pular / Pulo Duplo",
		"Dash (impulso)",
		"Correr",
	};

	private readonly Dictionary<string, Button> _botõesDeAção = new Dictionary<string, Button>();
	private string _açãoAguardandoTecla = null;
	private Control _raiz;
	private Tween _tweenTransição;

	public override void _Ready()
	{
		// O jogo fica pausado enquanto esta camada está visível;
		// WhenPaused mantém a camada (e seus filhos) funcionando na pausa
		ProcessMode = Node.ProcessModeEnum.WhenPaused;

		CriarPainelDeTeclas();
	}

	// Mostra a caixa com uma transição suave de opacidade
	public void ShowWithFade()
	{
		Visible = true;
		_açãoAguardandoTecla = null;
		AtualizarTextosDasTeclas();

		if (_tweenTransição != null && _tweenTransição.IsValid()) { _tweenTransição.Kill(); }
		_raiz.Modulate = new Color(1f, 1f, 1f, 0f);
		_tweenTransição = CreateTween();
		_tweenTransição.TweenProperty(_raiz, "modulate:a", 1.0f, 0.2f);
	}

	// Volta para o menu de pausa, com fade de saída antes de sinalizar
	private void Voltar()
	{
		_açãoAguardandoTecla = null;
		if (_tweenTransição != null && _tweenTransição.IsValid()) { _tweenTransição.Kill(); }
		_tweenTransição = CreateTween();
		_tweenTransição.TweenProperty(_raiz, "modulate:a", 0.0f, 0.15f);
		_tweenTransição.Finished += () =>
		{
			Visible = false;
			EmitSignal(SignalName.VoltarPressed);
		};
	}

	// -------------------------------------------------------------------
	// REMAPEAMENTO DE TECLAS (painel construído em código)
	// -------------------------------------------------------------------
	private void CriarPainelDeTeclas()
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
			ContentMarginLeft = 24,
			ContentMarginRight = 24,
			ContentMarginTop = 20,
			ContentMarginBottom = 20,
		};
		painel.AddThemeStyleboxOverride("panel", estilo);
		painel.CustomMinimumSize = new Vector2(420, 0);
		centro.AddChild(painel);

		VBoxContainer vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		painel.AddChild(vbox);

		Label título = new Label
		{
			Text = "CONTROLES",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		título.AddThemeFontSizeOverride("font_size", 24);
		título.AddThemeColorOverride("font_color", Colors.White);
		vbox.AddChild(título);

		for (int i = 0; i < Global.AçõesRemapeáveis.Length; i++)
		{
			string ação = Global.AçõesRemapeáveis[i];

			HBoxContainer linha = new HBoxContainer();
			linha.AddThemeConstantOverride("separation", 10);

			Label nome = new Label { Text = NomesAções[i], CustomMinimumSize = new Vector2(190, 0) };
			nome.AddThemeFontSizeOverride("font_size", 16);
			nome.AddThemeColorOverride("font_color", Colors.White);
			linha.AddChild(nome);

			Button botão = new Button
			{
				CustomMinimumSize = new Vector2(150, 34),
				Text = Global.Instance.GetActionKeyText(ação),
			};
			string açãoCapturada = ação; // cópia para o closure
			botão.Pressed += () => ComeçarAguardandoTecla(açãoCapturada, botão);
			_botõesDeAção[ação] = botão;
			linha.AddChild(botão);

			vbox.AddChild(linha);
		}

		// --- Seção VÍDEO: tela cheia e resolução (salvas no settings.cfg) ---
		Label títuloVídeo = new Label
		{
			Text = "VÍDEO",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		títuloVídeo.AddThemeFontSizeOverride("font_size", 18);
		títuloVídeo.AddThemeColorOverride("font_color", Colors.White);
		vbox.AddChild(títuloVídeo);

		HBoxContainer linhaTelaCheia = new HBoxContainer();
		linhaTelaCheia.AddThemeConstantOverride("separation", 10);
		Label rótuloTela = new Label { Text = "Modo de exibição", CustomMinimumSize = new Vector2(190, 0) };
		rótuloTela.AddThemeFontSizeOverride("font_size", 16);
		rótuloTela.AddThemeColorOverride("font_color", Colors.White);
		linhaTelaCheia.AddChild(rótuloTela);
		CheckButton telaCheia = new CheckButton
		{
			Text = "Tela cheia",
			ButtonPressed = Global.Instance.IsFullscreen,
		};
		telaCheia.AddThemeColorOverride("font_color", Colors.White);
		telaCheia.Toggled += ligado => Global.Instance.SetFullscreen(ligado);
		linhaTelaCheia.AddChild(telaCheia);
		vbox.AddChild(linhaTelaCheia);

		HBoxContainer linhaResolução = new HBoxContainer();
		linhaResolução.AddThemeConstantOverride("separation", 10);
		Label rótuloResolução = new Label { Text = "Resolução", CustomMinimumSize = new Vector2(190, 0) };
		rótuloResolução.AddThemeFontSizeOverride("font_size", 16);
		rótuloResolução.AddThemeColorOverride("font_color", Colors.White);
		linhaResolução.AddChild(rótuloResolução);
		OptionButton seletorResolução = new OptionButton { CustomMinimumSize = new Vector2(150, 34) };
		int índiceAtual = 0;
		for (int i = 0; i < Global.ResoluçõesDisponíveis.Length; i++)
		{
			Vector2I r = Global.ResoluçõesDisponíveis[i];
			seletorResolução.AddItem($"{r.X} x {r.Y}");
			if (r == Global.Instance.ResoluçãoJanela) { índiceAtual = i; }
		}
		seletorResolução.Select(índiceAtual);
		seletorResolução.ItemSelected += índice =>
			Global.Instance.SetResolution(Global.ResoluçõesDisponíveis[(int)índice]);
		linhaResolução.AddChild(seletorResolução);
		vbox.AddChild(linhaResolução);

		Label ajuda = new Label
		{
			Text = "Clique em uma tecla e depois aperte a nova tecla.\nESC cancela.",
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		ajuda.AddThemeFontSizeOverride("font_size", 12);
		ajuda.AddThemeColorOverride("font_color", new Color(0.75f, 0.75f, 0.75f));
		vbox.AddChild(ajuda);

		// Botão de voltar agora faz parte da caixa (a tela antiga foi removida)
		Button voltar = new Button
		{
			Text = "VOLTAR",
			CustomMinimumSize = new Vector2(0, 40),
		};
		voltar.Pressed += Voltar;
		vbox.AddChild(voltar);
	}

	private void ComeçarAguardandoTecla(string ação, Button botão)
	{
		_açãoAguardandoTecla = ação;
		botão.Text = "Aperte uma tecla...";
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (_açãoAguardandoTecla == null) { return; }

		if (@event is InputEventKey tecla && tecla.Pressed)
		{
			GetViewport().SetInputAsHandled();

			Key novaTecla = tecla.PhysicalKeycode != Key.None ? tecla.PhysicalKeycode : tecla.Keycode;

			// ESC cancela o remapeamento
			if (novaTecla == Key.Escape)
			{
				_açãoAguardandoTecla = null;
				AtualizarTextosDasTeclas();
				return;
			}

			Global.Instance.SetActionKey(_açãoAguardandoTecla, novaTecla);
			_açãoAguardandoTecla = null;
			AtualizarTextosDasTeclas();
		}
	}

	private void AtualizarTextosDasTeclas()
	{
		foreach (KeyValuePair<string, Button> par in _botõesDeAção)
		{
			par.Value.Text = Global.Instance.GetActionKeyText(par.Key);
		}
	}
}

