using Caliburn.Micro;
using System.ComponentModel;

namespace ICsDatasheetFinder.ViewModels
{
	public abstract class ViewModelBase : Screen, INotifyPropertyChanged
	{
		protected readonly INavigationService _navigationService;

		protected ViewModelBase(INavigationService navigationService)
		{
			_navigationService = navigationService;
		}

		public void GoTo<ViewModelType>()
		{
			_navigationService.NavigateToViewModel<ViewModelType>();
		}

		public void GoBack()
		{
			_navigationService.GoBack();
		}

		public bool CanGoBack => _navigationService.CanGoBack;
	}
}
