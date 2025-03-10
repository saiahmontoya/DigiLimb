namespace DigiLimbMobile;

public partial class EmulatorPage : ContentPage
{
    public EmulatorPage()
    {
        InitializeComponent();
    }

    private async void OnMouseClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MousePage());
    }

    private async void OnKeyboardClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new KeyboardPage());
    }
}