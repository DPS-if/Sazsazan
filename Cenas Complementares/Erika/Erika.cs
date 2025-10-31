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

	// --- (NOVO) Variáveis de Água e Afogamento ---
	[Export]
	public float StaminaDrainRateInWater = 10.0f; // Estamina gasta por segundo na água
	[Export]
	public float DrowningDamage = 1.0f;          // Dano de afogamento (1 vida)
	[Export]
	public float DrowningDamageInterval = 1.0f;  // Dano a cada 1 segundo

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
	
	// --- (NOVO) Controle da Água ---
	private bool _isInWater = false;
	private float _drowningTimer = 0.0f;

	// --- SINAL PARA AVISAR O HUD ---
	[Signal]
	public delegate void StatsChangedEventHandler(float currentHealth, float maxHealth, float currentStamina, float maxStamina);

	// --- Menu de Pausa e Dicas ---
	private MenuPause PauseMenu;
	private bool dentroAreaDuplo = false;
	private Label dicaPuloDuplo;
	private bool mostrouDica = false;

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
		
		// --- (CORRETO) Pega o seu nó de sprite "Animaçoes" ---
		_animatedSprite = GetNode<AnimatedSprite2D>("Animaçoes");
		
		_blinkTimer = new Timer();
		_blinkTimer.OneShot = false; 
		_blinkTimer.WaitTime = BlinkRate;
		_blinkTimer.Timeout += OnBlinkTimerTimeout; 
		AddChild(_blinkTimer);
		
		var pauseScene = GD.Load<PackedScene>("res://Menudepausa/MenuPause.tscn");
		PauseMenu = pauseScene.Instantiate<MenuPause>();
		AddChild(PauseMenu);
		PauseMenu.Visible = false;
		
		// Conecta à área de Pulo Duplo
		var areaPuloDuplo = GetTree().Root.FindChild("AreaAprendePDuplo", true, false);
		if (areaPuloDuplo is Area2D area)
		{
			area.BodyEntered += OnDuploAreaBodyEntered;
			area.BodyExited += OnDuploAreaBodyExited;
		}

		// (NOVO) Conecta à Área de Água
		var waterArea = GetTree().Root.FindChild("WaterArea", true, false);
		if (waterArea is Area2D water)
		{
			water.BodyEntered += OnWaterAreaBodyEntered;
			water.BodyExited += OnWaterAreaBodyExited;
		}

		dicaPuloDuplo = new Label
		{
			Text = "Aperte duas vezes ESPAÇO para usar o pulo duplo",
			Visible = false,
			Position = new Vector2(200, 50)
		};
		AddChild(dicaPuloDuplo);

		CallDeferred(nameof(EmitStatsChanged));
	}
	
	private void EmitStatsChanged()
	{
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}

	// --- (NOVO) Métodos de Entrada/Saída da Água ---
	private void OnWaterAreaBodyEntered(Node body)
	{
		if (body == this) { _isInWater = true; }
	}

	private void OnWaterAreaBodyExited(Node body)
	{
		if (body == this) 
		{ 
			_isInWater = false; 
			_drowningTimer = 0.0f; // Reseta o timer de afogamento
		}
	}
	// --- Fim dos Métodos da Água ---

	private void OnDuploAreaBodyEntered(Node body)
	{
		if (body == this) { dentroAreaDuplo = true; }
	}

	private void OnDuploAreaBodyExited(Node body)
	{
		if (body == this) { dentroAreaDuplo = false; }
	}

	public override void _Input(InputEvent @event)
	{
		var keyEvent = @event as InputEventKey;
		if (keyEvent?.Pressed == true && keyEvent.Keycode == Key.Escape)
		{
			GetTree().Paused = !GetTree().Paused;
			PauseMenu.Visible = GetTree().Paused;
		}
		
		// INPUT DO DASH COM O BOTÃO ESQUERDO DO MOUSE
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
		// 1. Checa a Estamina
		if (CurrentStamina < DashStaminaCost)
		{
			GD.Print("Sem estamina suficiente para o Dash!");
			return;
		}

		// 2. Define a direção do dash
		float inputDirection = Input.GetAxis("ui_left", "ui_right");

		if (inputDirection == 0)
		{
			if (Mathf.Abs(Velocity.X) > 0)
			{
				_dashDirection = Mathf.Sign(Velocity.X);
			}
			else
			{
				_dashDirection = _animatedSprite.FlipH ? -1.0f : 1.0f;
			}
		}
		else
		{
			_dashDirection = inputDirection;
		}

		// 3. Aplica o custo e Cooldown
		CurrentStamina -= DashStaminaCost;
		_dashCooldownTimer = DashCooldown;

		// 4. Inicia o Dash
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
			if (_dashDurationTimer <= 0)
			{
				_isDashing = false;
			}
		}

		// (CORRIGIDO) O timer de estamina só conta se for maior que 0
		if (_staminaRegenTimer > 0) { _staminaRegenTimer -= (float)delta; }
		
		if (_damageCooldownTimer > 0) 
		{ 
			_damageCooldownTimer -= (float)delta;
			if (_damageCooldownTimer <= 0)
			{
				StopBlinking();
			}
		}

		// --- (NOVO) Lógica de Água e Afogamento ---
		if (_isInWater)
		{
			// 1. Drenar Estamina (Fôlego)
			if (CurrentStamina > 0)
			{
				// Usa a função DrainStamina para ser consistente com o delay
				DrainStamina(StaminaDrainRateInWater * (float)delta); 
				
				// Reseta o timer de afogamento enquanto tiver fôlego
				_drowningTimer = 0.0f; 
			}
			// 2. Acabou a Estamina -> Começa a Afogar
			else
			{
				_drowningTimer += (float)delta;
				if (_drowningTimer >= DrowningDamageInterval)
				{
					_drowningTimer = 0.0f; // Reseta o timer do dano
					
					// (NOVO) Chama a função de dano ambiental
					TakeEnvironmentalDamage(DrowningDamage); 
				}
			}
		}
		else
		{
			// Se não está na água, reseta o timer de afogamento
			_drowningTimer = 0.0f;
		}
		// --- Fim da Lógica de Água ---
		
		// --- Lógica de Regeneração de Vida ---
		_regenTimer += (float)delta;
		if (_regenTimer >= RegenInterval)
		{
			Heal(1.0f);
			_regenTimer = 0.0f;
		}

		// --- 2. Lógica de Movimento e Pulo ---
		Vector2 velocity = Velocity;

		if (_isDashing)
		{
			velocity.X = _dashDirection * DashSpeed;
			velocity.Y = 0;
		}
		else // Movimento e Gravidade Normal
		{
			// (ALTERADO) Se estiver na água, a gravidade é menor (flutua)
			if (!IsOnFloor() && !_isInWater) 
			{ 
				velocity.Y += Gravity * (float)delta; 
			}
			else if (_isInWater)
			{
				// Flutuação leve (ajuste o 0.5f se quiser mais forte/fraco)
				velocity.Y += Gravity * 0.5f * (float)delta; 
			}

			// Reset do Pulo e Lógica de Aprendizado
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
				if (IsOnFloor() || _isInWater) // (ALTERADO) Pode pular do chão ou da água
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

			// --- 3. Lógica de Estamina e Sprint (TOTALMENTE CORRIGIDA) ---
			bool wantsToSprint = Input.IsActionPressed("ui_corrida");
			float currentSpeed = Speed;
			
			// (CORRIGIDO) "canSprint" agora checa o timer!
			bool canSprint = _staminaRegenTimer <= 0 && CurrentStamina > 0;

			// (ALTERADO) Não pode correr se estiver se afogando
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
					// (CORRIGIDO) Só ativamos o timer quando a estamina ACABA
					CurrentStamina = 0;
					_staminaRegenTimer = StaminaRegenDelay;
				}
			}
			else
			{
				// Lógica de Regeneração (está correta)
				// (ALTERADO) Não regenera estamina na água
				if (_staminaRegenTimer <= 0 && CurrentStamina < MaxStamina && !wantsToSprint && !_isInWater)
				{
					CurrentStamina += StaminaRegenRate * (float)delta;
					CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina);
				}
			}
			
			// 4. Aplica Movimento e Knockback (fora do Dash)
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
		
		// --- CHAMADA DA FUNÇÃO DE ANIMAÇÃO ---
		// (A lógica de espelhar (FlipH) foi movida para dentro desta função)
		_UpdateAnimation();

		MoveAndSlide();

		// 5. Emitir o Sinal
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}
	
	// --- FUNÇÃO DE ANIMAÇÃO (CORRIGIDA E COM ÁGUA) ---
	private void _UpdateAnimation()
	{
		// 1. Lógica de Espelhar (FlipH)
		// Só vira se a velocidade horizontal for significativa
		if (Mathf.Abs(Velocity.X) > 0.1f) 
		{
			// vira (true) se a velocidade for negativa (esquerda)
			_animatedSprite.FlipH = (Velocity.X < 0); 
		}

		// 2. Lógica de Animação
		string newAnimation = "";

		if (_isDashing)
		{
			// (Opcional: se você tiver uma animação "dash", coloque aqui)
			// newAnimation = "Erikadash"; 
		}
		else if (_isInWater) // (NOVO) Checa a água primeiro
		{
			// (AVISO: Você precisa criar uma animação "Erikapiscina" no seu Sprite)
			newAnimation = "Erikapiscina";
		}
		else if (!IsOnFloor())
		{
			newAnimation = "Erikapulando";
		}
		else // Está no chão
		{
			if (Mathf.Abs(Velocity.X) > 0.1f) // Se está se movendo
			{
				// (CORRIGIDO) A condição de "sprint"
				if (Input.IsActionPressed("ui_corrida") && _staminaRegenTimer <= 0 && CurrentStamina > 0)
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

		// Toca a animação apenas se ela mudou
		if (newAnimation != "" && _animatedSprite.Animation != newAnimation)
		{
			// (AVISO) Certifique-se de que você tem animações com estes nomes:
			// "Erikapiscina", "Erikapulando", "Erikaparada", "Erikaandando", "Erikacorrendo"
			_animatedSprite.Play(newAnimation);
		}
	}
	// --- FIM DA FUNÇÃO DE ANIMAÇÃO ---
	
	
	// MÉTODOS DE DANO/CURA/RESTART

	public void ApplyKnockback(Vector2 damageSourcePosition)
	{
		float directionX = (GlobalPosition.X - damageSourcePosition.X) > 0 ? 1.0f : -1.0f;

		_knockbackVelocity = new Vector2(
			directionX * KnockbackHorizontalForce,
			KnockbackVerticalForce
		);
	}

	public void Heal(float amount)
	{
		CurrentHealth += amount;
		CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
	}

	// --- MÉTODO PARA DRENAR ESTAMINA ---
	public void DrainStamina(float amount)
	{
		if (CurrentStamina > 0)
		{
			CurrentStamina -= amount;
			CurrentStamina = Mathf.Max(CurrentStamina, 0);
			
			// Isso garante que o timer de delay seja resetado
			_staminaRegenTimer = StaminaRegenDelay; 
		}
	}
	// --- FIM DO MÉTODO ---

	// --- (NOVO) MÉTODO DE DANO AMBIENTAL (AFOGAMENTO) ---
	public void TakeEnvironmentalDamage(float amount)
	{
		// Respeita os frames de invencibilidade (piscar)
		if (_damageCooldownTimer > 0)
		{
			return;
		}

		CurrentHealth -= amount;
		CurrentHealth = Mathf.Max(CurrentHealth, 0);

		_damageCooldownTimer = DamageCooldownTime; // Ativa o cooldown/piscar

		StartBlinking();

		GD.Print($"Dano de afogamento! Vida restante: {CurrentHealth}");

		if (CurrentHealth <= 0)
		{
			RestartGame();
		}
	}
	// --- FIM DO MÉTODO ---

	public void TakeDamage(float amount, Node2D damageSource)
	{
		// Se estiver em Dash, ou piscando, ignora o dano
		if (_damageCooldownTimer > 0)
		{
			return;
		}

		CurrentHealth -= amount;
		CurrentHealth = Mathf.Max(CurrentHealth, 0);

		_damageCooldownTimer = DamageCooldownTime;

		ApplyKnockback(damageSource.GlobalPosition);
		StartBlinking();

		GD.Print($"Dano recebido de {amount}! Vida restante: {CurrentHealth}");

		if (CurrentHealth <= 0)
		{
			RestartGame();
		}
	}
	
	public void RestartGame()
	{
		if (_isRestarting) 
		{
			return;
		}
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
		_blinkTimer.Start();
	}

	// MÉTODO DE RECEBIMENTO DO SINAL DO ESPINHO 1
	private void _on_espinho_1_body_entered(Node2D body)
	{
		if (body == this)
		{
			float damageAmount = 1.0f;

			var espinho1 = GetTree().Root.FindChild("espinho1", true, false) as Node2D;

			if (espinho1 != null)
			{
				TakeDamage(damageAmount, espinho1);
			}
			else
			{
				TakeDamage(damageAmount, new Node2D() { GlobalPosition = GlobalPosition + new Vector2(100, 0) });
			}
		}
	}

	// MÉTODO DE RECEBIMENTO DO SINAL DO ESPINHO 2
	private void _on_espinho_2_body_entered(Node2D body)
	{
		if (body == this)
		{
			float damageAmount = 1.0f;

			var espinho2 = GetTree().Root.FindChild("espinho2", true, false) as Node2D;

			if (espinho2 != null)
			{
				TakeDamage(damageAmount, espinho2);
			}
			else
			{
				TakeDamage(damageAmount, new Node2D() { GlobalPosition = GlobalPosition + new Vector2(100, 0) });
			}
		}
	}

	// --- MÉTODO: VOID (VAZIO) ---
	private void _on_void_body_entered(Node2D body)
	{
		if (body == this)
		{
			GD.Print("Erika caiu no Void. Reiniciando a fase.");
			RestartGame();
		}
	}
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
