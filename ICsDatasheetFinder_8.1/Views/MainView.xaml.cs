using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using ICsDatasheetFinder_8._1;
using ICsDatasheetFinder_8._1.Common;
// Pour en savoir plus sur le modèle d'élément Page vierge, consultez la page http://go.microsoft.com/fwlink/?LinkId=234238

namespace ICsDatasheetFinder_8._1.Views
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class MainView : Page
    {
        public MainView()
        {
            this.InitializeComponent();

            _windowHelper = new WindowHelper(this)
            {
                States = new List<WindowState>()
                {
                    new WindowState { State = "Vertical", MatchCriterium = (w, h) => h > w },
                    new WindowState { State = "Horizontal", MatchCriterium = (w, h) => h <= w}
                }
            };
        }

        private void WatermarkTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(FirstTime)
            {
                HubView.ScrollToSection(InputHubSection);
                FirstTime = false;
            }
        }

        private void ManufacturersSemanticZoom_Loaded(object sender, RoutedEventArgs e)
        {
            // ZoomedOutView itemsource must be set in the code-behind to make semanticZoom navigation working
            ((sender as SemanticZoom).ZoomedOutView as ListViewBase).ItemsSource = this.ManufacturerSource.View.CollectionGroups;
        }
        
        private WindowHelper _windowHelper;
        
        private bool FirstTime = true;

        private void WatermarkTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as Callisto.Controls.WatermarkTextBox).Focus(FocusState.Programmatic);
        }
    }
}
