using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Rio.CleanupSolutionConfiguration.Logic;

namespace Rio.CleanupSolutionConfiguration.Windows
{
    /// <summary>
    /// Interaction logic for CleanupSolutionConfigurationPrompt.xaml
    /// </summary>
    public partial class CleanupSolutionConfigurationPrompt : Window, INotifyPropertyChanged
    {
        private FrameworkName _frameworkName;

        private string _productName;

        public CleanupSolutionConfigurationPrompt()
        {
            InitializeComponent();
        }

        public CleanupSolutionConfigurationPrompt(string visualStudioVersion, string currentFrameworkName, string currentProductName,
            bool showNETFrameworkVersion, bool showProductName)
            : base()
        {
            if (!showNETFrameworkVersion)
                wrapPanelTargetFramework.Visibility = Visibility.Hidden;
            else
            {
                FrameworkVersions frameworkVersions = new FrameworkVersions(visualStudioVersion);
                comboTargetFramework.ItemsSource = frameworkVersions.GetFrameworks();

                FrameworkName = frameworkVersions.GetFrameworkByFullname(currentFrameworkName);
            }

            if (!showProductName)
                wrapPanelProductName.Visibility = Visibility.Hidden;
            else
                ProductName = currentProductName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public FrameworkName FrameworkName
        {
            get
            {
                return _frameworkName;
            }
            set
            {
                _frameworkName = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FrameworkName"));
            }
        }

        public string ProductName
        {
            get
            {
                return _productName;
            }
            set
            {
                _productName = value;
                PropertyChanged(this, new PropertyChangedEventArgs("ProductName"));
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            this.Close();
        }

        private void buttonCleanup_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}