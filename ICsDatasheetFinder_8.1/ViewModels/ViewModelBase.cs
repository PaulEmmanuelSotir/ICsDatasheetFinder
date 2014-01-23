using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using Caliburn.Micro;

namespace ICsDatasheetFinder_8._1.ViewModels
{
    public abstract class ViewModelBase : Screen, INotifyPropertyChanged
    {
        protected readonly INavigationService navigationService;

        protected ViewModelBase(INavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        public void GoTo<ViewModelType>()
        {
            navigationService.NavigateToViewModel<ViewModelType>();
        }

        public void GoBack()
        {
            navigationService.GoBack();
        }

        public bool CanGoBack
        {
            get
            {
                return navigationService.CanGoBack;
            }
        }
    }
}
