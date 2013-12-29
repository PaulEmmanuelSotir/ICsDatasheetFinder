using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Storage.Streams;
using Windows.Storage;
using Windows.Data.Pdf;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace ICsDatasheetFinder_8._1.Common
{
    public class IncrementalyLoadingPdfPages : IncrementalLoadingBase
    {
        public IncrementalyLoadingPdfPages(PdfDocument pdf)
        {
            Datasheet = pdf;
        }

        protected async override Task<IList<object>> LoadMoreItemsOverrideAsync(System.Threading.CancellationToken c, uint count)
        {
            var DatasheetPages = new List<Object>();

            if (Datasheet != null)
            {
                uint ToDo = System.Math.Min((uint)count, (uint)Datasheet.PageCount - doneCount);

                for (uint i = 0; i < ToDo; i++)
                {
                    //Get Pdf page
                    var pdfPage = Datasheet.GetPage(i + doneCount);

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
                            DatasheetPages.Add(newPage);
                        }
                    }
                }

                doneCount += ToDo;
            }

            return DatasheetPages;
        }

        protected override bool HasMoreItemsOverride()
        {
            if (Datasheet != null)
                return Datasheet.PageCount > doneCount;
            return false;
        }

        protected uint doneCount = 0;
        PdfDocument Datasheet;
    }
}
