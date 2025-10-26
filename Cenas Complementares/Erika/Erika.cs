using Godot;
using System;

public partial class Erika : CharacterBody2D
{
	// --- Variáveis de Movimento ---
	[Export]
	public float Speed = 200.0f;
	[Export]
	public float SprintMultiplier = 2.0f;
	[Export]
	public float JumpVelocity = -350.0f;
	public int ndepulo = 1;

	// --- Variáveis de Stats ---
	[Export]
	public float MaxHealth = 100.0f;
	[Export]
	public float MaxStamina = 100.0f;
	[Export]
	public float StaminaDrainRate = 20.0f; 
	[Export]
	public float StaminaRegenRate = 15.0f; 
	[Export]
	public float StaminaRegenDelay = 2.0f; // O delay de 2s quando acaba
	
	public float CurrentHealth { get; private set; }
	public float CurrentStamina { get; private set; }
	
	private float _staminaRegenTimer = 0.0f; // Timer interno
	
	// --- SINAL PARA AVISAR O HUD ---
	[Signal]
	public delegate void StatsChangedEventHandler(float currentHealth, float maxHealth, float currentStamina, float maxStamina);
	
	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

	public override void _Ready()
	{
		CurrentHealth = MaxHealth;
		CurrentStamina = MaxStamina;
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// 1. Gravidade
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// 2. Pulo
		if (Input.IsActionJustPressed("ui_accept") && ndepulo > 0)
		{
			ndepulo -= 1; 
			velocity.Y = JumpVelocity;
		}
		if (IsOnFloor())
		{
			ndepulo = 1;
		}

		// --- (LÓGICA DE ESTAMINA E CORRIDA TOTALMENTE ATUALIZADA) ---
		
		bool wantsToSprint = Input.IsActionPressed("ui_corrida");
		float currentSpeed = Speed; // Começa com a velocidade NORMAL

		// 1. Contar o timer de delay (se estiver ativo)
		if (_staminaRegenTimer > 0)
		{
			_staminaRegenTimer -= (float)delta;
		}

		// 2. Lógica de Corrida (Gastar Estamina)
		// O jogador PODE tentar correr? (Se o delay de 2s não estiver ativo)
		bool canSprint = _staminaRegenTimer <= 0;

		if (wantsToSprint && canSprint)
		{
			// (ALTERADO) Calcula o gasto ANTES de verificar
			float staminaToDrain = StaminaDrainRate * (float)delta;

			// (ALTERADO) Verifica se temos estamina SUFICIENTE para este frame
			if (CurrentStamina > staminaToDrain)
			{
				// Sim, temos. Corra.
				currentSpeed = Speed * SprintMultiplier;
				CurrentStamina -= staminaToDrain;
			}
			else
			{
				// Não temos estamina suficiente.
				// Define a estamina como 0 e ATIVA o delay de 2s
				CurrentStamina = 0;
				_staminaRegenTimer = StaminaRegenDelay;
				// Nota: currentSpeed continua sendo a 'Speed' normal. O personagem para de correr.
			}
		}
		else
		{
			// 3. Lógica de Regeneração
			// REGENERA se:
			// A. O timer de delay acabou (<= 0)
			// B. A estamina não está cheia
			// C. (NOVO) O JOGADOR NÃO ESTÁ SEGURANDO O BOTÃO DE CORRIDA (!wantsToSprint)
			if (_staminaRegenTimer <= 0 && CurrentStamina < MaxStamina && !wantsToSprint)
			{
				CurrentStamina += StaminaRegenRate * (float)delta;
				CurrentStamina = Mathf.Min(CurrentStamina, MaxStamina); // Trava no máximo
			}
		}
		
		// 4. Handle Movimento Esquerda/Direita
		float horizontalDirection = Input.GetAxis("ui_left", "ui_right");

		if (horizontalDirection != 0)
		{
			// Aplica a velocidade (que será 'Speed' normal ou 'Sprint Speed')
			velocity.X = horizontalDirection * currentSpeed;
		}
		else
		{
			// Desaceleração (atrito)
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		// 5. Aplica a velocidade e move o personagem
		Velocity = velocity;
		MoveAndSlide();
		
		// 6. Emitir o Sinal (sempre no final)
		EmitSignal(SignalName.StatsChanged, CurrentHealth, MaxHealth, CurrentStamina, MaxStamina);
	}
}
