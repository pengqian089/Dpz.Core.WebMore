namespace Dpz.Core.WebMore.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var mainPage = new MainPage();
        var nav = new NavigationPage(mainPage);
        NavigationPage.SetHasNavigationBar(mainPage, false);
        return new Window(nav) { Title = "Dpz.Core" };
    }
}