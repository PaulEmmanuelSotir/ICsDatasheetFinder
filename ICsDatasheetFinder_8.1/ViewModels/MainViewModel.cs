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

namespace ICsDatasheetFinder_8._1.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel(INavigationService navigationService)
            : base(navigationService)
        {
            Manufacturers = new BindableCollection<IGrouping<char, Manufacturer>>();
            selectedManufacturers = new List<Manufacturer>();
            datasheets = new BindableCollection<Part>();
            //     FoundDatasheets = new Common.IncrementalyLoadingPartList();
            //selectedManufacturers = new List<Manufacturer>();

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

        private async void SeeElecDatabase()
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store:PDP?PFN=45311Paul-EmmanuelSotir.ElectronicDatabase_7q75p07zxm5km"));
        }

        private void UpdateManufacturerSelection(SelectionChangedEventArgs e)
        {
            foreach (Manufacturer manu in e.AddedItems)
            {
                if (!selectedManufacturers.Contains(manu))
                    selectedManufacturers.Add(manu);
            }
            foreach (Manufacturer manu in e.RemovedItems)
            {
                selectedManufacturers.Remove(manu);
            }
        }
        //private Task SearchTask;
        private object obj = new object();
        private async void QueryForDatasheets(object sender)
        {
            Query = (sender as Callisto.Controls.WatermarkTextBox).Text;

            if (ManufacturerSelectionEnabled)
            {
                // datasheets.reset(Query, selectedManufacturers);
                // Datasheets = await DatasheetDataSource.SearchForDatasheet(Query, selectedManufacturers);
            }
            else
            {
                var SynchronizationCtxt = TaskScheduler.FromCurrentSynchronizationContext();


                await Task.Factory.StartNew(() =>
                {
                    if (datasheets.Count != 0)
                        datasheets.Clear();
                    lock (obj)
                    {
                        return DatasheetDataSource.SearchForDatasheet(Query, 120);
                    }
                }).ContinueWith((ResultingTask) =>
                             {
                                 Datasheets.AddRange(ResultingTask.Result);
                             }, SynchronizationCtxt);

                /*
           //     var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
                Task.Factory.StartNew(async () =>
                {
                    ulong offset = 0;

               /*     datasheets = await DatasheetDataSource.SearchForDatasheet(Query, 10000);
                    Datasheets2 = datasheets.ToList();
*/
                /*
                                  cts.Token.ThrowIfCancellationRequested();

                                  while (DatasheetDataSource.PART_NUMBER > offset)
                                  {
                                      if (DatasheetDataSource.PART_NUMBER <= offset + 10000)
                                          offset = offset;
                                      datasheets.UnionWith(await DatasheetDataSource.SearchForDatasheet(Query, 10000, offset));
                                      Datasheets2 = datasheets.ToList();
              /*
                                      // Notify on UI thread
                                      await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                          {
                                              NotifyOfPropertyChange<List<Part>>(() => Datasheets2);
                                          });
                                      */
                /* Task.Factory.StartNew(() =>
                 {
                     NotifyOfPropertyChange<IEnumerable<Part>>(() => Datasheets);
                     NotifyOfPropertyChange<List<Part>>(() => Datasheets2);
                 }, scheduler);*/
                /*
                        offset += 10000;
                        cts.Token.ThrowIfCancellationRequested();
                    }
                    // uint 
                    // while()

                }, cts.Token);*/


                /* DatasheetDataSource.SearchForDatasheet(Query, -1).ContinueWith( (ResultingTask) =>
                     {
                         Datasheets = ResultingTask.Result;
                     }, TaskScheduler.FromCurrentSynchronizationContext());*/
                // datasheets.reset(Query);
                // Datasheets = await DatasheetDataSource.SearchForDatasheet(Query);
            }
            //    FoundDatasheets.Query = "NE";
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

        public BindableCollection<Part> Datasheets
        {
            get
            {
                return datasheets;
            }
            private set
            {
                datasheets = value;
                NotifyOfPropertyChange<BindableCollection<Part>>(() => Datasheets);
            }
        }
        private BindableCollection<Part> datasheets;

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
