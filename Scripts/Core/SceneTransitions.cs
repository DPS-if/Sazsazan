using Godot;
using System.Threading.Tasks;

/// <summary>
/// Autoload (singleton) responsável por transições suaves (fade) entre cenas.
/// Registrado no project.godot na seção [autoload].
/// Também guarda a última fase visitada, para que a Tela de Morte
/// renasça o jogador na fase correta.
/// </summary>
public partial class SceneTransitions : CanvasLayer
{
	public static SceneTransitions Instance { get; private set; }

	/// <summary>Caminho da última fase em que o jogador estava (usado ao renascer).</summary>
	public static string LastLevelPath { get; private set; } = "res://Cenas/Fases/Ato1.tscn";

	private const string DeathScenePath = "res://Cenas/UI/telaMorte.tscn";

	private ColorRect _fadeRect;
	private bool _transitioning;

	public override void _Ready()
	{
		Instance = this;
		Layer = 100; // Sempre por cima de tudo
		ProcessMode = Node.ProcessModeEnum.Always; // Funciona mesmo com o jogo pausado

		_fadeRect = new ColorRect
		{
			Color = new Color(0, 0, 0, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			AnchorRight = 1.0f,
			AnchorBottom = 1.0f,
			Visible = false,
		};
		AddChild(_fadeRect);
	}

	/// <summary>
	/// Troca de cena com fade para preto e volta.
	/// </summary>
	public void ChangeScene(string scenePath, float fadeTime = 0.35f)
	{
		if (_transitioning || string.IsNullOrEmpty(scenePath))
		{
			return;
		}
		_transitioning = true;
		_ = RunTransition(scenePath, fadeTime, rememberAsLevel: false);
	}

	/// <summary>
	/// Chamado quando a Erika morre: lembra a fase atual e vai para a Tela de Morte.
	/// </summary>
	public void PlayerDied(float fadeTime = 0.6f)
	{
		var current = GetTree().CurrentScene;
		if (current != null && current.SceneFilePath != DeathScenePath)
		{
			LastLevelPath = current.SceneFilePath;
		}
		ChangeScene(DeathScenePath, fadeTime);
	}

	private async Task RunTransition(string scenePath, float fadeTime, bool rememberAsLevel)
	{
		// Bloqueia cliques durante o fade
		_fadeRect.MouseFilter = Control.MouseFilterEnum.Stop;
		_fadeRect.Visible = true;

		Tween fadeIn = CreateTween();
		fadeIn.TweenProperty(_fadeRect, "color:a", 1.0f, fadeTime);
		await ToSignal(fadeIn, Tween.SignalName.Finished);

		GetTree().ChangeSceneToFile(scenePath);

		Tween fadeOut = CreateTween();
		fadeOut.TweenProperty(_fadeRect, "color:a", 0.0f, fadeTime);
		await ToSignal(fadeOut, Tween.SignalName.Finished);

		_fadeRect.Visible = false;
		_fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		_transitioning = false;
	}
}
