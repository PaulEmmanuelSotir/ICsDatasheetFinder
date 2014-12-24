using Caliburn.Micro;
using ICsDatasheetFinder.Data;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.Storage.Streams;
using Windows.System;

namespace ICsDatasheetFinder.ViewModels
{
	public class DatasheetViewModel : ViewModelBase
	{
		// TODO : restaurer l'état de la mainView quand on "goBack"
		public DatasheetViewModel(INavigationService navigationService)
			: base(navigationService)
		{
			this.Activated += new EventHandler<ActivationEventArgs>((sender, e) =>
			{
				LoadDatasheet();
			});
		}

		private async void LoadDatasheet()
		{
			PdfDocument pdfDocument;
			try
			{
				_pdfFile = await DownloadDatasheet();

				if (_pdfFile != null)
				{
					IsDownloadingDatasheet = false;
					IsLoadingDatasheet = true;
					pdfDocument = await PdfDocument.LoadFromFileAsync(_pdfFile);

					_datasheetPages = new BindableCollection<DatasheetPage>();

					if (pdfDocument != null)
					{
						for (uint i = 0; i < pdfDocument.PageCount; i++)
						{
							var pdfPage = await Task.Run(() => pdfDocument.GetPage(i));

							if (pdfPage != null)
							{
								// TODO : add images in a specific folder in temp
								StorageFile pngFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".png", CreationCollisionOption.ReplaceExisting);


								if (pngFile != null)
								{
									IRandomAccessStream randomStream = await pngFile.OpenAsync(FileAccessMode.ReadWrite);

									PdfPageRenderOptions pdfPageRenderOptions = new PdfPageRenderOptions();
									pdfPageRenderOptions.DestinationHeight = (uint)pdfPage.Dimensions.ArtBox.Height * 2;
									pdfPageRenderOptions.DestinationWidth = (uint)pdfPage.Dimensions.ArtBox.Width * 2;

									await pdfPage.RenderToStreamAsync(randomStream);
									await randomStream.FlushAsync();
									randomStream.Dispose();
									DatasheetPage page = new DatasheetPage(pdfPage.Index + 1, pdfPage.Dimensions.ArtBox, pngFile.Path);
									pdfPage.Dispose();

									_datasheetPages.Add(page);
									NotifyOfPropertyChange<BindableCollection<DatasheetPage>>(() => DatasheetPages);
								}
							}
						}
					}
					IsLoadingDatasheet = false;
				}
				else
				{
					// TODO : afficher un message d'erreur
					await SeeDatasheetOnBrowser();
					GoBack();
				}
			}
			catch (Exception)
			{
				IsLoadingDatasheet = false;
				IsDownloadingDatasheet = false;
				//TODO : gerer l'erreur
			}
		}

		private async Task<StorageFile> DownloadDatasheet()
		{
			// Lien de la déclaration de confidentialité : "http://ma.ms.giz.fr/?name=Datasheet+Finder"
			HttpClient client = new HttpClient();
			client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.63 Safari/537.36");

			StorageFile datasheetFile = null;

			using (client)
			using (var response = await client.GetAsync(DatasheetURL))
			{
				if (response.Content.Headers.ContentType.MediaType == "application/pdf")
				{
					// TODO : add temporary images and datasheets in specific folders
					datasheetFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".pdf", CreationCollisionOption.ReplaceExisting);

					using (IRandomAccessStream fs = await datasheetFile.OpenAsync(FileAccessMode.ReadWrite))
					using (DataWriter writer = new DataWriter(fs.GetOutputStreamAt(0)))
					{
						writer.WriteBytes(await response.Content.ReadAsByteArrayAsync());
						await writer.StoreAsync();
						await fs.FlushAsync();
					}
				}
			}

			return datasheetFile;
		}

		private async void PageUnloaded()
		{
			await _TempFolderDeletionLock.WaitAsync();

			try
			{
				foreach (var file in await ApplicationData.Current.TemporaryFolder.GetFilesAsync())
					await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
			finally
			{
				_TempFolderDeletionLock.Release();
			}
		}

		private async Task SeeDatasheetOnBrowser()
		{
			await Launcher.LaunchUriAsync(new Uri(DatasheetURL));
		}

		private async void OpenPDF()
		{
			if (_pdfFile != null)
			{
				var options = new LauncherOptions();
				options.DesiredRemainingView = Windows.UI.ViewManagement.ViewSizePreference.UseHalf;
				await Launcher.LaunchFileAsync(_pdfFile, options);
			}
		}

		private async void SavePDF()
		{
			if (_pdfFile != null)
			{
				var savePicker = new FileSavePicker();
				savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
				savePicker.FileTypeChoices.Add("PDF document", new List<string>() { ".pdf" });
				savePicker.SuggestedFileName = _pdfFile.DisplayName;
				StorageFile file = await savePicker.PickSaveFileAsync();

				if (file != null)
				{
					// Prevent updates to the remote version of the file until we finish making changes and call CompleteUpdatesAsync.
					CachedFileManager.DeferUpdates(file);

					await _pdfFile.CopyAndReplaceAsync(file);

					// Let Windows know that we're finished changing the file so the other app can update the remote version of the file.
					// Completing updates may require Windows to ask for user input.
					FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
					if (status != FileUpdateStatus.Complete)
					{
						// TODO : Message d'erreur
					}
				}
				//else
				//{
				// TODO : Message d'erreur
				//}
			}
		}

		#region Properties

		public String DatasheetURL
		{
			get
			{
				return _datasheetURL;
			}
			set
			{
				_datasheetURL = value;
				NotifyOfPropertyChange();
			}
		}

		public String PartReference
		{
			get
			{
				return _partReference;
			}
			set
			{
				_partReference = value;
				NotifyOfPropertyChange();
			}
		}

		public bool IsDownloadingDatasheet
		{
			get
			{
				return _isDownloadingDatasheet;
			}
			private set
			{
				_isDownloadingDatasheet = value;
				NotifyOfPropertyChange();
			}
		}

		public bool IsLoadingDatasheet
		{
			get
			{
				return _isLoadingDatasheet;
			}
			private set
			{
				_isLoadingDatasheet = value;
				NotifyOfPropertyChange();
			}
		}

		public BindableCollection<DatasheetPage> DatasheetPages
		{
			get
			{
				return _datasheetPages;
			}
			private set
			{
				_datasheetPages = value;
				NotifyOfPropertyChange();
			}
		}

		#endregion

		#region Members

		private String _datasheetURL;
		private String _partReference;
		private bool _isLoadingDatasheet = false;
		private bool _isDownloadingDatasheet = true;
		private StorageFile _pdfFile;
		private BindableCollection<DatasheetPage> _datasheetPages;

		private static SemaphoreSlim _TempFolderDeletionLock = new SemaphoreSlim(1);

		#endregion
	}
}
