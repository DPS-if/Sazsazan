using Godot;

public partial class SkyFollow : Sprite2D
{
	public override void _Ready()
	{
		// 1. Centraliza o sprite quando o jogo começa
		CenterSprite();

		// 2. Conecta ao sinal 'size_changed' do Viewport.
		//    Quando a janela mudar de tamanho, a função 'CenterSprite' será chamada.
		GetViewport().SizeChanged += OnViewportSizeChanged;
	}

	private void CenterSprite()
	{
		// Centraliza este sprite no meio da tela (viewport).
		this.GlobalPosition = GetViewportRect().Size / 2.0f;
	}

	private void OnViewportSizeChanged()
	{
		// Chama a mesma função de centralizar novamente
		CenterSprite();
	}
	
	// Opcional: Desconectar o sinal ao sair da cena para evitar erros
	public override void _ExitTree()
	{
		GetViewport().SizeChanged -= OnViewportSizeChanged;
	}
}
