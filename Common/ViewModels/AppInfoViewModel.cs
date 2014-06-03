using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
    public class AppInfoViewModel : Conductor<IScreen>.Collection.OneActive
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();

            var aboutVM = IoC.Get<AboutViewModel>();
            aboutVM.DisplayName = "about";
            Items.Add(aboutVM);

            var historyVM = IoC.Get<VersionHistoryViewModel>();
            historyVM.DisplayName = "history";
            Items.Add(historyVM);
        }
    }
}