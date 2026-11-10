using Godot;

/// <summary>
/// Janela de CHEATS para testes rápidos de level design.
/// Abre/fecha apertando C + ENTER ao mesmo tempo.
/// - Slider de multiplicador de velocidade da Erika (em tempo real)
/// - Modo Fantasma (noclip): sem colisão, voo livre com WASD/setas
/// - Slider de zoom da câmera para explorar o cenário
/// - Botão para desbloquear todas as habilidades de uma vez
/// </summary>
public partial class CheatConsole : CanvasLayer
{
	private HSlider _speedSlider;
	private Label _speedValue;
	private CheckButton _noclipToggle;
	private HSlider _zoomSlider;
	private Label _zoomValue;
	private Camera2D _camera;
	private Control _raiz;
	private bool _comboHeld = false;

	public override void _Ready()
	{
		Name = "CheatConsole";
		Layer = 50;
		Visible = false;
		// Funciona mesmo com o jogo pausado
		ProcessMode = Node.ProcessModeEnum.Always;

		// Raiz cobrindo a tela inteira: o painel fica no meio da câmera/viewport
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
			BgColor = new Color(0.05f, 0.05f, 0.10f, 0.95f),
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomLeft = 10,
			CornerRadiusBottomRight = 10,
			ContentMarginLeft = 18,
			ContentMarginRight = 18,
			ContentMarginTop = 12,
			ContentMarginBottom = 14,
		};
		painel.AddThemeStyleboxOverride("panel", estilo);
		centro.AddChild(painel);

		VBoxContainer vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 10);
		painel.AddChild(vbox);

		Label titulo = new Label { Text = "CHEATS", HorizontalAlignment = HorizontalAlignment.Center };
		titulo.AddThemeFontSizeOverride("font_size", 22);
		titulo.AddThemeColorOverride("font_color", Colors.White);
		vbox.AddChild(titulo);

		// --- Multiplicador de velocidade ---
		HBoxContainer linhaVelocidade = new HBoxContainer();
		linhaVelocidade.AddThemeConstantOverride("separation", 8);
		Label rotuloVelocidade = new Label { Text = "Velocidade" };
		rotuloVelocidade.AddThemeColorOverride("font_color", Colors.White);
		_speedSlider = new HSlider
		{
			MinValue = 0.25,
			MaxValue = 5.0,
			Step = 0.05,
			Value = Global.Instance.SpeedMultiplier,
			CustomMinimumSize = new Vector2(150, 20),
		};
		_speedValue = new Label { Text = $"x{Global.Instance.SpeedMultiplier:0.00}" };
		_speedValue.AddThemeColorOverride("font_color", Colors.White);
		_speedValue.CustomMinimumSize = new Vector2(48, 0);
		_speedSlider.ValueChanged += OnVelocidadeAlterada;
		linhaVelocidade.AddChild(rotuloVelocidade);
		linhaVelocidade.AddChild(_speedSlider);
		linhaVelocidade.AddChild(_speedValue);
		vbox.AddChild(linhaVelocidade);

		// --- Modo fantasma (noclip) ---
		_noclipToggle = new CheckButton { Text = "Modo Fantasma (Noclip)", ButtonPressed = Global.Instance.NoclipEnabled };
		_noclipToggle.AddThemeColorOverride("font_color", Colors.White);
		_noclipToggle.Toggled += OnNoclipAlternado;
		vbox.AddChild(_noclipToggle);

		// --- Zoom da câmera (para ver o cenário de perto ou de longe) ---
		HBoxContainer linhaZoom = new HBoxContainer();
		linhaZoom.AddThemeConstantOverride("separation", 8);
		Label rotuloZoom = new Label { Text = "Zoom câmera" };
		rotuloZoom.AddThemeColorOverride("font_color", Colors.White);
		_zoomSlider = new HSlider
		{
			MinValue = 0.25, // menos zoom ( vê mais do cenário)
			MaxValue = 4.0,  // mais zoom (vê de perto)
			Step = 0.05,
			Value = 1.0,
			CustomMinimumSize = new Vector2(150, 20),
		};
		_zoomValue = new Label { Text = "x1.00" };
		_zoomValue.AddThemeColorOverride("font_color", Colors.White);
		_zoomValue.CustomMinimumSize = new Vector2(48, 0);
		_zoomSlider.ValueChanged += OnZoomAlterado;
		linhaZoom.AddChild(rotuloZoom);
		linhaZoom.AddChild(_zoomSlider);
		linhaZoom.AddChild(_zoomValue);
		vbox.AddChild(linhaZoom);

		// --- Desbloquear tudo (para testar as habilidades sem andar o mapa) ---
		Button desbloquearTudo = new Button { Text = "Desbloquear todas as habilidades" };
		desbloquearTudo.Pressed += OnDesbloquearTudo;
		vbox.AddChild(desbloquearTudo);

		// --- Voltar ao padrão (reseta cheats) ---
		Button resetarCheats = new Button { Text = "Voltar ao padrão" };
		resetarCheats.Pressed += OnVoltarAoPadrao;
		vbox.AddChild(resetarCheats);

		Label ajuda = new Label { Text = "C + Enter abre/fecha esta janela", HorizontalAlignment = HorizontalAlignment.Center };
		ajuda.AddThemeFontSizeOverride("font_size", 12);
		ajuda.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		vbox.AddChild(ajuda);
	}

	public override void _Process(double delta)
	{
		// Detecta a "bordada" da combinação C + Enter (sem repetir enquanto segurado)
		bool combinacao = Input.IsKeyPressed(Key.C)
			&& (Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.KpEnter));

		if (combinacao && !_comboHeld)
		{
			Visible = !Visible;
			// Ao reabrir, sincroniza sliders/botão com o estado atual
			// (a Erika pode ter renascido, trocando a câmera)
			if (Visible)
			{
				AtualizarControles();
			}
		}
		_comboHeld = combinacao;
	}

	// Busca a câmera da Erika atual (re-busca se a personagem foi recriada)
	private Camera2D GetCamera()
	{
		if (IsInstanceValid(_camera))
		{
			return _camera;
		}
		_camera = null;
		Erika erika = Global.Instance.CurrentErika;
		if (erika != null)
		{
			_camera = erika.GetNodeOrNull<Camera2D>("Camera2D");
		}
		return _camera;
	}

	// Sincroniza os controles com o estado real do jogo
	private void AtualizarControles()
	{
		_speedSlider.SetValueNoSignal(Global.Instance.SpeedMultiplier);
		_speedValue.Text = $"x{Global.Instance.SpeedMultiplier:0.00}";
		_noclipToggle.SetPressedNoSignal(Global.Instance.NoclipEnabled);

		Camera2D cam = GetCamera();
		double zoomAtual = cam != null ? cam.Zoom.X : 1.0;
		_zoomSlider.SetValueNoSignal(zoomAtual);
		_zoomValue.Text = $"x{zoomAtual:0.00}";
	}

	private void OnVelocidadeAlterada(double valor)
	{
		Global.Instance.SpeedMultiplier = (float)valor;
		_speedValue.Text = $"x{valor:0.00}";
	}

	private void OnZoomAlterado(double valor)
	{
		Camera2D cam = GetCamera();
		if (cam != null)
		{
			cam.Zoom = new Vector2((float)valor, (float)valor);
		}
		_zoomValue.Text = $"x{valor:0.00}";
	}

	private void OnNoclipAlternado(bool ligado)
	{
		Global.Instance.SetNoclip(ligado);
	}

	private void OnVoltarAoPadrao()
	{
		// Reseta noclip e multiplicador de velocidade no Global
		Global.Instance.ResetCheats();

		// Reseta o zoom da câmera para 1.0×
		Camera2D cam = GetCamera();
		if (cam != null)
		{
			cam.Zoom = new Vector2(1.0f, 1.0f);
		}
		_zoomSlider.SetValueNoSignal(1.0);
		_zoomValue.Text = "x1.00";

		// Sincroniza os demais controles com os novos valores padrão
		AtualizarControles();
	}

	private void OnDesbloquearTudo()
	{
		Global.Instance.HasRun = true;
		Global.Instance.HasDash = true;
		Global.Instance.HasDoubleJump = true;
	}
}
