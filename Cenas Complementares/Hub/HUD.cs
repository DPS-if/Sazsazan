// HUD.cs (Inalterado)
using Godot;
using System; // Necessário para Mathf.Round

public partial class HUD : CanvasLayer
{
	private TextureProgressBar _healthBar;
	private AnimatedSprite2D _staminaBarAnimated; 

	public override void _Ready()
	{
		_healthBar = GetNode<TextureProgressBar>("HealthBar");
		_staminaBarAnimated = GetNode<AnimatedSprite2D>("StaminaBarAnimated");
		
		// Garante que a animação correta está selecionada
		_staminaBarAnimated.Animation = "level";
		_staminaBarAnimated.Stop();
		
		// Conexão CRÍTICA (Você deve fazer isso no Godot Editor uma vez)
		// var player = GetTree().Root.FindChild("Erika", true, false) as Erika;
		// if (player != null) { player.StatsChanged += OnPlayerStatsChanged; }
	}

	public void OnPlayerStatsChanged(float currentHealth, float maxHealth, float currentStamina, float maxStamina)
	{
		// Lógica da Vida (progress bar)
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
		
		// --- (LÓGICA DA ESTAMINA COM 35 FRAMES) ---
		float staminaPercent = currentStamina / maxStamina;
		int totalFrames = _staminaBarAnimated.SpriteFrames.GetFrameCount("level");

		// Calcula e inverte o frame
		int frameIndex = (int)Mathf.Round(staminaPercent * (totalFrames - 1));
		int invertedFrameIndex = (totalFrames - 1) - frameIndex;
		
		invertedFrameIndex = Mathf.Clamp(invertedFrameIndex, 0, totalFrames - 1);

		_staminaBarAnimated.Frame = invertedFrameIndex;
	}
}
