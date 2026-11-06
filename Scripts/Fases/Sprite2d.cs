using Godot;

public partial class Sprite2d : Sprite2D // O nome da classe deve ser igual ao nome do arquivo
{
	// Fator de parallax do fundo:
	//   0.0 = fundo colado na tela (distância infinita)
	//   1.0 = fundo acompanha o mundo normalmente
	// Valores pequenos (0.1 ~ 0.3) dão o efeito de profundidade.
	[Export]
	public float ParallaxFactor = 0.12f;

	public override void _Ready()
	{
		// O sprite usa "centered = false" (origem no canto superior esquerdo),
		// então posicionamos tudo manualmente no _Process.
		Centered = false;

		// Habilita repetição em ESPELHO da textura: quando o céu desliza
		// com o parallax, ele se repete infinitamente e as emendas
		// ficam quase invisíveis.
		TextureRepeat = TextureRepeatEnum.Mirror;
		RegionEnabled = true;
	}

	public override void _Process(double delta)
	{
		if (Texture == null) { return; }

		Camera2D camera = GetViewport().GetCamera2D();
		if (camera == null) { return; }

		Vector2 tamanhoTela = GetViewportRect().Size;
		Vector2 tamanhoDesenhado = Texture.GetSize() * Scale;

		// Garante uma região larga o bastante para cobrir a tela inteira
		// em qualquer fase do ciclo de repetição do parallax
		float larguraNecessaria = tamanhoTela.X + tamanhoDesenhado.X * 2.0f;
		if (RegionRect.Size.X < larguraNecessaria)
		{
			RegionRect = new Rect2(0, 0, larguraNecessaria, tamanhoDesenhado.Y);
		}

		// PARALLAX: o céu desliza devagar para o lado oposto da câmera.
		// PosMod garante que o ciclo de repetição nunca mostre lacunas.
		float deslocamentoX = Mathf.PosMod(
			camera.GetScreenCenterPosition().X * ParallaxFactor,
			tamanhoDesenhado.X
		);

		// Corrige o fundo descentralizado: centraliza verticalmente na tela
		// (antes a posição era o centro da tela, mas com centered = false
		// a imagem inteira ia para baixo/direita).
		Position = new Vector2(
			-deslocamentoX,
			(tamanhoTela.Y - tamanhoDesenhado.Y) / 2.0f
		);
	}
}
