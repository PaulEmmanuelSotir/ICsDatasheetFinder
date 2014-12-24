using Caliburn.Micro;
using System.ComponentModel;

namespace ICsDatasheetFinder.ViewModels
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

		public bool CanGoBack => navigationService.CanGoBack;
	}
}
