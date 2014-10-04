using Caliburn.Micro;
using System;

namespace Bicikelj.ViewModels
{
    public class AppInfoViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public Type InitialPage { get; set; }
        public bool IsADialog { get; set; }

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

        public void Close()
        {
            IoC.Get<INavigationService>().GoBack();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (InitialPage != null)
            {
                foreach (var item in Items)
                {
                    if ((item as object).GetType() == InitialPage)
                    {
                        this.ActiveItem = item;
                        break;
                    }
                }
            }
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            // clear the values if the view is cached
            IsADialog = false;
            InitialPage = null;
            NotifyOfPropertyChange(() => IsADialog);
        }
    }
}