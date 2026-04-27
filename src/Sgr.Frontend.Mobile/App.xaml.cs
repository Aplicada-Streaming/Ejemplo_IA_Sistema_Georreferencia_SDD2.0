namespace Sgr.Frontend.Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // MAUI 10: en lugar de setear MainPage en el constructor (obsoleto),
    // se sobreescribe CreateWindow para inicializar la ventana raíz.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new MainPage());
}
