using ICsDatasheetFinder_8._1.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ICsDatasheetFinder_8._1.Views
{
	/// <summary>
	/// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
	/// </summary>
	public sealed partial class MainView : Page, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public MainView()
		{
			this.InitializeComponent();
			_windowHelper = new WindowHelper(this)
			{
				States = new List<WindowState>()
				{
					new WindowState { State = "Vertical", MatchCriterium = (w, h) => h >= w },
					new WindowState { State = "Horizontal", MatchCriterium = (w, h) => h < w}
				}
			};

			_windowHelper.StateChanged += new StateChangedEventHandler((sender, state) =>
			{
				if (state.State == "Vertical")
				{
					HubView.Orientation = Orientation.Vertical;
					LogoVisibility = Visibility.Collapsed;
				}
				else if (state.State == "Horizontal")
				{
					HubView.Orientation = Orientation.Horizontal;
					LogoVisibility = Visibility.Visible;
				}
			});
		}

		public Visibility LogoVisibility
		{
			get
			{
				return _logoVisibility;
			}
			set
			{
				_logoVisibility = value;
				OnPropertyChanged();
			}
		}

		private void ManufacturersSemanticZoom_Loaded(object sender, RoutedEventArgs e)
		{
			// ZoomedOutView itemsource must be set in the code-behind to make semanticZoom navigation working
			((sender as SemanticZoom).ZoomedOutView as ListViewBase).ItemsSource = this.ManufacturerSource.View.CollectionGroups;
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void page_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is ViewModels.MainViewModel)
			{
				var VM = DataContext as ViewModels.MainViewModel;
				VM.PropertyChanged += new PropertyChangedEventHandler((sndr, args) =>
				{
					if (args.PropertyName == nameof(ViewModels.MainViewModel.Datasheets))
					{
						//if (!(VM.Datasheets.Count == 0 && VM.IsProcesssing))
						if (VM.Datasheets.Count > 0 && !VM.IsProcesssing)
						{
							NumberOfResults.Text = VM.Datasheets.Count.ToString();
							NumberOfResults.Visibility = Windows.UI.Xaml.Visibility.Visible;
						}
						else
							NumberOfResults.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
					}
					if (args.PropertyName == nameof(ViewModels.MainViewModel.IsMoreResult))
					{
						if (VM.IsMoreResult)
						{
							NumberOfResults.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
						}
					}
				});
			}
		}

		private Visibility _logoVisibility = Visibility.Visible;
		private WindowHelper _windowHelper;
	}
}
