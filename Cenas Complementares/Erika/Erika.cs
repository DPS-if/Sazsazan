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
	public float DrowningDamageInterval = 1.0f;  
	
	// --- (NOVO) VARIÁVEL DE ESPINHO ---
	[Export]
	public float SpikeDamage = 1.0f;
	// NOTA: O intervalo de dano contínuo é controlado por DamageCooldownTime (1.1s)

	// --- VARIÁVEIS DE DANO/REGENERAÇÃO/KNOCKBACK/BLINK ---
	[Export]
	public float DamagePercentage = 0.20f;
	[Export]
	public float RegenPercentage = 0.05f;
	
	[Export]
	public float RegenInterval = 10.0f;     
	[Export]
	public float DamageCooldownTime = 1.1f; // Define a frequência de dano de espinho e invencibilidade
	
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
	
	// --- (CRÍTICO) Controle dos Espinhos ---
	private bool _isTouchingSpikes = false;

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

		// Conecta à Área de Água
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
			if (_dashDurationTimer <= 0)
			{
				_isDashing = false;
			}
		}

		if (_staminaRegenTimer > 0) { _staminaRegenTimer -= (float)delta; }
		
		// Temporizador de Invencibilidade (Geral)
		if (_damageCooldownTimer > 0) 
		{ 
			_damageCooldownTimer -= (float)delta; // Decrementa o timer
			if (_damageCooldownTimer <= 0)
			{
				StopBlinking();
			}
		}

		// --- Lógica de Dano Contínuo de Espinhos (CRÍTICO) ---
		// Só aplica dano se estiver tocando o espinho E o período de invencibilidade (1.1s) tiver acabado.
		if (_isTouchingSpikes && _damageCooldownTimer <= 0)
		{
			// Procura o nó espinho para aplicar o Knockback.
			Node2D spikesSource = GetTree().Root.FindChild("espinhos", true, false) as Node2D;
			
			if (spikesSource != null)
			{
				// O TakeDamage() aplica o dano e REINICIA o _damageCooldownTimer (1.1s),
				// garantindo o intervalo de dano contínuo.
				TakeDamage(SpikeDamage, spikesSource);
			}
		}
		// --- Fim da Lógica de Espinhos ---

		// --- Lógica de Água e Afogamento ---
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

			// --- 3. Lógica de Estamina e Sprint ---
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
					_staminaRegenTimer = StaminaRegenDelay;
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
		
		_UpdateAnimation();

		MoveAndSlide();

		// 5. Emitir o Sinal
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}
	
	// --- FUNÇÃO DE ANIMAÇÃO ---
	private void _UpdateAnimation()
	{
		if (Mathf.Abs(Velocity.X) > 0.1f) 
		{
			_animatedSprite.FlipH = (Velocity.X < 0); 
		}

		string newAnimation = "";

		if (_isDashing)
		{
			// ...
		}
		else if (_isInWater) 
		{
			newAnimation = "Erikapiscina";
		}
		else if (!IsOnFloor())
		{
			newAnimation = "Erikapulando";
		}
		else 
		{
			if (Mathf.Abs(Velocity.X) > 0.1f) 
			{
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

		if (newAnimation != "" && _animatedSprite.Animation != newAnimation)
		{
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
		if (_damageCooldownTimer > 0)
		{
			return;
		}

		CurrentHealth -= amount;
		CurrentHealth = Mathf.Max(CurrentHealth, 0);

		_damageCooldownTimer = DamageCooldownTime; 

		StartBlinking();

		GD.Print($"Dano de afogamento! Vida restante: {CurrentHealth}");

		if (CurrentHealth <= 0)
		{
			RestartGame();
		}
	}

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
	
	// --- (NOVO) MÉTODOS DE COLISÃO DO NÓ PAI "espinhos" ---
	// ESTES MÉTODOS DEVEM SER CONECTADOS AO NÓ 'espinhos' NO EDITOR
	private void _on_espinhos_body_entered(Node2D body)
	{
		if (body == this)
		{
			GD.Print("Erika entrou na área dos espinhos!"); // DEBUG
			
			_isTouchingSpikes = true;
			
			// Aplica o primeiro dano imediatamente na entrada
			Node2D spikesSource = GetTree().Root.FindChild("espinhos", true, false) as Node2D;
			
			if (spikesSource != null)
			{
				TakeDamage(SpikeDamage, spikesSource);
			}
		}
	}

	private void _on_espinhos_body_exited(Node2D body)
	{
		if (body == this)
		{
			GD.Print("Erika saiu da área dos espinhos!"); // DEBUG
			_isTouchingSpikes = false;
		}
	}
	// --- FIM DOS MÉTODOS DE COLISÃO DO NÓ PAI "espinhos" ---
	
	// --- MÉTODOS ANTIGOS DE ESPINHO 1 e 2 FORAM REMOVIDOS ---
	// Para não criar confusão, eles não estão mais aqui.
	// Você pode removê-los do seu script, pois eles não servem para o nó pai 'espinhos'.

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
