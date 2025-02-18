namespace DigiLimb
{
    public partial class EmulationPage : ContentPage
    {
        public EmulationPage()
        {
            InitializeComponent();

#if WINDOWS
            Title = "Emulation";
            TitleLabel.Text = "Emulation Page";
#else
            Title = "Emulators";
            TitleLabel.Text = "Emulators";
#endif
        }
    }
}
