using Godot;

public partial class HUD : CanvasLayer
{
	// --- DECLARAÇÕES DAS VARIÁVEIS ---
	// Elas precisam estar AQUI, no topo da classe,
	// para que TODOS os métodos (como _Ready e OnPlayerStatsChanged)
	// possam "vê-las".
	// ------------------------------------
	private TextureProgressBar _healthBar;
	private TextureProgressBar _staminaBar;
	private AnimatedSprite2D _staminaDrainFX; 
	private float _lastStaminaValue = -1f; 
	private Texture2D _staminaBarOriginalTexture;

	// _Ready é chamado uma vez quando a cena é iniciada
	public override void _Ready()
	{
		// Aqui nós "pegamos" os nós e os guardamos nas variáveis
		_healthBar = GetNode<TextureProgressBar>("HealthBar");
		
		// Lembre-se, seu nó se chama "staminaBar" (minúsculo), como na sua foto!
		_staminaBar = GetNode<TextureProgressBar>("staminaBar"); 
		
		// Verifique se o caminho "staminaBar/StaminaDrainFX" está 100% correto
		// Se o seu AnimatedSprite2D se chamar "staminaDrainFX" (minúsculo), mude aqui.
		_staminaDrainFX = GetNode<AnimatedSprite2D>("staminaBar/StaminaDrainFX");
		
		// Converte o MaxValue para float
		_lastStaminaValue = (float)_staminaBar.MaxValue; 
		
		// Guarda a textura "básica" (o preenchimento) para usarmos depois
		_staminaBarOriginalTexture = _staminaBar.TextureProgress;
	}

	// OnPlayerStatsChanged é chamado a cada frame pelo sinal da Erika
	public void OnPlayerStatsChanged(float currentHealth, float maxHealth, float currentStamina, float maxStamina)
	{
		// Como as variáveis foram declaradas no topo, este método pode usá-las.
		
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
		
		_staminaBar.MaxValue = maxStamina; 
		_staminaBar.Value = currentStamina;

		// --- LÓGICA DE ANIMAÇÃO ATUALIZADA ---

		bool isDraining = currentStamina < _lastStaminaValue;
		bool isNotDraining = currentStamina > _lastStaminaValue || currentStamina == 0 || currentStamina == maxStamina;

		if (isDraining)
		{
			// ESCONDE a barra básica
			_staminaBar.TextureProgress = null; 
			
			// MOSTRA a animação personalizada
			_staminaDrainFX.Visible = true;
			
			if (!_staminaDrainFX.IsPlaying())
			{
				_staminaDrainFX.Play("drain");
			}
		}
		else if (isNotDraining)
		{
			// MOSTRA a barra básica de novo
			_staminaBar.TextureProgress = _staminaBarOriginalTexture;
			
			// ESCONDE a animação personalizada
			_staminaDrainFX.Stop();
			_staminaDrainFX.Visible = false;
		}

		_lastStaminaValue = currentStamina;
	}
}
