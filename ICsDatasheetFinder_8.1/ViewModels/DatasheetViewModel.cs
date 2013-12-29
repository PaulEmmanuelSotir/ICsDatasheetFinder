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
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

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
            // TODO : enlever cette ligne et suprimmer le fichier !
            Parameter.datasheetURL = @"Data\4030.pdf";

            PdfDocument _pdfDocument;
            try
            {
                StorageFile pdfFile = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(parameter.datasheetURL);
                //Load Pdf File
                _pdfDocument = await PdfDocument.LoadFromFileAsync(pdfFile); ;

                IsLoadingDatasheet = false;
                datasheetPages = new BindableCollection<BitmapImage>();
                //DatasheetPages = new IncrementalyLoadingPdfPages(_pdfDocument);

                if (_pdfDocument != null)
                {
                    for (uint i = 0; i < _pdfDocument.PageCount; i++)
                    {
                        //Get Pdf page
                        var pdfPage = await Task.Run(() => _pdfDocument.GetPage(i));

                        if (pdfPage != null)
                        {
                            // next, generate a bitmap of the page
                            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

                            StorageFile jpgFile = await tempFolder.CreateFileAsync(Guid.NewGuid().ToString() + ".png", CreationCollisionOption.ReplaceExisting);

                            if (jpgFile != null)
                            {
                                IRandomAccessStream randomStream = await jpgFile.OpenAsync(FileAccessMode.ReadWrite);

                                PdfPageRenderOptions pdfPageRenderOptions = new PdfPageRenderOptions();
                                await pdfPage.RenderToStreamAsync(randomStream);
                                await randomStream.FlushAsync();

                                randomStream.Dispose();
                                pdfPage.Dispose();

                                BitmapImage newPage = new BitmapImage();
                                await newPage.SetSourceAsync(await jpgFile.OpenAsync(FileAccessMode.Read));
                                datasheetPages.Add(newPage);
                                NotifyOfPropertyChange<BindableCollection<BitmapImage>>(() => DatasheetPages);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                IsLoadingDatasheet = false;
                //TODO : gerer l'erreur
            }
        }

        private async void SeeDatasheetOnBrowser()
        {
            await Launcher.LaunchUriAsync(new Uri(Parameter.datasheetURL));
        }

        private bool isLoadingDatasheet = true;
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

        private BindableCollection<BitmapImage> datasheetPages;
        public BindableCollection<BitmapImage> DatasheetPages
        {
            get
            {
                return datasheetPages;
            }
            private set
            {
                datasheetPages = value;
                // TODO : verifier que cette notification est bonne 
                NotifyOfPropertyChange<BindableCollection<BitmapImage>>(() => DatasheetPages);
            }
        }

        //private IncrementalyLoadingPdfPages datasheetPages;
        //public IncrementalyLoadingPdfPages DatasheetPages
        //{
        //    get
        //    {
        //        return datasheetPages;
        //    }
        //    private set
        //    {
        //        datasheetPages = value;
        //        // TODO : verifier que cette notification est bonne 
        //        NotifyOfPropertyChange<IncrementalyLoadingPdfPages>(() => DatasheetPages);
        //    }
        //}
    }
}
