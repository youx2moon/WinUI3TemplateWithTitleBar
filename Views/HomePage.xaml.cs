using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace $safeprojectname$.Views
{
    public sealed partial class HomePage : Page
    {
        public ViewModels.MainViewModel ViewModel { get; private set; } = null!;

        public HomePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ViewModels.MainViewModel vm)
            {
                ViewModel = vm;
            }
            base.OnNavigatedTo(e);
        }
    }
}