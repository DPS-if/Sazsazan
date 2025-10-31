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
	public float DrowningDamageInterval = 1.1f; 

	// --- VARIÁVEL DE ESPINHO ---
	[Export]
	public float SpikeDamage = 1.0f;

	// --- VARIÁVEIS DE DANO/REGENERAÇÃO/KNOCKBACK/BLINK ---
	[Export]
	public float DamagePercentage = 0.20f;
	[Export]
	public float RegenPercentage = 0.05f;
	
	[Export]
	public float RegenInterval = 10.0f;     
	[Export]
	public float DamageCooldownTime = 1.1f; 
	
	[Export]
	public float KnockbackHorizontalForce = 384.0f;
	[Export]
	public float KnockbackVerticalForce = -384.0f;
	
	[Export] 
	public float BlinkRate = 0.05f; 
	
	private float _regenTimer = 0.0f;
	private float _damageCooldownTimer = 0.0f;
	private Vector2 _knockbackVelocity = Vector2.Zero;
	private bool _isRestarting = false;
	
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

	// --- Menu de Pausa e Dicas ---
	private MenuPause PauseMenu;
	private bool dentroAreaDuplo = false;
	private Label dicaPuloDuplo;
	private bool mostrouDica = false;
	
	// --- Variáveis do Tutorial ---
	private Label _tutorialLabel;
	private Tween _tutorialTween; 
	private int _tutorialStep = 0;
	private bool _textIsShowing = false;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
		
		_animatedSprite = GetNode<AnimatedSprite2D>("Animaçoes");
		
		// Pega o nó Label do Tutorial
		_tutorialLabel = GetNode<Label>("TutorialLabel");
		
		// (CORRIGIDO) Linha do GetNode<Tween> REMOVIDA
		
		_tutorialLabel.Modulate = new Color(1, 1, 1, 0); // Começa invisível
		_tutorialLabel.Text = "";
		
		// Configura o Blink
		_blinkTimer = new Timer();
		_blinkTimer.OneShot = false; 
		_blinkTimer.WaitTime = BlinkRate;
		_blinkTimer.Timeout += OnBlinkTimerTimeout; 
		AddChild(_blinkTimer);
		
		// Configura o Menu de Pausa
		var pauseScene = GD.Load<PackedScene>("res://Menudepausa/MenuPause.tscn");
		PauseMenu = pauseScene.Instantiate<MenuPause>();
		AddChild(PauseMenu);
		PauseMenu.Visible = false;
		
		// --- Conecta a TODAS as Áreas de Trigger ---
		ConnectTriggerZone("AreaAprendePDuplo", OnDuploAreaBodyEntered, OnDuploAreaBodyExited);
		ConnectTriggerZone("WaterArea", OnWaterAreaBodyEntered, OnWaterAreaBodyExited);
		ConnectTriggerZone("espinhos", _on_espinhos_body_entered, _on_espinhos_body_exited);
		ConnectTriggerZone("Void", _on_void_body_entered); // (Adicionado para conectar o Void)
		
		// Conecta as Zonas de Tutorial
		ConnectTriggerZone("TutorialJumpZone", OnTutorialJumpZoneEntered);
		ConnectTriggerZone("TutorialFireZone", OnTutorialFireZoneEntered);
		ConnectTriggerZone("TutorialDashZone", OnTutorialDashZoneEntered);
		ConnectTriggerZone("TutorialRunZone", OnTutorialRunZoneEntered);
		ConnectTriggerZone("TutorialStaminaZone", OnTutorialStaminaZoneEntered);
		// --- Fim das Conexões ---

		dicaPuloDuplo = new Label
		{
			Text = "Aperte duas vezes ESPAÇO para usar o pulo duplo",
			Visible = false,
			Position = new Vector2(200, 50)
		};
		AddChild(dicaPuloDuplo);

		CallDeferred(nameof(EmitStatsChanged));
	}
	
	// --- (BUG CS1503 CORRIGIDO) ---
	// Método auxiliar para conectar sinais
	// Agora usa "Node2D" em vez de "Node"
	private void ConnectTriggerZone(string zoneName, Action<Node2D> enterCallback, Action<Node2D> exitCallback = null)
	{
		var zone = GetTree().Root.FindChild(zoneName, true, false);
		if (zone is Area2D area)
		{
			// --- (BUG CS1661 CORRIGIDO) ---
			// O lambda agora espera "Node2D"
			area.BodyEntered += (Node2D body) => { if (body == this) enterCallback(body); };
			if (exitCallback != null)
			{
				area.BodyExited += (Node2D body) => { if (body == this) exitCallback(body); };
			}
		}
		else
		{
			GD.Print($"Aviso: Não foi possível encontrar a Area2D chamada '{zoneName}'.");
		}
	}
	
	private void EmitStatsChanged()
	{
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}

	// --- (BUG CS1503 CORRIGIDO) ---
	// Callbacks das Áreas agora usam "Node2D"
	private void OnWaterAreaBodyEntered(Node2D body) { _isInWater = true; }
	private void OnWaterAreaBodyExited(Node2D body) { _isInWater = false; _drowningTimer = 0.0f; }
	private void OnDuploAreaBodyEntered(Node2D body) { dentroAreaDuplo = true; }
	private void OnDuploAreaBodyExited(Node2D body) { dentroAreaDuplo = false; }

	// --- (BUG CS1503 CORRIGIDO) ---
	// Callbacks de Tutorial agora usam "Node2D"
	private void OnTutorialJumpZoneEntered(Node2D body)
	{
		if (_tutorialStep == 2)
		{
			ShowTutorialMessage("Pressione Espaço para pular");
			_tutorialStep = 3; 
		}
	}

	private void OnTutorialFireZoneEntered(Node2D body)
	{
		if (_tutorialStep == 3)
		{
			ShowTutorialMessage("Cuidado! Obstáculos como fogo e espinhos tiram sua vida.");
			_tutorialStep = 4;
		}
	}

	private void OnTutorialDashZoneEntered(Node2D body)
	{
		if (_tutorialStep == 4)
		{
			ShowTutorialMessage("Use o Botão Esquerdo do Mouse para um impulso rápido (Dash)");
			_tutorialStep = 5;
		}
	}

	private void OnTutorialRunZoneEntered(Node2D body)
	{
		if (_tutorialStep == 5)
		{
			ShowTutorialMessage("Segure Ctrl Esquerdo para correr");
			_tutorialStep = 6;
		}
	}

	private void OnTutorialStaminaZoneEntered(Node2D body)
	{
		if (_tutorialStep == 6)
		{
			ShowTutorialMessage("Correr, Pular no Ar e usar o Dash consomem sua Estamina (barra verde)", 5.0f);
			_tutorialStep = 7; // Tutorial completo
		}
	}
	// --- Fim dos Callbacks de Tutorial ---

	public override void _Input(InputEvent @event)
	{
		var keyEvent = @event as InputEventKey;
		if (keyEvent?.Pressed == true && keyEvent.Keycode == Key.Escape)
		{
			GetTree().Paused = !GetTree().Paused;
			PauseMenu.Visible = GetTree().Paused;
		}
		
		var mouseEvent = @event as InputEventMouseButton;
		if (mouseEvent?.Pressed == true && mouseEvent.ButtonIndex == MouseButton.Left && !_isDashing && _dashCooldownTimer <= 0)
		{
			PerformDash();
		}
		
		if (@event.IsActionPressed("ui_inventory"))
		{
			GerenciarInventario();
		}
	}
	
	private void PerformDash()
	{
		if (CurrentStamina < DashStaminaCost)
		{
			GD.Print("Sem estamina suficiente para o Dash!");
			return;
		}

		float inputDirection = Input.GetAxis("ui_left", "ui_right");
		_dashDirection = (inputDirection == 0) ? (_animatedSprite.FlipH ? -1.0f : 1.0f) : inputDirection;

		CurrentStamina -= DashStaminaCost;
		_dashCooldownTimer = DashCooldown;
		_isDashing = true;
		_dashDurationTimer = DashDuration;
		_damageCooldownTimer = DashDuration;
		StartBlinking();
	}

	public override void _PhysicsProcess(double delta)
	{
		// --- 1. Lógica de Tempo e Timers ---
		if (_dashCooldownTimer > 0) { _dashCooldownTimer -= (float)delta; }
		if (_isDashing)
		{
			_dashDurationTimer -= (float)delta;
			if (_dashDurationTimer <= 0) { _isDashing = false; }
		}
		if (_staminaRegenTimer > 0) { _staminaRegenTimer -= (float)delta; }
		if (_damageCooldownTimer > 0) 
		{ 
			_damageCooldownTimer -= (float)delta; 
			if (_damageCooldownTimer <= 0) { StopBlinking(); }
		}

		// --- 2. Lógica de Dano de Espinhos ---
		if (_isTouchingSpikes && _damageCooldownTimer <= 0)
		{
			Node2D spikesSource = GetTree().Root.FindChild("espinhos", true, false) as Node2D;
			TakeDamage(SpikeDamage, spikesSource ?? this);
		}

		// --- 3. Lógica de Água e Afogamento ---
		if (_isInWater)
		{
			if (CurrentStamina > 0)
			{
				DrainStamina(StaminaDrainRateInWater * (float)delta); 
				_drowningTimer = 0.0f; 
			}
			else
			{
				_drowningTimer += (float)delta;
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
		
		// --- 4. Lógica de Regeneração de Vida ---
		_regenTimer += (float)delta;
		if (_regenTimer >= RegenInterval)
		{
			Heal(1.0f);
			_regenTimer = 0.0f;
		}

		// --- 5. Lógica de Movimento e Pulo ---
		Vector2 velocity = Velocity;
		if (_isDashing)
		{
			velocity.X = _dashDirection * DashSpeed;
			velocity.Y = 0;
		}
		else // Movimento e Gravidade Normal
		{
			if (!IsOnFloor() && !_isInWater) 
			{ 
				velocity.Y += Gravity * (float)delta; 
			}
			else if (_isInWater)
			{
				velocity.Y += Gravity * 0.5f * (float)delta; 
			}

			if (IsOnFloor())
			{
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

			if (Input.IsActionJustPressed("ui_accept"))
			{
				if (IsOnFloor() || _isInWater) 
				{
					velocity.Y = JumpVelocity;
				}
				else if (sabepular2 && _canDoubleJump)
				{
					if (CurrentStamina >= DoubleJumpStaminaCost)
					{
						_canDoubleJump = false;
						velocity.Y = JumpVelocity * DoubleJumpVelocityMultiplier;
						CurrentStamina -= DoubleJumpStaminaCost;
						CurrentStamina = Mathf.Max(CurrentStamina, 0);
						if (CurrentStamina <= 0)
						{
							_staminaRegenTimer = StaminaRegenDelay;
						}
						if (dicaPuloDuplo.Visible) { dicaPuloDuplo.Visible = false; }
					}
				}
			}

			// --- 6. Lógica de Estamina e Sprint (CORRIGIDA) ---
			bool wantsToSprint = Input.IsActionPressed("ui_corrida");
			float currentSpeed = Speed;
			bool canSprint = _staminaRegenTimer <= 0 && CurrentStamina > 0;

			if (wantsToSprint && canSprint && _drowningTimer <= 0)
			{
				float staminaToDrain = StaminaDrainRate * (float)delta;
				if (CurrentStamina > staminaToDrain)
				{
					currentSpeed = Speed * SprintMultiplier;
					CurrentStamina -= staminaToDrain;
				}
				else
				{
					CurrentStamina = 0;
					_staminaRegenTimer = StaminaRegenDelay; // Ativa o delay
				}
			}
			else
			{
				if (_staminaRegenTimer <= 0 && CurrentStamina < MaxStamina && !wantsToSprint && !_isInWater)
				{
					CurrentStamina += StaminaRegenRate * (float)delta;
					CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina);
				}
			}
			
			// 7. Aplica Movimento e Knockback
			float horizontalDirection = Input.GetAxis("ui_left", "ui_right");
			_knockbackVelocity = _knockbackVelocity.MoveToward(Vector2.Zero, 1000 * (float)delta); 

			if (_knockbackVelocity.LengthSquared() > 0)
			{
				velocity.X = _knockbackVelocity.X;
				velocity.Y += _knockbackVelocity.Y;
				_knockbackVelocity.Y = 0;
			}
			else
			{
				if (horizontalDirection != 0) { velocity.X = horizontalDirection * currentSpeed; }
				else { velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed); }
			}
		}
		Velocity = velocity;
		
		// --- 8. Chamada de Funções de Update ---
		_UpdateTutorial();
		_UpdateAnimation();
		MoveAndSlide();

		// 9. Emitir o Sinal
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}
	
	// --- FUNÇÃO DE ANIMAÇÃO (CORRIGIDA) ---
	private void _UpdateAnimation()
	{
		// 1. Lógica de Espelhar (FlipH)
		if (Mathf.Abs(Velocity.X) > 0.1f) 
		{
			_animatedSprite.FlipH = (Velocity.X < 0); 
		}

		// 2. Lógica de Animação
		string newAnimation = "";

		if (_isDashing) { /* (Opcional: animação de dash) */ }
		else if (_isInWater) { newAnimation = "Erikapiscina"; } 
		else if (!IsOnFloor()) { newAnimation = "Erikapulando"; }
		else // Está no chão
		{
			if (Mathf.Abs(Velocity.X) > 0.1f) // Se está se movendo
			{
				// (CORRIGIDO) A condição de "sprint"
				if (Input.IsActionPressed("ui_corrida") && CurrentStamina > 0)
				{
					newAnimation = "Erikacorrendo";
				}
				else
				{
					newAnimation = "Erikaandando";
				}
			}
			else
			{
				newAnimation = "Erikaparada";
			}
		}

		if (newAnimation != "" && _animatedSprite.Animation != newAnimation)
		{
			// (AVISO) Certifique-se de que você tem animações com estes nomes:
			// "Erikapiscina", "Erikapulando", "Erikaparada", "Erikaandando", "Erikacorrendo"
			_animatedSprite.Play(newAnimation);
		}
	}
	
	// --- FUNÇÃO DE TUTORIAL (CORRIGIDA) ---
	private void _UpdateTutorial()
	{
		float horizontalDirection = Input.GetAxis("ui_left", "ui_right");

		switch (_tutorialStep)
		{
			case 0: // 1. Spawn: "Use A/D para se mover"
				if (!_textIsShowing)
				{
					ShowTutorialMessage("Use A e D para se mover");
					_textIsShowing = true;
				}
				// Checa a progressão
				if (horizontalDirection != 0)
				{
					_tutorialStep = 1; // Vai para o próximo passo
					_textIsShowing = false; // Permite o próximo texto aparecer
				}
				break;

			case 1: // 2. Primeiro Movimento: "Ato 1"
				if (!_textIsShowing)
				{
					ShowTutorialMessage("Ato 1", 3.0f); // Mostra "Ato 1" por 3 segundos
					_tutorialStep = 2; // Imediatamente vai para o próximo estado (esperar o pulo)
					_textIsShowing = true; // Impede que "Ato 1" toque de novo
				}
				break;
			
			// Casos 2, 3, 4, 5, 6: Esperando pelas Zonas de Trigger
			case 2: // Esperando pela TutorialJumpZone
			case 3: // Esperando pela TutorialFireZone
			case 4: // Esperando pela TutorialDashZone
			case 5: // Esperando pela TutorialRunZone
			case 6: // Esperando pela TutorialStaminaZone
				break; 
			
			case 7: // Tutorial Completo
				// Não faz nada
				break;
		}
	}
	
	// --- (NOVO) Funções de Transição de Texto (Fade) ---
	// (ESTA É A VERSÃO CORRIGIDA)
	private void ShowTutorialMessage(string text, float duration = 0)
	{
		// Mata qualquer animação anterior
		if (_tutorialTween != null && _tutorialTween.IsRunning())
		{
			_tutorialTween.Kill();
		}
		
		// Cria uma nova sequência de transição (O JEITO CERTO DO GODOT 4)
		_tutorialTween = CreateTween();
		
		// 1. Fade out do texto antigo (se estiver visível)
		if (_tutorialLabel.Modulate.A > 0)
		{
			_tutorialTween.TweenProperty(_tutorialLabel, "modulate:a", 0, 0.3f);
		}
		
		// 2. Mudar o texto quando estiver invisível
		_tutorialTween.TweenCallback(Callable.From(() => 
		{ 
			_tutorialLabel.Text = text; 
			_textIsShowing = true;
			
			// (Re-centraliza o Label)
			// Isso ajusta a caixa para o novo tamanho do texto
			_tutorialLabel.ResetSize(); 
			_tutorialLabel.Size = new Vector2(300, 50); // Força o tamanho da caixa que definimos
			_tutorialLabel.Position = new Vector2(-150, -60); // Re-centraliza a caixa
		}));
		
		// 3. Fade in do texto novo
		_tutorialTween.TweenProperty(_tutorialLabel, "modulate:a", 1, 0.3f);
		
		// 4. Se tiver duração, espera e faz fade out
		if (duration > 0)
		{
			_tutorialTween.TweenInterval(duration);
			_tutorialTween.TweenProperty(_tutorialLabel, "modulate:a", 0, 0.3f);
			_tutorialTween.TweenCallback(Callable.From(() => { _textIsShowing = false; }));
		}
		// --- (BUG DE LÓGICA CORRIGIDO) ---
		// Se a duração for 0 (como na primeira mensagem),
		// o _textIsShowing nunca ficava "false".
		else
		{
			// Marca como "não mostrando" 1 segundo depois do fade-in
			_tutorialTween.TweenInterval(1.0f); 
			_tutorialTween.TweenCallback(Callable.From(() => { _textIsShowing = false; }));
		}
	}
	
	
	// MÉTODOS DE DANO/CURA/RESTART

	public void ApplyKnockback(Vector2 damageSourcePosition)
	{
		float directionX = (GlobalPosition.X - damageSourcePosition.X) > 0 ? 1.0f : -1.0f;
		_knockbackVelocity = new Vector2(directionX * KnockbackHorizontalForce, KnockbackVerticalForce);
	}

	public void Heal(float amount)
	{
		CurrentHealth += amount;
		CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
	}

	public void DrainStamina(float amount)
	{
		if (CurrentStamina > 0)
		{
			CurrentStamina -= amount;
			CurrentStamina = Mathf.Max(CurrentStamina, 0);
			_staminaRegenTimer = StaminaRegenDelay; 
		}
	}

	public void TakeEnvironmentalDamage(float amount)
	{
		if (_damageCooldownTimer > 0) { return; }
		CurrentHealth -= amount;
		CurrentHealth = Mathf.Max(CurrentHealth, 0);
		_damageCooldownTimer = DamageCooldownTime; 
		StartBlinking();
		GD.Print($"Dano de afogamento! Vida restante: {CurrentHealth}");
		if (CurrentHealth <= 0) { RestartGame(); }
	}

	public void TakeDamage(float amount, Node2D damageSource)
	{
		if (_damageCooldownTimer > 0) { return; }
		CurrentHealth -= amount;
		CurrentHealth = Mathf.Max(CurrentHealth, 0);
		_damageCooldownTimer = DamageCooldownTime;
		ApplyKnockback(damageSource.GlobalPosition);
		StartBlinking();
		GD.Print($"Dano recebido de {amount}! Vida restante: {CurrentHealth}");
		if (CurrentHealth <= 0) { RestartGame(); }
	}
	
	public void RestartGame()
	{
		if (_isRestarting) { return; }
		_isRestarting = true; 
		GD.Print("Erika morreu! Reiniciando o jogo.");
		GetTree().CallDeferred("reload_current_scene");
	}
	
	// --- MÉTODOS DE CONTROLE DO BLINK (PISCAR) ---
	private void OnBlinkTimerTimeout()
	{
		_animatedSprite.Visible = !_animatedSprite.Visible; 
	}

	private void StopBlinking()
	{
		_blinkTimer.Stop();
		_animatedSprite.Visible = true; 
	}

	private void StartBlinking()
	{
		_animatedSprite.Visible = true; // Garante que começa visível
		_blinkTimer.Start();
	}
	
	// --- (BUG CS1503 CORRIGIDO) ---
	// Métodos de colisão "espinhos" agora usam "Node2D"
	private void _on_espinhos_body_entered(Node2D body)
	{
		if (body == this)
		{
			GD.Print("Erika entrou na área dos espinhos!");
			_isTouchingSpikes = true;
			
			// Aplica o primeiro dano imediatamente na entrada
			Node2D spikesSource = GetTree().Root.FindChild("espinhos", true, false) as Node2D;
			TakeDamage(SpikeDamage, spikesSource ?? this);
		}
	}

	private void _on_espinhos_body_exited(Node2D body)
	{
		if (body == this)
		{
			GD.Print("Erika saiu da área dos espinhos!"); 
			_isTouchingSpikes = false;
		}
	}
	// --- Fim dos Métodos de Espinhos ---

	// --- (BUG CS1503 CORRIGIDO) ---
	// Método do Void agora usa "Node2D"
	private void _on_void_body_entered(Node2D body)
	{
		if (body == this)
		{
			GD.Print("Erika caiu no Void. Reiniciando a fase.");
			RestartGame();
		}
	}
	
	// --- MÉTODO DO INVENTÁRIO ---
	private void GerenciarInventario()
	{
		if (_inventarioInstance == null)
		{
			var inventarioScene = GD.Load<PackedScene>("res://inventário/Inventário.tscn");
			_inventarioInstance = (CanvasLayer)inventarioScene.Instantiate();
			_inventarioInstance.Visible = false;
			AddChild(_inventarioInstance);
		}
		_inventarioInstance.Visible = !_inventarioInstance.Visible;
		GetTree().Paused = _inventarioInstance.Visible;
	}
}
