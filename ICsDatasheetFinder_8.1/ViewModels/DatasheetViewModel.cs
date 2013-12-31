using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Caliburn.Micro;
using ICsDatasheetFinder_8._1.Data;
using ICsDatasheetFinder_8._1.Common;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Networking.BackgroundTransfer;
using System.Net.Http;

namespace ICsDatasheetFinder_8._1.ViewModels
{
	public class DatasheetViewModel : ViewModelBase
	{
		//TODO : ajout d'une déclaration de confidentialité !
		public DatasheetViewModel(INavigationService navigationService)
			: base(navigationService)
		{
			this.Activated += new EventHandler<ActivationEventArgs>((sender, e) =>
			{
				LoadDatasheet();
			});
		}

		private Part parameter;
		public Part Parameter
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

		private async void LoadDatasheet()
		{
			PdfDocument _pdfDocument;
			try
			{
				_pdfFile = await DownloadDatasheet();

				if (_pdfFile != null)
				{
					IsDownloadingDatasheet = false;
					IsLoadingDatasheet = true;
					_pdfDocument = await PdfDocument.LoadFromFileAsync(_pdfFile);

					datasheetPages = new BindableCollection<DatasheetPage>();

					if (_pdfDocument != null)
					{
						for (uint i = 0; i < _pdfDocument.PageCount; i++)
						{
							var pdfPage = await Task.Run(() => _pdfDocument.GetPage(i));

							if (pdfPage != null)
							{
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
									DatasheetPage page = new DatasheetPage(pdfPage.Index + 1, pngFile.Path);
									pdfPage.Dispose();

									datasheetPages.Add(page);
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
			HttpResponseMessage response = await client.GetAsync(Parameter.datasheetURL);
			StorageFile DatasheetFile;
			if (response.Content.Headers.ContentType.MediaType == "application/pdf")
			{
				DatasheetFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Parameter.reference + ".pdf", CreationCollisionOption.ReplaceExisting);

				using (IRandomAccessStream fs = await DatasheetFile.OpenAsync(FileAccessMode.ReadWrite))
				{
					using (DataWriter writer = new DataWriter(fs.GetOutputStreamAt(0)))
					{
						writer.WriteBytes(await response.Content.ReadAsByteArrayAsync());
						await writer.StoreAsync();
						await fs.FlushAsync();
					}
				}
				response.Dispose();
				client.Dispose();

				return DatasheetFile;
			}
			return null;
		}

		private async void PageUnloaded()
		{
			foreach(var file in await ApplicationData.Current.TemporaryFolder.GetFilesAsync())
			{
				await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}

		private async Task SeeDatasheetOnBrowser()
		{
			await Launcher.LaunchUriAsync(new Uri(Parameter.datasheetURL));
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
				FileSavePicker savePicker = new FileSavePicker();
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

		private bool isDownloadingDatasheet = true;
		public bool IsDownloadingDatasheet
		{
			get
			{
				return isDownloadingDatasheet;
			}
			private set
			{
				isDownloadingDatasheet = value;
				NotifyOfPropertyChange<bool>(() => IsDownloadingDatasheet);
			}
		}

		private bool isLoadingDatasheet = false;
		public bool IsLoadingDatasheet
		{
			get
			{
				return isLoadingDatasheet;
			}
			private set
			{
				isLoadingDatasheet = value;
				NotifyOfPropertyChange<bool>(() => IsLoadingDatasheet);
			}
		}

		private BindableCollection<DatasheetPage> datasheetPages;
		public BindableCollection<DatasheetPage> DatasheetPages
		{
			get
			{
				return datasheetPages;
			}
			private set
			{
				datasheetPages = value;
				NotifyOfPropertyChange<BindableCollection<DatasheetPage>>(() => DatasheetPages);
			}
		}

		private StorageFile _pdfFile;
	}
}
