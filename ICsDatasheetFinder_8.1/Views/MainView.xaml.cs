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
        }

        // TODO : enlever tous ça !!
      //  private Common.IncrementalyLoadingPartList Datasheets;
        private void WatermarkTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // EnableManufacturerSelection.IsOn
       /*     if (Datasheets == null)
                Datasheets = new Common.IncrementalyLoadingPartList();
            Datasheets.reset((sender as Callisto.Controls.WatermarkTextBox).Text);
            DatasheetSource.Source = Datasheets;*/
        }

        private void ManufacturersSemanticZoom_Loaded(object sender, RoutedEventArgs e)
        {
            // ZoomedOutView itemsource must be set in the code-behind to make semanticZoom navigation working
            ((sender as SemanticZoom).ZoomedOutView as ListViewBase).ItemsSource = this.ManufacturerSource.View.CollectionGroups;
        }
    }
}
