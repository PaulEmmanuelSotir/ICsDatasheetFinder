using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Search;
using Windows.Storage;
using Windows.Storage.Compression;
using Windows.Storage.Streams;
using Windows.UI.ApplicationSettings;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using ICsDatasheetFinder.ViewModels;
using ICsDatasheetFinder.Views;

namespace ICsDatasheetFinder
{
	/// <summary>
	/// Fournit un comportement spécifique à l'application afin de compléter la classe Application par défaut.
	/// </summary>
	sealed partial class App
	{
		public App()
		{
			this.InitializeComponent();
			//Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "fr-FR";
		}

		protected override void Configure()
		{
			_container = new WinRTContainer();
			_container.RegisterWinRTServices();

			_container.PerRequest<MainViewModel>();
			_container.PerRequest<DatasheetViewModel>();

			SettingsPane.GetForCurrentView().CommandsRequested += SettingCommandsRequested;
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
			string CurrentVersion = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
			object firstLaunchFlag = localSettings.Values["FirstLaunch"];
			if (firstLaunchFlag is string)
			{
				if ((string)firstLaunchFlag != CurrentVersion)
				{
					localSettings.Values["FirstLaunch"] = CurrentVersion;

					try
					{
						await DecompressDatabase(false);
					}
					catch (Exception)
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
				catch (Exception)
				{
					//TODO gèrer les exceptions
				}
			}

			DisplayRootView<MainView>(e.Arguments);
		}

		private void SettingCommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
		{
			var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
			var PrivacyPolicy = loader.GetString("PrivacyPolicy");

			args.Request.ApplicationCommands.Add(new SettingsCommand(PrivacyPolicy, PrivacyPolicy, async Command =>
			{
				await Windows.System.Launcher.LaunchUriAsync(new Uri(loader.GetString("PrivacyPolicyURL")));
			}));
		}

		protected override void OnSearchActivated(SearchActivatedEventArgs args)
		{
			//TODO: s'assurer que la base de donnée est décompressée ici?
			DisplayRootView<MainView>(args.QueryText);
		}

		private void OnQuerySubmitted(object sender, SearchPaneQuerySubmittedEventArgs args)
		{
			//TODO: s'assurer que la base de donnée est décompressée ici?
			DisplayRootView<MainView>(args.QueryText);
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
			return _container.GetInstance(service, key);
		}

		protected override IEnumerable<object> GetAllInstances(Type service)
		{
			return _container.GetAllInstances(service);
		}

		protected override void BuildUp(object instance)
		{
			_container.BuildUp(instance);
		}

		protected override void PrepareViewFirst(Frame rootFrame)
		{
			_container.RegisterNavigationService(rootFrame);
		}

		private async Task DecompressDatabase(bool IsFirstLaunch)
		{
			// Decompress and copy database to Application Data Local Folder.
			var compressedDBAsyncOp = Package.Current.InstalledLocation.GetFileAsync($"Data\\{DATABASE_FILE_NAME}.compressed");
			var decompressedDB = await ApplicationData.Current.LocalFolder.CreateFileAsync(DATABASE_FILE_NAME, IsFirstLaunch ? CreationCollisionOption.FailIfExists : CreationCollisionOption.ReplaceExisting);
			var compressedDB = await compressedDBAsyncOp;

			using (var compressedInput = await compressedDB.OpenSequentialReadAsync())
			using (var decompressor = new Decompressor(compressedInput))
			using (var decompressedOutput = await decompressedDB.OpenAsync(FileAccessMode.ReadWrite))
			{
				await RandomAccessStream.CopyAsync(decompressor, decompressedOutput);
			}
		}

		// Add our IOC container for registering services etc
		private WinRTContainer _container;

		public const string DATABASE_FILE_NAME = "datasheets.sqlite";
	}
}
