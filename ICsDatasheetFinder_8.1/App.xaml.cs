using Caliburn.Micro;
using ICsDatasheetFinder_8._1.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Compression;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

using ICsDatasheetFinder_8._1.Views;

// Pour plus d'informations sur le modèle Application vide, consultez la page http://go.microsoft.com/fwlink/?LinkId=234227

namespace ICsDatasheetFinder_8._1
{
    /// <summary>
    /// Fournit un comportement spécifique à l'application afin de compléter la classe Application par défaut.
    /// </summary>
    sealed partial class App
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override void Configure()
        {
            container = new WinRTContainer();
            container.RegisterWinRTServices();

            container.PerRequest<MainViewModel>();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            // Show release note or decompress database if app is newly updated or has been launched for the first time.
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string CurrentVersion = string.Format("{0}.{1}.{2}.{3}", Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);
            object firstLaunchFlag = localSettings.Values["FirstLaunch"];
            if (firstLaunchFlag is string)
            {
                if (((string)firstLaunchFlag) != CurrentVersion)
                {
                    localSettings.Values["FirstLaunch"] = CurrentVersion;

                    try
                    {
                        await DecompressDatabase(false);
                    }
                    catch (Exception ex)
                    {
                        //TODO gèrer les exceptions
                        // Utliliser les 'Coroutines' pour les messages à afficher à l'utlisateur un message, voir http://caliburnmicro.codeplex.com/wikipage?title=The%20Event%20Aggregator&referringTitle=Documentation
                    }

                    // TODO show release note during decompression
                }
            }
            else
            {
                localSettings.Values["FirstLaunch"] = CurrentVersion;

                try
                {
                    await DecompressDatabase(true);
                }
                catch (Exception ex)
                {
                    //TODO gèrer les exceptions
                }
            }
            DisplayRootView<MainView>();
        }

        /// <summary>
        /// Appelé lorsque la navigation vers une page donnée échoue
        /// </summary>
        /// <param name="sender">Frame à l'origine de l'échec de navigation.</param>
        /// <param name="e">Détails relatifs à l'échec de navigation</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        protected override object GetInstance(Type service, string key)
        {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            container.BuildUp(instance);
        }

        protected override void PrepareViewFirst(Frame rootFrame)
        {
            container.RegisterNavigationService(rootFrame);
        }

        private async Task DecompressDatabase(bool IsFirstLaunch)
        {
            // Decompress and copy database to Application Data Local Folder.
            IAsyncOperation<StorageFile> compressedDBOp = Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(string.Format("Data\\{0}.compressed", DATABASE_FILE_NAME));
            var decompressedDB = await ApplicationData.Current.LocalFolder.CreateFileAsync(DATABASE_FILE_NAME, IsFirstLaunch ? CreationCollisionOption.FailIfExists : CreationCollisionOption.ReplaceExisting);
            var compressedDB = await compressedDBOp;

            using (var compressedInput = await compressedDB.OpenSequentialReadAsync())
            using (var decompressor = new Decompressor(compressedInput))
            using (var decompressedOutput = await decompressedDB.OpenAsync(FileAccessMode.ReadWrite))
            {
                await RandomAccessStream.CopyAsync(decompressor, decompressedOutput);
            }
        }
        
        // add our IOC container for registering services etc
        private WinRTContainer container;

        public const string DATABASE_FILE_NAME = "datasheets.sqlite";
    }
}
