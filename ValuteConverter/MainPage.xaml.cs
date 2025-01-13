namespace ValuteConverter
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            DateSelector.MaximumDate = DateTime.Today;
        }
    }
}