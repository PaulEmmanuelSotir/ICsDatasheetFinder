using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.System;
using Caliburn.Micro;
using ICsDatasheetFinder_8._1.Data;
using System.Collections.ObjectModel;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
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
                if(Args.PropertyName == "ManufacturerSelectionEnabled")
                    QueryForDatasheets(this);
            });
        }

        protected override async void OnInitialize()
        {
            base.OnInitialize();

            await Task.Factory.StartNew(() =>
            {
                Manufacturers.AddRange(DatasheetDataSource.GetManufacturers());
            });
            // Load manufacturers logos
            await DatasheetDataSource.LoadManufacturersImagesAsync();
        }

        private async void SeeDatasheet(ItemClickEventArgs e)
        {
            var part = e.ClickedItem as Part;
            part.IsLoadingDatasheet = true;
            await Launcher.LaunchUriAsync(new Uri(part.datasheetURL));
            part.IsLoadingDatasheet = false;
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

        private async void QueryForDatasheets(object sender)
        {
            IsEmptyResult = false;
            IsMoreResult = false;

            if (sender != this)
                Query = (sender as Callisto.Controls.WatermarkTextBox).Text;

            ulong CurrentQueryNumber = QueryNumber + 1;
            QueryNumber++;
            if (CurrentQueryNumber == ulong.MaxValue)
                CurrentQueryNumber = 0;

            if (Query != string.Empty)
            {
                IsProcesssing = true;
                if (datasheets.Count != 0)
                {
                    datasheets.Clear();
                    NotifyOfPropertyChange<int>(() => DatasheetsCount);
                }
                await Task.Factory.StartNew(() =>
                {
                    if (ManufacturerSelectionEnabled)
                        return DatasheetDataSource.SearchForDatasheet(Query, selectedManufacturers, FIRST_SEARCH_RANGE);
                    return DatasheetDataSource.SearchForDatasheet(Query, FIRST_SEARCH_RANGE);
                }).ContinueWith(async (ResultingTask) =>
                {
                    if (CurrentQueryNumber == QueryNumber)
                    {
                        Datasheets.UnionWith(ResultingTask.Result);
                        NotifyOfPropertyChange<int>(() => DatasheetsCount);
                        ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList());
                        IsMoreResult = Datasheets.Count == FIRST_SEARCH_RANGE;
                        await ViewDatasheets.LoadMoreItemsAsync(FIRST_SEARCH_RANGE);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
                IsProcesssing = false;
            }

            if (Datasheets.Count == 0 && CurrentQueryNumber == QueryNumber)
                IsEmptyResult = true;
            else
                IsEmptyResult = false;
        }

        private async void FindMoreResults()
        {
            IsProcesssing = true;

            await Task.Factory.StartNew(() =>
            {
                if (ManufacturerSelectionEnabled)
                    return DatasheetDataSource.SearchForDatasheet(Query, selectedManufacturers);
                return DatasheetDataSource.SearchForDatasheet(Query);
            }).ContinueWith(async (ResultingTask) =>
            {
                datasheets.Clear();
                Datasheets.UnionWith(ResultingTask.Result);
                NotifyOfPropertyChange<int>(() => DatasheetsCount);
                ViewDatasheets = new Common.IncrementalLoadingDatasheetList(Datasheets.ToList());
                IsMoreResult = false;
                await ViewDatasheets.LoadMoreItemsAsync(120);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            IsProcesssing = false;
        }

        private void Hub_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var ViewHub = (sender as Hub);
            if (e.NewSize.Width < e.NewSize.Height)
            {
                ViewHub.Orientation = Orientation.Vertical;
               // HeroImageSection.Width = e.NewSize.Width;
            }
            else
            {
                ViewHub.Orientation = Orientation.Horizontal;
               // HeroImageSection.Width = (Window.Current.Bounds.Width * 2) / 3;
            }
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
        private ulong QueryNumber = 0;

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
