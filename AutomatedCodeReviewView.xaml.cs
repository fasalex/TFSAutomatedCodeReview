using System.Windows;

namespace AutomatedCodeReview
{
    /// <summary>
    ///     Interaction logic for AutomatedCodeReview.xaml
    /// </summary>
    public partial class AutomatedCodeReviewView
    {
        public bool IsChecked { get; private set; }

        public AutomatedCodeReviewView()
        {
            InitializeComponent();
        }

        private void OnChecked(object sender, RoutedEventArgs e)
        {
            IsChecked = true;
        }

        private void OnUnchecked(object sender, RoutedEventArgs e)
        {
            IsChecked = false;
        }
    }
}