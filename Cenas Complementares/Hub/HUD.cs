using Godot;
using System; // Necessário para Mathf.Round

public partial class HUD : CanvasLayer
{
	private TextureProgressBar _healthBar;
	private AnimatedSprite2D _staminaBarAnimated; 

	public override void _Ready()
	{
		_healthBar = GetNode<TextureProgressBar>("HealthBar");
		
		// Pega o nó. Lembre-se de usar o nome exato!
		_staminaBarAnimated = GetNode<AnimatedSprite2D>("StaminaBarAnimated");
		
		// Garante que a animação correta está selecionada
		_staminaBarAnimated.Animation = "level";
		_staminaBarAnimated.Stop(); // Garante que está parado
	}

	public void OnPlayerStatsChanged(float currentHealth, float maxHealth, float currentStamina, float maxStamina)
	{
		// Lógica da Vida (continua igual)
		_healthBar.MaxValue = maxHealth;
		_healthBar.Value = currentHealth;
		
		// --- (LÓGICA DA ESTAMINA COM 35 FRAMES) ---

		// 1. Calcula a porcentagem de estamina (de 0.0 a 1.0)
		//    (Usamos maxStamina para o caso de você querer mudar de 100 depois)
		float staminaPercent = currentStamina / maxStamina;

		// 2. Pega o número total de frames
		//    (GetFrameCount("level") vai retornar 35)
		int totalFrames = _staminaBarAnimated.SpriteFrames.GetFrameCount("level");

		// 3. Converte a porcentagem para o índice do frame (de 0 a 34)
		//    (NOVO) Invertemos a matemática, já que o seu Frame 0 é "cheio"
		//
		//    Exemplo CHEIO:  Percent = 1.0 -> (1.0 * 34) = 34. -> Invertido (34 - 34) = Frame 0 (Correto!)
		//    Exemplo VAZIO:  Percent = 0.0 -> (0.0 * 34) = 0.  -> Invertido (34 - 0)  = Frame 34 (Correto!)
		//    Exemplo MEIO:   Percent = 0.5 -> (0.5 * 34) = 17.  -> Invertido (34 - 17) = Frame 17 (Correto!)
		
		int frameIndex = (int)Mathf.Round(staminaPercent * (totalFrames - 1));
		
		// (NOVO) Inverte o índice
		int invertedFrameIndex = (totalFrames - 1) - frameIndex;
		
		// 4. Garante que o frame não saia dos limites (segurança)
		invertedFrameIndex = Mathf.Clamp(invertedFrameIndex, 0, totalFrames - 1);

		// 5. Define o frame!
		_staminaBarAnimated.Frame = invertedFrameIndex;
	}
}
