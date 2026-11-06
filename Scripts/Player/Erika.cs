using Godot;
using System;

public partial class Erika : CharacterBody2D
{

	private CanvasLayer _inventarioInstance;
	// --- Variáveis de Movimento ---
	[Export]
	public float Speed = 200.0f;
	[Export]
	public float SprintMultiplier = 2.0f;
	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	// --- GAME FEEL: aceleração, atrito e limites ---
	[Export]
	public float Acceleration = 1800.0f;     // Quão rápido ela ganha velocidade
	[Export]
	public float Friction = 2200.0f;         // Quão rápido ela para ao soltar a tecla
	[Export]
	public float TerminalFallSpeed = 900.0f; // Limite da velocidade de queda

	// --- GAME FEEL: Coyote Time e Jump Buffer ---
	[Export]
	public float CoyoteTime = 0.1f;          // Tempo que ainda dá pra pular após sair da borda
	[Export]
	public float JumpBufferTime = 0.12f;     // Apertar pulo pouco antes de aterrissar ainda funciona

	// --- VARIÁVEIS DE DASH ---
	[Export(PropertyHint.Range, "100.0,2000.0,1.0")]
	public float DashSpeed = 900.0f;
	[Export(PropertyHint.Range, "0.1,0.5,0.01")]
	public float DashDuration = 0.15f;
	[Export]
	public float DashCooldown = 1.2f;

	public float DashStaminaCost => MaxStamina * 0.085f;

	// --- Variáveis de Pulo ---
	[Export]
	public float JumpVelocity = -350.0f;
	[Export]
	public float DoubleJumpVelocityMultiplier = 1.2f;

	// --- Variáveis de Stats e Estamina ---
	[Export]
	public float MaxHealth = 3.0f;
	[Export]
	public float MaxStamina = 100.0f;
	[Export]
	public float StaminaDrainRate = 5.0f;
	[Export]
	public float StaminaRegenRate = 15.0f;
	[Export]
	public float StaminaRegenDelay = 2.0f;
	[Export]
	public float DoubleJumpStaminaCost = 10.0f;

	// --- Variáveis de Água e Afogamento ---
	[Export]
	public float StaminaDrainRateInWater = 10.0f;
	[Export]
	public float DrowningDamage = 1.0f;
	[Export]
	public float DrowningDamageInterval = 1.0f;

	// --- VARIÁVEL DE ESPINHO ---
	[Export]
	public float SpikeDamage = 1.0f;

	// --- DANO/REGENERAÇÃO/KNOCKBACK ---
	[Export]
	public float DamagePercentage = 0.20f;
	// Regeneração: cura RegenPercentage% da vida máxima a cada RegenInterval
	// segundos (0.10 * 3.0 = 0.3 HP a cada 4s ≈ coração cheio em ~40s)
	[Export]
	public float RegenPercentage = 0.10f;
	[Export]
	public float RegenInterval = 4.0f;
	[Export]
	public float DamageCooldownTime = 1.1f;
	[Export]
	public float KnockbackHorizontalForce = 384.0f;
	[Export]
	public float KnockbackVerticalForce = -384.0f;
	[Export]
	public float KnockbackDuration = 0.25f;       // Tempo em que o knockback controla o movimento
	[Export]
	public float KnockbackDeceleration = 1200.0f; // Quão rápido o empurrão horizontal perde força
	[Export]
	public float BlinkRate = 0.05f;

	private float _regenTimer = 0.0f;
	private float _damageCooldownTimer = 0.0f;
	private Vector2 _knockbackVelocity = Vector2.Zero;
	private float _knockbackTimer = 0.0f;
	private bool _isRestarting = false;

	// --- Timers de game feel e direção ---
	private float _coyoteTimer = 0.0f;
	private float _jumpBufferTimer = 0.0f;
	private float _facingDirection = 1.0f;  // 1 = direita, -1 = esquerda (independe do sprite)
	private float _dashInvulnTimer = 0.0f;  // Invencibilidade do dash (separada da de dano)

	// Variáveis de estado do Dash
	private float _dashCooldownTimer = 0.0f;
	private float _dashDurationTimer = 0.0f;
	private bool _isDashing = false;
	private float _dashDirection = 0.0f;

	// VARIÁVEIS PARA O EFEITO BLINK (PISCAR)
	private AnimatedSprite2D _animatedSprite;
	private Timer _blinkTimer;

	public float CurrentHealth { get; private set; }
	public float CurrentStamina { get; private set; }

	// --- Controle do Pulo ---
	private bool _canDoubleJump = false;
	public bool sabepular2 = false;

	private float _staminaRegenTimer = 0.0f;

	// --- Controle da Água ---
	private bool _isInWater = false;
	private float _drowningTimer = 0.0f;

	// --- Controle dos Espinhos ---
	private bool _isTouchingSpikes = false;

	// --- SINAL PARA AVISAR O HUD ---
	[Signal]
	public delegate void StatsChangedEventHandler(float currentHealth, float maxHealth, float currentStamina, float maxStamina);

	// --- Menu de Pausa, ConfigBotões e Dicas ---
	private MenuPause PauseMenu;
	private ConfigBotões _configInstance;
	private Area2D _spikesSource; // Referência guardada uma única vez (antes era buscada a cada dano)
	private bool dentroAreaDuplo = false;
	private Label dicaPuloDuplo;
	private bool mostrouDica = false;

	// --- VARIÁVEL DO TEXTO INICIAL ---
	private Label _textoInicial;

	// --- VARIÁVEL DA DICA DE PULO SIMPLES ---
	private Label dicaPuloSimples;

	// --- VARIÁVEL DA DICA DE DASH ---
	private Label dicaDash;

	// --- Estado das dicas (para aparecerem apenas uma vez) ---
	private bool _dicaPuloSimplesMostrada = false;
	private bool _dicaDashMostrada = false;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;

		_animatedSprite = GetNode<AnimatedSprite2D>("Animaçoes");

		_blinkTimer = new Timer();
		_blinkTimer.OneShot = false;
		_blinkTimer.WaitTime = BlinkRate;
		_blinkTimer.Timeout += OnBlinkTimerTimeout;
		AddChild(_blinkTimer);

		var pauseScene = GD.Load<PackedScene>("res://Cenas/UI/MenuPause.tscn");
		PauseMenu = pauseScene.Instantiate<MenuPause>();
		AddChild(PauseMenu);
		PauseMenu.Visible = false;

		// As áreas da fase (pulo duplo, água, dicas, espinhos e void) são
		// conectadas pela própria cena da fase (node_2d.tscn), via sinais.
		// Aqui só guardamos a referência dos espinhos, usada para o knockback.
		_spikesSource = GetNodeOrNull<Area2D>("../espinhos");
		if (_spikesSource == null)
		{
			_spikesSource = GetTree().Root.FindChild("espinhos", true, false) as Area2D;
		}

		// Instancia a tela de ConfigBotões como uma camada sobre o jogo
		// (antes ela trocava de cena e o jogo em andamento era perdido)
		var configScene = GD.Load<PackedScene>("res://Cenas/UI/ConfigBotões.tscn");
		_configInstance = configScene.Instantiate<ConfigBotões>();
		_configInstance.Visible = false;
		AddChild(_configInstance);
		_configInstance.VoltarPressed += OnConfigVoltarPressed;
		PauseMenu.ConfigRequested += OnPauseConfigRequested;

		// -------------------------------------------------------------------
		// 1. CRIAÇÃO DA CONFIGURAÇÃO DE ESTILO (Reutilizável)
		// -------------------------------------------------------------------
		LabelSettings blackTextSettings = new LabelSettings();
		blackTextSettings.FontColor = Colors.Black;

		// -------------------------------------------------------------------
		// INICIALIZAÇÃO DO TEXTO INICIAL
		// -------------------------------------------------------------------
		_textoInicial = new Label
		{
			Text = "Aperte A/D para se mover e SHIFT para correr ---ATO1---",
			Position = new Vector2(200, 70),
			Visible = true,
			LabelSettings = blackTextSettings
		};
		AddChild(_textoInicial);

		Timer fadeTimer = new Timer();
		fadeTimer.OneShot = true;
		fadeTimer.WaitTime = 4.0f;
		fadeTimer.Timeout += () => _textoInicial.Visible = false;
		AddChild(fadeTimer);
		fadeTimer.Start();

		// -------------------------------------------------------------------
		// INICIALIZAÇÃO DA DICA DE PULO DUPLO
		// -------------------------------------------------------------------
		dicaPuloDuplo = new Label
		{
			Text = "Aperte duas vezes ESPAÇO para usar o pulo duplo",
			Visible = false,
			Position = new Vector2(200, 50),
			LabelSettings = blackTextSettings
		};
		AddChild(dicaPuloDuplo);

		// -------------------------------------------------------------------
		// INICIALIZAÇÃO DA DICA DE PULO SIMPLES
		// -------------------------------------------------------------------
		LabelSettings whiteTextSettings = new LabelSettings();
		whiteTextSettings.FontColor = Colors.White;

		dicaPuloSimples = new Label
		{
			Text = "Aperte ESPAÇO para pular",
			Visible = false,
			Position = new Vector2(270, 100),
			LabelSettings = whiteTextSettings
		};
		AddChild(dicaPuloSimples);

		// -------------------------------------------------------------------
		// INICIALIZAÇÃO DA DICA DE DASH
		// -------------------------------------------------------------------
		dicaDash = new Label
		{
			Text = "Aperte o botão esquerdo do mouse para usar o impulso",
			Visible = false,
			Position = new Vector2(180, 130),
			LabelSettings = blackTextSettings
		};
		AddChild(dicaDash);

		CallDeferred(nameof(EmitStatsChanged));
	}

	private void EmitStatsChanged()
	{
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}

	// --- MÉTODO: Entrada na área de Dica de Dash ---
	private void OnDicaDashBodyEntered(Node2D body)
	{
		if (body == this && !_dicaDashMostrada)
		{
			dicaDash.Visible = true;
			// Oculta outras dicas para focar no dash
			if (_textoInicial.Visible) { _textoInicial.Visible = false; }
			if (dicaPuloSimples.Visible) { dicaPuloSimples.Visible = false; }

			_dicaDashMostrada = true;
		}
	}

	// --- MÉTODO: Entrada na área de Ensino de Pulo Simples ---
	private void OnEnsinarPuloBodyEntered(Node2D body)
	{
		if (body == this && !_dicaPuloSimplesMostrada)
		{
			dicaPuloSimples.Visible = true;
			// Oculta a dica de movimento para não competir
			if (_textoInicial.Visible)
			{
				_textoInicial.Visible = false;
			}

			_dicaPuloSimplesMostrada = true;
		}
	}

	// --- Métodos de Entrada/Saída da Água ---
	private void OnWaterAreaBodyEntered(Node body)
	{
		if (body == this) { _isInWater = true; }
	}

	private void OnWaterAreaBodyExited(Node body)
	{
		if (body == this)
		{
			_isInWater = false;
			_drowningTimer = 0.0f;
		}
	}

	private void OnDuploAreaBodyEntered(Node body)
	{
		if (body == this) { dentroAreaDuplo = true; }
	}

	private void OnDuploAreaBodyExited(Node body)
	{
		if (body == this) { dentroAreaDuplo = false; }
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Pausa com Esc (via _UnhandledInput para não conflitar com cliques da UI)
		if (@event.IsActionPressed("ui_cancel"))
		{
			TogglePause();
		}

		if (@event.IsActionPressed("inventario"))
		{
			GerenciarInventario();
		}
	}

	private void TogglePause()
	{
		GetTree().Paused = !GetTree().Paused;
		PauseMenu.Visible = GetTree().Paused;

		// Se sair da pausa enquanto a tela de configuração estiver aberta, fecha ela também
		if (!GetTree().Paused && _configInstance != null && _configInstance.Visible)
		{
			_configInstance.Visible = false;
		}
	}

	// --- Navegação entre o Menu de Pausa e a tela de ConfigBotões ---
	// (agora são camadas sobre o jogo, não mais trocas de cena)
	private void OnPauseConfigRequested()
	{
		PauseMenu.Visible = false;
		_configInstance.ShowWithFade();
	}

	private void OnConfigVoltarPressed()
	{
		_configInstance.Visible = false;
		PauseMenu.Visible = true;
	}

	private void PerformDash()
	{
		if (CurrentStamina < DashStaminaCost)
		{
			return;
		}

		// Direção preferida: a tecla que estiver pressionada; senão, a última
		// direção em que ela estava virada (sem depender do FlipH do sprite)
		float inputDirection = Input.GetAxis("mover_esquerda", "mover_direita");

		if (inputDirection != 0)
		{
			_dashDirection = inputDirection;
		}
		else
		{
			_dashDirection = _facingDirection;
		}

		CurrentStamina -= DashStaminaCost;
		CurrentStamina = Mathf.Max(CurrentStamina, 0);
		_dashCooldownTimer = DashCooldown;

		_isDashing = true;
		_dashDurationTimer = DashDuration;

		// Invencibilidade do dash usa o próprio timer do dash,
		// sem roubar o cooldown de dano (que é responsabilidade do blink)
		_dashInvulnTimer = DashDuration;

		// Oculta a dica de dash ao usar o Dash
		if (dicaDash.Visible)
		{
			dicaDash.Visible = false;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		// --- 1. Timers ---
		if (_dashCooldownTimer > 0) { _dashCooldownTimer -= dt; }
		if (_dashInvulnTimer > 0) { _dashInvulnTimer -= dt; }
		if (_knockbackTimer > 0) { _knockbackTimer -= dt; }
		if (_staminaRegenTimer > 0) { _staminaRegenTimer -= dt; }

		if (_isDashing)
		{
			_dashDurationTimer -= dt;
			if (_dashDurationTimer <= 0)
			{
				_isDashing = false;
				// Só para o blink aqui se ele não for o blink de dano
				if (_damageCooldownTimer <= 0)
				{
					StopBlinking();
				}
			}
		}

		// Temporizador de Invencibilidade (dano)
		if (_damageCooldownTimer > 0)
		{
			_damageCooldownTimer -= dt;
			if (_damageCooldownTimer <= 0)
			{
				StopBlinking();
			}
		}

		// --- 2. Dano contínuo de Espinhos ---
		// Dano a cada 1.1s enquanto estiver em cima. Não machuca durante o dash.
		if (_isTouchingSpikes && _damageCooldownTimer <= 0 && _dashInvulnTimer <= 0)
		{
			if (_spikesSource != null)
			{
				TakeDamage(SpikeDamage, _spikesSource);
			}
		}

		// --- 3. Lógica de Água e Afogamento ---
		if (_isInWater)
		{
			if (CurrentStamina > 0)
			{
				DrainStamina(StaminaDrainRateInWater * dt);
				_drowningTimer = 0.0f;
			}
			else
			{
				_drowningTimer += dt;
				if (_drowningTimer >= DrowningDamageInterval)
				{
					_drowningTimer = 0.0f;
					TakeEnvironmentalDamage(DrowningDamage);
				}
			}
		}
		else
		{
			_drowningTimer = 0.0f;
		}

		// --- 4. Regeneração de Vida (usa a porcentagem configurada) ---
		_regenTimer += dt;
		if (_regenTimer >= RegenInterval)
		{
			Heal(RegenPercentage * MaxHealth);
			_regenTimer = 0.0f;
		}

		// --- 5. Input do Dash (por ação: funciona com mouse e gamepad) ---
		if (Input.IsActionJustPressed("dash") && !_isDashing && _dashCooldownTimer <= 0)
		{
			PerformDash();
		}

		// --- 6. Movimento ---
		Vector2 velocity = Velocity;

		if (_isDashing)
		{
			velocity.X = _dashDirection * DashSpeed;
			velocity.Y = 0;
		}
		else // Movimento e Gravidade Normal
		{
			// Gravidade (mais fraca na água) com limite de velocidade de queda
			if (_isInWater)
			{
				velocity.Y += Gravity * 0.5f * dt;
			}
			else if (!IsOnFloor())
			{
				velocity.Y += Gravity * dt;
			}
			velocity.Y = Mathf.Min(velocity.Y, TerminalFallSpeed);

			// --- Coyote Time: permite pular um instante após sair da borda ---
			if (IsOnFloor())
			{
				_coyoteTimer = CoyoteTime;
				_canDoubleJump = true;

				if (dentroAreaDuplo && !sabepular2)
				{
					sabepular2 = true;
					if (!mostrouDica)
					{
						dicaPuloDuplo.Text = "Aperte duas vezes ESPAÇO para usar o pulo duplo";
						dicaPuloDuplo.Visible = true;
						mostrouDica = true;
					}
				}
			}
			else
			{
				_coyoteTimer -= dt;
			}

			// --- Jump Buffer: registra o pulo apertado pouco antes de aterrissar ---
			if (Input.IsActionJustPressed("pular"))
			{
				_jumpBufferTimer = JumpBufferTime;
			}
			else if (_jumpBufferTimer > 0)
			{
				_jumpBufferTimer -= dt;
			}

			if (_jumpBufferTimer > 0)
			{
				if (IsOnFloor() || _coyoteTimer > 0 || _isInWater)
				{
					_jumpBufferTimer = 0.0f;
					_coyoteTimer = 0.0f;
					velocity.Y = JumpVelocity;

					// Oculta a dica de pulo simples ao pular
					if (dicaPuloSimples.Visible)
					{
						dicaPuloSimples.Visible = false;
					}
				}
				else if (sabepular2 && _canDoubleJump && CurrentStamina >= DoubleJumpStaminaCost)
				{
					_jumpBufferTimer = 0.0f;
					_canDoubleJump = false;
					velocity.Y = JumpVelocity * DoubleJumpVelocityMultiplier;
					CurrentStamina = Mathf.Max(CurrentStamina - DoubleJumpStaminaCost, 0);

					if (CurrentStamina <= 0)
					{
						_staminaRegenTimer = StaminaRegenDelay;
					}

					if (dicaPuloDuplo.Visible) { dicaPuloDuplo.Visible = false; }
				}
			}

			// --- 7. Lógica de Estamina e Sprint ---
			bool wantsToSprint = Input.IsActionPressed("correr");
			float currentSpeed = Speed;

			bool canSprint = _staminaRegenTimer <= 0 && CurrentStamina > 0;

			if (wantsToSprint && canSprint && _drowningTimer <= 0)
			{
				float staminaToDrain = StaminaDrainRate * dt;
				if (CurrentStamina > staminaToDrain)
				{
					currentSpeed = Speed * SprintMultiplier;
					CurrentStamina -= staminaToDrain;
				}
				else
				{
					CurrentStamina = 0;
					_staminaRegenTimer = StaminaRegenDelay;
				}
			}
			else if (_staminaRegenTimer <= 0 && CurrentStamina < MaxStamina && !wantsToSprint && !_isInWater)
			{
				CurrentStamina = Mathf.Min(CurrentStamina + StaminaRegenRate * dt, MaxStamina);
			}

			// --- 8. Knockback ou movimento normal ---
			if (_knockbackTimer > 0)
			{
				// Durante o knockback, o empurrão controla o movimento horizontal.
				// O impulso vertical é aplicado uma única vez (como um soco pra cima).
				velocity.X = _knockbackVelocity.X;
				velocity.Y += _knockbackVelocity.Y;
				_knockbackVelocity.Y = 0;
				_knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, KnockbackDeceleration * dt);
			}
			else
			{
				float horizontalDirection = Input.GetAxis("mover_esquerda", "mover_direita");

				if (horizontalDirection != 0)
				{
					// Aceleração suave até a velocidade alvo (antes era instantâneo)
					velocity.X = Mathf.MoveToward(velocity.X, horizontalDirection * currentSpeed, Acceleration * dt);
					_facingDirection = horizontalDirection;
				}
				else
				{
					// Atrito: desacelera suavemente ao soltar a tecla
					velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * dt);
				}
			}
		}

		Velocity = velocity;

		_UpdateAnimation();

		MoveAndSlide();

		// 9. Emitir o Sinal
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}

	private void _UpdateAnimation()
	{
		// Nomes das animações definidos no SpriteFrames da cena:
		// Erikaparada, Erikaandando, Erikacorrendo, Erikapulando
		string targetAnim;

		if (_isDashing)
		{
			targetAnim = "Erikapulando"; // Ainda não existe animação de dash própria
			_animatedSprite.FlipH = _dashDirection < 0;
		}
		else if (!IsOnFloor())
		{
			targetAnim = "Erikapulando";
			_animatedSprite.FlipH = _facingDirection < 0;
		}
		else if (Math.Abs(Velocity.X) > 0.1)
		{
			bool isSprintInput = Input.IsActionPressed("correr");
			targetAnim = isSprintInput ? "Erikacorrendo" : "Erikaandando";
			_animatedSprite.FlipH = Velocity.X < 0;
		}
		else
		{
			targetAnim = "Erikaparada";
			_animatedSprite.FlipH = _facingDirection < 0;
		}

		// Só troca se for diferente (evita reiniciar o frame a frame)
		if (_animatedSprite.Animation != targetAnim)
		{
			// Proteção: nunca chama Play com um nome que não existe no SpriteFrames
			if (_animatedSprite.SpriteFrames.HasAnimation(targetAnim))
			{
				_animatedSprite.Play(targetAnim);
			}
		}
	}

	// --- MÉTODOS DE STATS E DANO ---

	public void Heal(float amount)
	{
		CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
	}

	public void DrainStamina(float amount)
	{
		CurrentStamina -= amount;
		CurrentStamina = Mathf.Max(CurrentStamina, 0);
	}

	// Dano causado pelo ambiente (afogamento), sem knockback
	public void TakeEnvironmentalDamage(float amount)
	{
		if (_damageCooldownTimer > 0) { return; }

		CurrentHealth -= amount;
		_damageCooldownTimer = DamageCooldownTime;

		if (CurrentHealth <= 0)
		{
			CurrentHealth = 0;
			RestartGame();
		}
	}

	public void TakeDamage(float amount, Node2D damageSource)
	{
		// Invencibilidade do dash: dash atravessa perigos sem gastar o
		// cooldown de dano (que pertence ao blink)
		if (_dashInvulnTimer > 0) { return; }

		if (_damageCooldownTimer > 0) { return; }

		float totalDamage = amount + (amount * DamagePercentage);
		CurrentHealth -= totalDamage;

		_damageCooldownTimer = DamageCooldownTime;
		StartBlinking();

		ApplyKnockback(damageSource.GlobalPosition);

		if (CurrentHealth <= 0)
		{
			CurrentHealth = 0;
			RestartGame();
		}
	}

	public void RestartGame()
	{
		if (_isRestarting) { return; }

		_isRestarting = true;
		GetTree().Paused = false;

		// Transição suave para a Tela de Morte (o autoload lembra a fase
		// em que a Erika estava, para o botão de recomeçar voltar certo)
		SceneTransitions.Instance.PlayerDied();
	}

	public void ApplyKnockback(Vector2 damageSourcePosition)
	{
		float directionX = (GlobalPosition.X - damageSourcePosition.X) > 0 ? 1.0f : -1.0f;

		_knockbackVelocity = new Vector2(
			directionX * KnockbackHorizontalForce,
			KnockbackVerticalForce
		);
		// O knockback agora é um "estado": ele controla o movimento por um
		// tempo (KnockbackDuration) em vez de ser um impulso de 1 frame
		_knockbackTimer = KnockbackDuration;
	}

	// --- MÉTODOS DE BLINK (PISCA INVENCIBILIDADE) ---
	private void StartBlinking()
	{
		if (!_blinkTimer.IsStopped())
		{
			_blinkTimer.Stop();
		}

		_animatedSprite.Visible = true;
		_blinkTimer.Start();
	}

	private void StopBlinking()
	{
		_blinkTimer.Stop();
		_animatedSprite.Visible = true;
	}

	private void OnBlinkTimerTimeout()
	{
		_animatedSprite.Visible = !_animatedSprite.Visible;
	}

	// --- MÉTODOS DE ESPINHOS E VOID ---
	private void _on_espinhos_body_entered(Node2D body)
	{
		if (body == this)
		{
			_isTouchingSpikes = true;
			if (_spikesSource != null)
			{
				TakeDamage(SpikeDamage, _spikesSource);
			}
		}
	}

	private void _on_espinhos_body_exited(Node2D body)
	{
		if (body == this)
		{
			_isTouchingSpikes = false;
		}
	}

	private void _on_void_body_entered(Node2D body)
	{
		if (body == this)
		{
			RestartGame();
		}
	}

	// --- MÉTODO: Gerencia a abertura/fechamento do inventário ---
	private void GerenciarInventario()
	{
		if (_inventarioInstance == null)
		{
			var inventarioScene = GD.Load<PackedScene>("res://Cenas/UI/Inventário.tscn");
			_inventarioInstance = inventarioScene.Instantiate<CanvasLayer>();
			_inventarioInstance.Visible = false;
			AddChild(_inventarioInstance);
		}

		// Alterna a visibilidade da tela de inventário
		_inventarioInstance.Visible = !_inventarioInstance.Visible;
	}
}
