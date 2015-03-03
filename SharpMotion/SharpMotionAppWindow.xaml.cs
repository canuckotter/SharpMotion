using System.Threading;
using System.Windows;

namespace SharpMotion
{
    /// <summary>
    /// Interaction logic for SharpMotionAppWindow.xaml
    /// </summary>
    public partial class SharpMotionAppWindow : Window
    {
        private SharpMotionViewModel _viewModel;

        public SharpMotionAppWindow()
        {
            _viewModel = new SharpMotionViewModel();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModelPropertyChanged;
            InitializeComponent();
        }

        void ViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentImage")
            {
                Dispatcher.Invoke( () =>
                ActiveImage.Source = _viewModel.CurrentImage);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Shutdown();
            base.OnClosing(e);
        }

        private void GoButton_OnClick(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentImage = null;
            ThreadPool.QueueUserWorkItem(state =>
                {
                    bool hasImage = false;
                    while (!hasImage)
                    {
                        Thread.Sleep(100);
                        hasImage = _viewModel.CurrentImage != null;
                    }

                    Dispatcher.Invoke(() =>
                        {
                            ActiveImage.Height = _viewModel.CurrentImage.Height;
                            ActiveImage.Width = _viewModel.CurrentImage.Width;
                        });
                });
        }
    }
}
