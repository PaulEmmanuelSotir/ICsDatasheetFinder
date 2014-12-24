using Caliburn.Micro;
using ICsDatasheetFinder.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ICsDatasheetFinder.ViewModels
{
	public sealed class MainViewModel : ViewModelBase
	{
		public MainViewModel(INavigationService navigationService)
			: base(navigationService)
		{
			Manufacturers = new BindableCollection<IGrouping<char, Manufacturer>>();
			_selectedManufacturers = new List<Manufacturer>();
			_datasheets = new HashSet<Part>();

			// Update found datasheets if query or selected manufacturers changed
			this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler((obj, Args) =>
			{
				if (Args.PropertyName == nameof(ManufacturerSelectionEnabled) || Args.PropertyName == nameof(_selectedManufacturers) || Args.PropertyName == nameof(Query))
					QueryForDatasheets();
			});
		}

		protected override async void OnInitialize()
		{
			base.OnInitialize();

			// If page have been launched by search panel we query database. Results are automaticly updated (see MainViewModel constructor) so we have to be aware that
			// manufacturers will not be used in 'QueryForDatasheets' as they aren't loaded yet.
			Query = Parameter ?? "";

			await Task.Factory.StartNew(() =>
			{
				Manufacturers.AddRange(DatasheetDataSource.GetManufacturers());
				// Get ungrouped manufacters
				_ungroupedManufacters = (from manusGroupedByLetter in Manufacturers from manu in manusGroupedByLetter select manu).ToList<Manufacturer>();
			});

			// Load manufacturers logos
			await DatasheetDataSource.LoadManufacturersImagesAsync();
		}

		/// <summary>
		/// Navigates to DatasheetView in order to display datasheet.
		/// </summary>
		private void SeeDatasheet(ItemClickEventArgs e)
		{
			Part part = e.ClickedItem as Part;
			navigationService.UriFor<DatasheetViewModel>()
				.WithParam<String>((instance) => instance.DatasheetURL, part.datasheetURL)
				.WithParam<String>((instance) => instance.PartReference, part.reference)
				.Navigate();
		}

		/// <summary>
		/// Ask windows store to display Electronic database app sheet
		/// </summary>
		private async void SeeElecDatabase()
		{
			await Launcher.LaunchUriAsync(new Uri("ms-windows-store:PDP?PFN=45311Paul-EmmanuelSotir.ElectronicDatabase_7q75p07zxm5km"));
		}

		// TODO : quand on recherche avec tout les résultats, conserver seulement les manufacturers concernés
		/// <summary>
		/// Searches matching part datasheet in SQLite database. 
		/// The criteria used to query the database are the selected manufacturers and the user query (from 'selectedManufacturers' and 'Query').
		/// </summary>
		private async void QueryForDatasheets()
		{
			// Cancel old datasheet queries and create a new CancellationTokenSource
			if (_previousTokenSource != null)
				_previousTokenSource.Cancel(true);
			// TODO : wait for the canceled task (task cancelling is not instantaneous !)
			_currentTokenSource = new CancellationTokenSource();
			_previousTokenSource = _currentTokenSource;

			IsZeroManufacturerSelected = false;
			IsEmptyResult = false;
			// TODO : règler le problème d'affichage de "0 datasheet trouvées" pendant la recherche ! (cf TODO dans le XAML)
			IsMoreResult = false;

			try
			{
				if (IsAnyQuery)
				{
					_currentTokenSource.Token.ThrowIfCancellationRequested();
					IsProcesssing = true;

					if (_datasheets.Count != 0)
					{
						_datasheets.Clear();
						NotifyOfPropertyChange<HashSet<Part>>(() => Datasheets);
						NotifyOfPropertyChange<int>(() => DatasheetsCount);
					}

					IList<Part> data = await Task.Factory.StartNew(() =>
					{
						if (ManufacturerSelectionEnabled)
							return DatasheetDataSource.SearchForDatasheet(Query, _currentTokenSource.Token, _selectedManufacturers, FIRST_SEARCH_RANGE);
						return DatasheetDataSource.SearchForDatasheet(Query, _currentTokenSource.Token, FIRST_SEARCH_RANGE);
					}, _currentTokenSource.Token);

					_currentTokenSource.Token.ThrowIfCancellationRequested();

					Datasheets.UnionWith(data);
					NotifyOfPropertyChange(nameof(Datasheets));
					NotifyOfPropertyChange(nameof(DatasheetsCount));
					// Preparation de l'incremental loading et affichage de la première page.
					ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), _ungroupedManufacters);
					IsMoreResult = Datasheets.Count == FIRST_SEARCH_RANGE;

					_currentTokenSource.Token.ThrowIfCancellationRequested();
					await ViewDatasheets.LoadMoreItemsAsync(FIRST_SEARCH_RANGE);
					_currentTokenSource.Token.ThrowIfCancellationRequested();

					IsProcesssing = false;
				}

				_currentTokenSource.Token.ThrowIfCancellationRequested();

				if (Datasheets.Count == 0)
					if (ManufacturerSelectionEnabled && _selectedManufacturers.Count == 0)
						IsZeroManufacturerSelected = true;
					else
						IsEmptyResult = true;
				else
				{
					IsZeroManufacturerSelected = false;
					IsEmptyResult = false;
				}
			}
			catch (OperationCanceledException) { }
		}

		/// <summary>
		/// Manually synchronizes selected manufacturers of view and the 'selectedManufacturers' collection.
		/// </summary>
		private void UpdateManufacturerSelection(SelectionChangedEventArgs e)
		{
			// Boolean used to call 'NotifyOfPropertyChange' once
			bool selectedManufacturersUpdated = false;

			foreach (Manufacturer manu in e.AddedItems)
				if (!_selectedManufacturers.Contains(manu))
				{
					_selectedManufacturers.Add(manu);
					selectedManufacturersUpdated = true;
				}

			foreach (Manufacturer manu in e.RemovedItems)
				if (_selectedManufacturers.Contains(manu))
				{
					_selectedManufacturers.Remove(manu);
					selectedManufacturersUpdated = true;
				}

			if (selectedManufacturersUpdated)
				NotifyOfPropertyChange(nameof(_selectedManufacturers));
		}

		public void UserQueryChanged(object sender)
		{
			// Doesn't throws exceptions: if(sender == null) => (sender is TextBox) == false
			if (sender is TextBox)
			{
				string input = (sender as TextBox).Text;

				// Avoid SQLite misunderstanding due to ' character in the query
				if (input.Contains('\''))
					input = input.Replace("\'", "");
				Query = input.Trim();
			}
		}

		private async void FindMoreResults()
		{
			// Prevent any double task
			_previousTokenSource?.Cancel(true);
			//TODO : wait the canceled task.
			_currentTokenSource = new CancellationTokenSource();
			_previousTokenSource = _currentTokenSource;

			try
			{
				_currentTokenSource.Token.ThrowIfCancellationRequested();
				IsProcesssing = true;

				var data = await Task.Factory.StartNew(() =>
				{
					if (ManufacturerSelectionEnabled)
						return DatasheetDataSource.SearchForDatasheet(Query, _currentTokenSource.Token, _selectedManufacturers);
					return DatasheetDataSource.SearchForDatasheet(Query, _currentTokenSource.Token);
				}, _currentTokenSource.Token);

				_currentTokenSource.Token.ThrowIfCancellationRequested();

				_datasheets.Clear();
				Datasheets.UnionWith(data);
				NotifyOfPropertyChange(nameof(Datasheets));
				NotifyOfPropertyChange(nameof(DatasheetsCount));
				ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), _ungroupedManufacters);
				IsMoreResult = false;

				_currentTokenSource.Token.ThrowIfCancellationRequested();
				await ViewDatasheets.LoadMoreItemsAsync(120);
				_currentTokenSource.Token.ThrowIfCancellationRequested();

				IsProcesssing = false;
			}
			catch (OperationCanceledException) { }
		}

		private void QueryTextBox_Loaded(object sender)
		{
			var txtBox = (sender as TextBox);
			// Ecrit la requete issue du search panel dans la TextBox
			if (Query != string.Empty)
			{
				txtBox.Text = Query;
				// Place le curseur à la fin du texte
				txtBox.Select(txtBox.Text.Length, 0);
			}
			// TODO : vérifier que PreventKeyboardDisplayOnProgrammaticFocus ne doit pas être plutôt mit à true !
			txtBox.PreventKeyboardDisplayOnProgrammaticFocus = false;
			txtBox.Focus(FocusState.Programmatic);
		}

		#region Properties

		public BindableCollection<IGrouping<char, Manufacturer>> Manufacturers { get; private set; }

		// TODO : tester le search panel à la fin
		/// <summary>
		/// Parameter is a string representing a query from the search panel.
		/// The name 'Parameter' is automatically recognized and initialized by caliburn micro.
		/// </summary>
		public string Parameter
		{
			get
			{
				return _parameter;
			}
			set
			{
				_parameter = value;
				NotifyOfPropertyChange();
			}
		}

		public string Query
		{
			get
			{
				return _query;
			}
			private set
			{
				if (value != _query)
				{
					_query = value;

					NotifyOfPropertyChange();
					NotifyOfPropertyChange(nameof(IsAnyQuery));
				}
			}
		}

		public bool IsAnyQuery => (_query?.Length ?? 0) > 0;

		public bool IsMoreResult
		{
			get
			{
				return _isMoreResult;
			}
			set
			{
				_isMoreResult = value;
				NotifyOfPropertyChange();
			}
		}

		public bool IsEmptyResult
		{
			get
			{
				return _isEmptyResult && !_isZeroManufacturerSelected;
			}
			set
			{
				_isEmptyResult = value;
				NotifyOfPropertyChange();
			}
		}

		public bool IsZeroManufacturerSelected
		{
			get
			{
				return _isZeroManufacturerSelected && !_isEmptyResult;
			}
			set
			{
				_isZeroManufacturerSelected = value;
				NotifyOfPropertyChange();
			}
		}

		public bool IsProcesssing
		{
			get
			{
				return _isProcessing;
			}
			set
			{
				_isProcessing = value;
				NotifyOfPropertyChange();
			}
		}

		public int DatasheetsCount => _datasheets.Count;

		public HashSet<Part> Datasheets
		{
			get
			{
				return _datasheets;
			}
			private set
			{
				_datasheets = value;

				NotifyOfPropertyChange();
				NotifyOfPropertyChange(nameof(DatasheetsCount));
			}
		}

		public Common.IncrementalLoadingDatasheetList ViewDatasheets
		{
			get
			{
				return _viewDatasheets;
			}
			private set
			{
				_viewDatasheets = value;
				NotifyOfPropertyChange();
			}
		}

		public bool ManufacturerSelectionEnabled
		{
			get
			{
				return _manufacturerSelectionEnabled;
			}
			set
			{
				_manufacturerSelectionEnabled = value;
				NotifyOfPropertyChange();
			}
		}

		#endregion

		#region Members

		private const int FIRST_SEARCH_RANGE = 120;

		private string _parameter;
		private string _query;
		private bool _isMoreResult = false;
		private bool _isEmptyResult = false;
		private bool _isZeroManufacturerSelected = false;
		private bool _isProcessing = false;
		private HashSet<Part> _datasheets;
		private Common.IncrementalLoadingDatasheetList _viewDatasheets;
		private bool _manufacturerSelectionEnabled;
		private List<Manufacturer> _selectedManufacturers;
		private List<Manufacturer> _ungroupedManufacters;


		// Cancellation Token used to cancel unnecessary/old datasheet queries.
		private CancellationTokenSource _currentTokenSource = new CancellationTokenSource();
		private CancellationTokenSource _previousTokenSource;

		#endregion
	}
}
