using Godot;

/// <summary>
/// Singleton global (autoload "Global").
/// - Guarda o estado das habilidades desbloqueadas da Erika, que persistem
///   entre as cenas/salas durante a sessão do jogo.
/// - Guarda o multiplicador de velocidade e o estado do modo fantasma (cheats).
/// - Carrega/salva o remapeamento de teclas em user://settings.cfg.
/// </summary>
public partial class Global : Node
{
	public static Global Instance { get; private set; }

	// --- HABILIDADES DESBLOQUEÁVEIS (todas começam TRANCADAS) ---
	// Desbloqueadas pelas áreas de tutorial da fase (AreaRun, AreaDash,
	// AreaDoubleJump). Ficam aqui para persistirem entre salas/cenas.
	public bool HasRun { get; set; } = false;
	public bool HasDash { get; set; } = false;
	public bool HasDoubleJump { get; set; } = false;

	// --- CHEATS (console C + Enter) ---
	public float SpeedMultiplier { get; set; } = 1.0f;
	public bool NoclipEnabled { get; private set; } = false;
	public Erika CurrentErika { get; private set; }

	// --- VÍDEO (resolução / tela cheia) ---
	// Opções oferecidas na tela de configurações (a primeira é o padrão do projeto)
	public static readonly Vector2I[] ResoluçõesDisponíveis =
	{
		new Vector2I(1152, 648),
		new Vector2I(1280, 720),
		new Vector2I(1600, 900),
		new Vector2I(1920, 1080),
		new Vector2I(2560, 1440),
	};

	public bool IsFullscreen { get; private set; } = false;
	public Vector2I ResoluçãoJanela { get; private set; } = new Vector2I(1152, 648);

	private const string SettingsPath = "user://settings.cfg";

	// Ações que podem ser remapeadas na tela de configurações
	public static readonly string[] AçõesRemapeáveis =
	{
		"mover_esquerda", "mover_direita", "pular", "dash", "correr"
	};

	public override void _Ready()
	{
		Instance = this;
		LoadKeyBindings();
		LoadVideoSettings();
		AddChild(new CheatConsole());
	}

	// --- REFERÊNCIA DA ERIKA (para os cheats controlarem ela) ---

	public void RegisterErika(Erika erika)
	{
		CurrentErika = erika;
		// Reaplica o modo fantasma na Erika nova (ex.: depois de morrer)
		if (NoclipEnabled && CurrentErika != null)
		{
			CurrentErika.SetNoclip(true);
		}
	}

	public void SetNoclip(bool enabled)
	{
		NoclipEnabled = enabled;
		if (IsInstanceValid(CurrentErika))
		{
			CurrentErika.SetNoclip(enabled);
		}
	}

	/// <summary>
	/// Remove todas as modificações feitas pelos cheats (modo fantasma, multiplicador de
	/// velocidade). Chamado quando o jogador sai da partida (volta ao título) e também
	/// pelo botão "Voltar ao padrão" do console de cheats.
	/// </summary>
	public void ResetCheats()
	{
		NoclipEnabled = false;
		SpeedMultiplier = 1.0f;

		if (IsInstanceValid(CurrentErika))
		{
			CurrentErika.SetNoclip(false);
		}
	}

	// --- VÍDEO: TELA CHEIA / RESOLUÇÃO ---

	/// <summary>Alterna entre tela cheia e janela, aplicando na hora e salvando.</summary>
	public void SetFullscreen(bool fullscreen)
	{
		IsFullscreen = fullscreen;
		DisplayServer.WindowSetMode(fullscreen
			? DisplayServer.WindowMode.Fullscreen
			: DisplayServer.WindowMode.Windowed);

		// Voltando para janela, re-aplica a resolução escolhida e centraliza
		if (!fullscreen)
		{
			ApplyResolution();
		}
		SaveVideoSettings();
	}

	/// <summary>Define a resolução da janela. Em tela cheia, só guarda para quando sair.</summary>
	public void SetResolution(Vector2I resolução)
	{
		ResoluçãoJanela = resolução;
		if (!IsFullscreen)
		{
			ApplyResolution();
		}
		SaveVideoSettings();
	}

	private void ApplyResolution()
	{
		DisplayServer.WindowSetSize(ResoluçãoJanela);
		CentralizarJanela();
	}

	// Centraliza a janela na área de trabalho
	private void CentralizarJanela()
	{
		Vector2I tela = DisplayServer.ScreenGetSize();
		Vector2I janela = DisplayServer.WindowGetSize();
		DisplayServer.WindowSetPosition((tela - janela) / 2);
	}

	/// <summary>Carrega as preferências de vídeo salvas no settings.cfg e aplica no boot.</summary>
	private void LoadVideoSettings()
	{
		ConfigFile cfg = new ConfigFile();
		if (cfg.Load(SettingsPath) != Error.Ok) { return; }

		IsFullscreen = (bool)cfg.GetValue("video", "fullscreen", false);

		string textoResolução = (string)cfg.GetValue("video", "resolução", "1152x648");
		foreach (Vector2I r in ResoluçõesDisponíveis)
		{
			if ($"{r.X}x{r.Y}" == textoResolução)
			{
				ResoluçãoJanela = r;
				break;
			}
		}

		// Com um pequeno atraso, para garantir que a janela principal já existe
		CallDeferred(nameof(AplicarVideoNoBoot));
	}

	private void AplicarVideoNoBoot()
	{
		if (IsFullscreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
		else
		{
			ApplyResolution();
		}
	}

	private void SaveVideoSettings()
	{
		ConfigFile cfg = new ConfigFile();
		cfg.Load(SettingsPath); // falha silenciosamente se ainda não existe
		cfg.SetValue("video", "fullscreen", IsFullscreen);
		cfg.SetValue("video", "resolução", $"{ResoluçãoJanela.X}x{ResoluçãoJanela.Y}");
		cfg.Save(SettingsPath);
	}

	// --- REMAPEAMENTO DE TECLAS ---

	/// <summary>
	/// Retorna o nome amigável da tecla atual de uma ação.
	/// Prefere eventos de teclado; se a ação só tiver mouse/gamepad, descreve o botão.
	/// </summary>
	public string GetActionKeyText(string action)
	{
		InputEventMouseButton mouse = null;

		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventKey key)
			{
				return key.PhysicalKeycode != Key.None
					? key.AsTextPhysicalKeycode()
					: key.AsText();
			}
			if (ev is InputEventMouseButton mb)
			{
				mouse = mb;
			}
		}

		if (mouse != null)
		{
			return mouse.ButtonIndex == MouseButton.Left
				? "Botão Esquerdo do Mouse"
				: $"Botão {(int)mouse.ButtonIndex} do Mouse";
		}

		return action;
	}

	/// <summary>
	/// Troca a tecla de uma ação (mantém os eventos de mouse/gamepad que existirem),
	/// aplica no InputMap e salva em user://settings.cfg.
	/// </summary>
	public void SetActionKey(string action, Key key)
	{
		if (!InputMap.HasAction(action)) { return; }

		// Remove apenas os eventos de TECLADO da ação
		foreach (InputEvent ev in InputMap.ActionGetEvents(action))
		{
			if (ev is InputEventKey)
			{
				InputMap.ActionEraseEvent(action, ev);
			}
		}

		InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });

		ConfigFile cfg = new ConfigFile();
		cfg.Load(SettingsPath); // falha silenciosamente se ainda não existe
		cfg.SetValue("input", action, (long)key);
		cfg.Save(SettingsPath);
	}

	/// <summary>Aplica as teclas salvas no settings.cfg (chamado no boot).</summary>
	private void LoadKeyBindings()
	{
		ConfigFile cfg = new ConfigFile();
		if (cfg.Load(SettingsPath) != Error.Ok) { return; }

		foreach (string action in AçõesRemapeáveis)
		{
			if (!cfg.HasSectionKey("input", action)) { continue; }

			Key key = (Key)(long)cfg.GetValue("input", action, 0L);
			if (key == Key.None) { continue; }

			foreach (InputEvent ev in InputMap.ActionGetEvents(action))
			{
				if (ev is InputEventKey)
				{
					InputMap.ActionEraseEvent(action, ev);
				}
			}
			InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
		}
	}
}
