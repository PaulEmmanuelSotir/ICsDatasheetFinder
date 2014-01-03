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
	public class MainViewModel : ViewModelBase
	{
		public MainViewModel(INavigationService navigationService)
			: base(navigationService)
		{
			Manufacturers = new BindableCollection<IGrouping<char, Manufacturer>>();
			selectedManufacturers = new List<Manufacturer>();
			datasheets = new HashSet<Part>();
			this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler((obj, Args) =>
			{
				if (Args.PropertyName == "ManufacturerSelectionEnabled")
					QueryForDatasheets(this);
			});
		}

		private string parameter;
		public string Parameter
		{
			get
			{
				return parameter;
			}
			set
			{
				parameter = value;
				NotifyOfPropertyChange();
			}
		}

		protected override async void OnInitialize()
		{
			base.OnInitialize();

			Query = Parameter ?? "";
			await Task.Factory.StartNew(() =>
			{
				Manufacturers.AddRange(DatasheetDataSource.GetManufacturers());
			});
			// Load manufacturers logos
			await DatasheetDataSource.LoadManufacturersImagesAsync();

			if (Query != string.Empty)
				QueryForDatasheets(this);
		}

		private void SeeDatasheet(ItemClickEventArgs e)
		{
			GoTo<DatasheetViewModel>(e.ClickedItem as Part);
		}

		private async void SeeElecDatabase()
		{
			await Launcher.LaunchUriAsync(new Uri("ms-windows-store:PDP?PFN=45311Paul-EmmanuelSotir.ElectronicDatabase_7q75p07zxm5km"));
		}

		private void UpdateManufacturerSelection(SelectionChangedEventArgs e)
		{
			foreach (Manufacturer manu in e.AddedItems)
			{
				if (!selectedManufacturers.Contains(manu))
				{
					selectedManufacturers.Add(manu);
					QueryForDatasheets(this);
				}
			}
			foreach (Manufacturer manu in e.RemovedItems)
			{
				if (selectedManufacturers.Contains(manu))
				{
					selectedManufacturers.Remove(manu);
					QueryForDatasheets(this);
				}
			}
		}
		
		private CancellationTokenSource PreviousTokenSource;

		// TODO : quand on recherche avec tout les résultats, conserver seulement les manufacturers concernés
		private async void QueryForDatasheets(object sender)
		{
			if (PreviousTokenSource != null)
				PreviousTokenSource.Cancel(true);
			// TODO : wait for the canceled task (task cancelling is not instantaneous !)

			var CurrentTokenSource = new CancellationTokenSource();
			PreviousTokenSource = CurrentTokenSource;

			IsZeroManufacturerSelected = false;
			IsEmptyResult = false;
			// TODO : règler le problème d'affichage de "0 datasheet trouvées" pendant la recherche ! (cf TODO dans le XAML)
			IsMoreResult = false;

			if (sender != this) // MVVM paradigm violation !!
				Query = (sender as TextBox).Text;
			
			try
			{
				if (IsAnyQuery)
				{
					IsProcesssing = true;

					// the task cannot be canceled here because an async function run synchronously until the first await.
					// Moreover, as "datasheets" collection cleaning is necessary whether the task was cancelled or not, there is no 
					// need to try to prevent it.

					if (datasheets.Count != 0)
					{
						datasheets.Clear();
						NotifyOfPropertyChange<int>(() => DatasheetsCount);
					}

					var result = await Task.Factory.StartNew(() =>
									{
										if (ManufacturerSelectionEnabled)
											return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, selectedManufacturers, FIRST_SEARCH_RANGE);
										return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, FIRST_SEARCH_RANGE);
									}, CurrentTokenSource.Token);

					CurrentTokenSource.Token.ThrowIfCancellationRequested();

					//ajout des résultats de recherche dans la liste:
					Datasheets.UnionWith(result);
					NotifyOfPropertyChange<int>(() => DatasheetsCount);

					// liste de tous les Manufacturers, /!\ maybe the list shall be saved for future used, saving some processing power at each request...
					// or maybe the correspondance between component and manufacturer should be done directly here, in the linq request
					var rslt = (from t in Manufacturers from manu in t select manu).ToList<Manufacturer>();

					// preparation de l'incremental loading et affichage de la première tranche.
					ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), rslt);
					IsMoreResult = Datasheets.Count == FIRST_SEARCH_RANGE;
					CurrentTokenSource.Token.ThrowIfCancellationRequested();
					await ViewDatasheets.LoadMoreItemsAsync(FIRST_SEARCH_RANGE);

					CurrentTokenSource.Token.ThrowIfCancellationRequested();
					IsProcesssing = false;
				}

				CurrentTokenSource.Token.ThrowIfCancellationRequested();

				if (Datasheets.Count == 0)
				{
					if (ManufacturerSelectionEnabled && selectedManufacturers.Count == 0)
						IsZeroManufacturerSelected = true;
					else
						IsEmptyResult = true;
				}
				else
				{
					IsZeroManufacturerSelected = false;
					IsEmptyResult = false;
				}
			}
			catch (OperationCanceledException) {
				//maybe requires "IsProcesssing = false;" ??
			}
		}

		private async void FindMoreResults()
		{
			// prevent any double task
			if (PreviousTokenSource != null)
				PreviousTokenSource.Cancel(true);
			//TODO : wait the canceled task.

			var CurrentTokenSource = new CancellationTokenSource();
			PreviousTokenSource = CurrentTokenSource;

			try
			{
				CurrentTokenSource.Token.ThrowIfCancellationRequested();
				IsProcesssing = true;

				await Task.Factory.StartNew(() =>
				{
					if (ManufacturerSelectionEnabled)
						return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, selectedManufacturers);
					return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token);
				}, CurrentTokenSource.Token).ContinueWith(async (ResultingTask) =>
				{
					CurrentTokenSource.Token.ThrowIfCancellationRequested();
					datasheets.Clear();
					Datasheets.UnionWith(ResultingTask.Result);
					NotifyOfPropertyChange<int>(() => DatasheetsCount);
					var rslt = (from t in Manufacturers from manu in t select manu).ToList<Manufacturer>();
					ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), rslt);
					IsMoreResult = false;

					CurrentTokenSource.Token.ThrowIfCancellationRequested();
					await ViewDatasheets.LoadMoreItemsAsync(120);
				}, CurrentTokenSource.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());

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
			txtBox.PreventKeyboardDisplayOnProgrammaticFocus = false;
			txtBox.Focus(FocusState.Programmatic);
		}

		public BindableCollection<IGrouping<char, Manufacturer>> Manufacturers
		{
			get;
			private set;
		}

		public String Query
		{
			get
			{
				return query;
			}
			set
			{
				if (value.Contains('\''))
					value = value.Replace("\'", "");
				value = value.Trim();

				if (value != query)
				{
					query = value;
					NotifyOfPropertyChange("Query");
					//NotifyOfPropertyChange<String>(() => Query);
					NotifyOfPropertyChange<bool>(() => IsAnyQuery);
				}
			}
		}
		private String query;
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

		private const int FIRST_SEARCH_RANGE = 120;

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
	}
}
