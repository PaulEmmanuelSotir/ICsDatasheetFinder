﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.System;
using Caliburn.Micro;
using ICsDatasheetFinder_8._1.Data;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using ICsDatasheetFinder_8._1.Views;
using Windows.UI.ViewManagement;

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

        private async void SeeDatasheet(ItemClickEventArgs e)
        {
            var part = e.ClickedItem as Part;
            if (part.datasheetURL.LastIndexOf(".pdf", StringComparison.OrdinalIgnoreCase) == part.datasheetURL.Length - 4)
                GoTo<DatasheetViewModel>(part);
            else
                await Launcher.LaunchUriAsync(new Uri(part.datasheetURL));
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

        private CancellationTokenSource CurrentTokenSource = new CancellationTokenSource();
        private CancellationTokenSource PreviousTokenSource;

        private async void QueryForDatasheets(object sender)
        {
            if (PreviousTokenSource != null)
                PreviousTokenSource.Cancel(true);
            CurrentTokenSource = new CancellationTokenSource();
            PreviousTokenSource = CurrentTokenSource;

            IsEmptyResult = false;
            IsMoreResult = false;

            if (sender != this)
                Query = (sender as Callisto.Controls.WatermarkTextBox).Text;
            try
            {
                if (Query != string.Empty)
                {
                    CurrentTokenSource.Token.ThrowIfCancellationRequested();
                    IsProcesssing = true;

                    if (datasheets.Count != 0)
                    {
                        datasheets.Clear();
                        NotifyOfPropertyChange<int>(() => DatasheetsCount);
                    }

                    await Task.Factory.StartNew(() =>
                    {
                        if (ManufacturerSelectionEnabled)
                            return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, selectedManufacturers, FIRST_SEARCH_RANGE);
                        return DatasheetDataSource.SearchForDatasheet(Query, CurrentTokenSource.Token, FIRST_SEARCH_RANGE);
                    }, CurrentTokenSource.Token).ContinueWith(async (ResultingTask) =>
                    {
                        CurrentTokenSource.Token.ThrowIfCancellationRequested();

                        Datasheets.UnionWith(ResultingTask.Result);
                        NotifyOfPropertyChange<int>(() => DatasheetsCount);
                        var rslt = (from t in Manufacturers from manu in t select manu).ToList<Manufacturer>();
                        ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList(), rslt);
                        IsMoreResult = Datasheets.Count == FIRST_SEARCH_RANGE;

                        CurrentTokenSource.Token.ThrowIfCancellationRequested();
                        await ViewDatasheets.LoadMoreItemsAsync(FIRST_SEARCH_RANGE);
                    }, CurrentTokenSource.Token, TaskContinuationOptions.NotOnCanceled, TaskScheduler.FromCurrentSynchronizationContext());


                    CurrentTokenSource.Token.ThrowIfCancellationRequested();
                    IsProcesssing = false;
                }

                CurrentTokenSource.Token.ThrowIfCancellationRequested();
                if (Datasheets.Count == 0)
                    IsEmptyResult = true;
                else
                    IsEmptyResult = false;
            }
            catch (OperationCanceledException) { }
        }

        private async void FindMoreResults()
        {
            CurrentTokenSource = new CancellationTokenSource();
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
            var txtBox = (sender as Callisto.Controls.WatermarkTextBox);
            // Ecrit la requete issue du search panel dans le watermarkTextBox
            if (Query != string.Empty)
            {
                txtBox.Text = Query;
                // Place le curseur à la fin du texte
                txtBox.Select(txtBox.Text.Length, 0);
            }
            txtBox.PreventKeyboardDisplayOnProgrammaticFocus = false;
            txtBox.Focus(FocusState.Programmatic);
        }

        private void PageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplicationView currentView = ApplicationView.GetForCurrentView();
            if (currentView.Orientation == ApplicationViewOrientation.Landscape)
                HubOrientation = Orientation.Horizontal;
            else
                HubOrientation = Orientation.Vertical;
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
                query = value;
                NotifyOfPropertyChange<String>(() => Query);
                NotifyOfPropertyChange<bool>(() => IsAnyQuery);
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
                NotifyOfPropertyChange<bool>(() => IsNoMoreResult);
            }
        }
        private bool isMoreResult = false;
        public bool IsEmptyResult
        {
            get
            {
                return isEmptyResult;
            }
            set
            {
                isEmptyResult = value;
                NotifyOfPropertyChange<bool>(() => IsEmptyResult);
            }
        }
        private bool isEmptyResult = false;
        public bool IsNoMoreResult
        {
            get
            {
                return !IsMoreResult;
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
                NotifyOfPropertyChange<bool>(() => IsNotProcesssing);
            }
        }
        private bool isProcessing = false;
        public bool IsNotProcesssing
        {
            get
            {
                return !isProcessing;
            }
        }

        public Orientation HubOrientation
        {
            get
            {
                return hubOrientation;
            }
            private set
            {
                hubOrientation = value;
                NotifyOfPropertyChange<Orientation>(() => HubOrientation);
            }
        }
        private Orientation hubOrientation = Orientation.Horizontal;

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
