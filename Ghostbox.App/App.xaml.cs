using Microsoft.Extensions.DependencyInjection;

namespace Ghostbox.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
        UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var page = new MainPage();
		var window = new Window(page) { Title = "Whispers of the Spirit | DEVELOPMENT PREVIEW", Width = 1600, Height = 1000, MinimumWidth = 1240, MinimumHeight = 760 };
		window.Stopped += (_, _) => page.StopScanning();
		window.Destroying += (_, _) => page.StopScanning();
		return window;
	}
}
