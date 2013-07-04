using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Caliburn.Micro;
using Bicikelj.Model;
using Bicikelj.Controls;
using Bicikelj.Views;

namespace Bicikelj.ViewModels
{
    //the hosting VM
    public class HostPageViewModel : Conductor<object>
    {
        public const string TARGET_VM_KEY = "cm-navigation-target-vm";
        public const string TARGET_VM_CTX = "cm-navigation-target-ctx";

        IPhoneService phoneService;
        public HostPageViewModel(IPhoneService phoneService)
        {
            this.phoneService = phoneService;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ActivateVM();
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            ActivateVM();
        }

        public override void ActivateItem(object item)
        {
            if (phoneService.State.ContainsKey(TARGET_VM_CTX))
            {
                var targetCtx = phoneService.State[TARGET_VM_CTX];
                phoneService.State.Remove(TARGET_VM_CTX);
                SetViewContext(targetCtx);
            }
            else
                SetViewContext(null);
            base.ActivateItem(item);
        }

        /*public override void DeactivateItem(object item, bool close)
        {
            base.DeactivateItem(item, close);
            SetViewContext(null);
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            SetViewContext(null);
        }*/

        private void SetViewContext(object targetCtx)
        {
            var v = GetView() as HostPageView;
            if (v != null)
                View.SetContext(v.ActiveItem, targetCtx);
        }

        private void ActivateVM()
        {
            if (!phoneService.State.ContainsKey(TARGET_VM_KEY)) return;

            var targetVM = phoneService.State[TARGET_VM_KEY];
            phoneService.State.Remove(TARGET_VM_KEY);
            this.ActivateItem(targetVM);
        }
    }
}
