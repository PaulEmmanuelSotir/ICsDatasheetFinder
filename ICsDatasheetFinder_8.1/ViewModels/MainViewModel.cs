using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Xaml.Controls;
using Windows.System;
using Caliburn.Micro;
using ICsDatasheetFinder_8._1.Data;
using Windows.UI.Xaml;

namespace ICsDatasheetFinder_8._1.ViewModels
{
	public sealed class MainViewModel : ViewModelBase
	{
		public MainViewModel(INavigationService navigationService)
			: base(navigationService)
		{
			Manufacturers = new BindableCollection<IGrouping<char, Manufacturer>>();
			selectedManufacturers = new List<Manufacturer>();
			datasheets = new HashSet<Part>();

			// Update found datasheets if query or selected manufacturers changed
			this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler((obj, Args) =>
			{
				if (Args.PropertyName == "ManufacturerSelectionEnabled" || Args.PropertyName == "selectedManufacturers" || Args.PropertyName == "Query")
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
				ungroupedManufacters = (from manusGroupedByLetter in Manufacturers from manu in manusGroupedByLetter select manu).ToList<Manufacturer>();
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
			if (PreviousTokenSource != null)
				PreviousTokenSource.Cancel(true);
			// TODO : wait for the canceled task (task cancelling is not instantaneous !)
			CurrentTokenSource = new CancellationTokenSource();
			PreviousTokenSource = CurrentTokenSource;

			IsZeroManufacturerSelected = false;
			IsEmptyResult = false;
			// TODO : règler le problème d'affichage de "0 datasheet trouvées" pendant la recherche ! (cf TODO dans le XAML)
			IsMoreResult = false;

			try
			{
				if (IsAnyQuery)
				{
					CurrentTokenSource.Token.ThrowIfCancellationRequested();
					IsProcesssing = true;

					if (datasheets.Count != 0)
					{
						datasheets.Clear();
						NotifyOfPropertyChange<int>(() => DatasheetsCount);
					}

					IList<Part> data = await Task.Factory.StartNew(() =>
					{
						if (ManufacturerSelectionEnabled)
							return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, selectedManufacturers, FIRST_SEARCH_RANGE);
						return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, FIRST_SEARCH_RANGE);
					}, CurrentTokenSource.Token);

					CurrentTokenSource.Token.ThrowIfCancellationRequested();

					Datasheets.UnionWith(data);
					NotifyOfPropertyChange<int>(() => DatasheetsCount);
					// preparation de l'incremental loading et affichage de la première page.
					ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), ungroupedManufacters);
					IsMoreResult = Datasheets.Count == FIRST_SEARCH_RANGE;

					CurrentTokenSource.Token.ThrowIfCancellationRequested();
					await ViewDatasheets.LoadMoreItemsAsync(FIRST_SEARCH_RANGE);
					CurrentTokenSource.Token.ThrowIfCancellationRequested();

					IsProcesssing = false;
				}

				CurrentTokenSource.Token.ThrowIfCancellationRequested();
				if (Datasheets.Count == 0)
					if (ManufacturerSelectionEnabled && selectedManufacturers.Count == 0)
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
			foreach (Manufacturer manu in e.AddedItems)
				if (!selectedManufacturers.Contains(manu))
					selectedManufacturers.Add(manu);

			foreach (Manufacturer manu in e.RemovedItems)
				if (selectedManufacturers.Contains(manu))
					selectedManufacturers.Remove(manu);
		}

		public void UserQueryChanged(object sender)
		{
			// doesn't throws exceptions: if(sender == null) => (sender is TextBox) == false
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
			// prevent any double task
			if (PreviousTokenSource != null)
				PreviousTokenSource.Cancel(true);
			//TODO : wait the canceled task.
			CurrentTokenSource = new CancellationTokenSource();
			PreviousTokenSource = CurrentTokenSource;

			try
			{
				CurrentTokenSource.Token.ThrowIfCancellationRequested();
				IsProcesssing = true;

				var data = await Task.Factory.StartNew(() =>
				{
					if (ManufacturerSelectionEnabled)
						return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, selectedManufacturers);
					return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token);
				}, CurrentTokenSource.Token);

				CurrentTokenSource.Token.ThrowIfCancellationRequested();

				datasheets.Clear();
				Datasheets.UnionWith(data);
				NotifyOfPropertyChange<int>(() => DatasheetsCount);
				ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), ungroupedManufacters);
				IsMoreResult = false;

				CurrentTokenSource.Token.ThrowIfCancellationRequested();
				await ViewDatasheets.LoadMoreItemsAsync(120);
				CurrentTokenSource.Token.ThrowIfCancellationRequested();

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

		#region Attributes

		public BindableCollection<IGrouping<char, Manufacturer>> Manufacturers
		{
			get;
			private set;
		}
		private List<Manufacturer> ungroupedManufacters
		{
			get;
			set;
		}

		// TODO : tester le search panel à la fin
		/// <summary>
		/// Parameter is a string representing a query from the search panel.
		/// The name 'Parameter' is automatically recognized and initialized by caliburn micro.
		/// </summary>
		public string Parameter
		{
			get
			{
				return parameter;
			}
			set
			{
				parameter = value;
				NotifyOfPropertyChange("Parameter");
			}
		}
		private string parameter;

		public string Query
		{
			get
			{
				return query;
			}
			private set
			{
				if (value != query)
				{
					query = value;
					// TODO : comprendre lequel des deux types de notifications est le meilleur :
					NotifyOfPropertyChange("Query");
					//NotifyOfPropertyChange<string>(() => Query);
					NotifyOfPropertyChange<bool>(() => IsAnyQuery);
				}
			}
		}
		private string query;
		public bool IsAnyQuery
		{
			get
			{
				if (query != null)
					return query.Length > 0;
				return false;
			}
		}

		public bool IsMoreResult
		{
			get
			{
				return isMoreResult;
			}
			set
			{
				isMoreResult = value;
				NotifyOfPropertyChange<bool>(() => IsMoreResult);
			}
		}
		private bool isMoreResult = false;
		public bool IsEmptyResult
		{
			get
			{
				return isEmptyResult && !isZeroManufacturerSelected;
			}
			set
			{
				isEmptyResult = value;
				NotifyOfPropertyChange<bool>(() => IsEmptyResult);
			}
		}
		private bool isEmptyResult = false;

		private bool isZeroManufacturerSelected = false;
		public bool IsZeroManufacturerSelected
		{
			get
			{
				return isZeroManufacturerSelected && !isEmptyResult;
			}
			set
			{
				isZeroManufacturerSelected = value;
				NotifyOfPropertyChange<bool>(() => IsZeroManufacturerSelected);
			}
		}

		public bool IsProcesssing
		{
			get
			{
				return isProcessing;
			}
			set
			{
				isProcessing = value;
				NotifyOfPropertyChange<bool>(() => IsProcesssing);
			}
		}
		private bool isProcessing = false;

		public int DatasheetsCount
		{
			get
			{
				return datasheets.Count;
			}
		}
		public HashSet<Part> Datasheets
		{
			get
			{
				return datasheets;
			}
			private set
			{
				datasheets = value;
				NotifyOfPropertyChange<HashSet<Part>>(() => Datasheets);
				NotifyOfPropertyChange<int>(() => DatasheetsCount);
			}
		}
		private HashSet<Part> datasheets;
		public Common.IncrementalLoadingDatasheetList ViewDatasheets
		{
			get
			{
				return viewDatasheets;
			}
			private set
			{
				viewDatasheets = value;
				NotifyOfPropertyChange<Common.IncrementalLoadingDatasheetList>(() => ViewDatasheets);
			}
		}
		private Common.IncrementalLoadingDatasheetList viewDatasheets;
		public bool ManufacturerSelectionEnabled
		{
			get
			{
				return manufacturerSelectionEnabled;
			}
			set
			{
				manufacturerSelectionEnabled = value;
				NotifyOfPropertyChange<bool>(() => ManufacturerSelectionEnabled);
			}
		}
		private bool manufacturerSelectionEnabled;

		private IList<Manufacturer> selectedManufacturers;

		private const int FIRST_SEARCH_RANGE = 120;

		// Cancellation Token used to cancel unnecessary/old datasheet queries.
		private CancellationTokenSource CurrentTokenSource = new CancellationTokenSource();
		private CancellationTokenSource PreviousTokenSource;

		#endregion
	}
}
