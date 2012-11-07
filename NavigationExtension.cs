using Caliburn.Micro;
using Bicikelj.ViewModels;

namespace Bicikelj
{
	//a convenience extension method
	public static class NavigationExtension
	{
		public static void NavigateTo(object targetModel)
		{
			IoC.Get<IPhoneService>().State[HostPageViewModel.TARGET_VM_KEY] = targetModel;
			IoC.Get<INavigationService>().UriFor<HostPageViewModel>().Navigate();
		}
	}
}