using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using System.Windows;
using System.Collections;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace Bicikelj.ViewModels
{
    public class SelectorViewModel : Screen
    {
        public DataTemplate GroupHeaderTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }
        public IEnumerable ItemsSource { get; set; }
        public object SelectedItem { get; set; }
        public bool IsFlatList { get; set; }
        
        private SelectorView selView;

        protected override void OnViewAttached(object view, object context)
        {
            selView = view as SelectorView;
            base.OnViewAttached(view, context);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            //var dlghost = GetView() as Caliburn.Micro.WindowManager;
            var view = selView;// dlghost as SelectorView;
            if (GroupHeaderTemplate != null)
                view.Selector.GroupHeaderTemplate = GroupHeaderTemplate;
            if (ItemTemplate != null)
                view.Selector.ItemTemplate = ItemTemplate;
            view.Selector.IsFlatList = IsFlatList;
            view.Selector.ItemsSource = ItemsSource;
            if (SelectedItem != null)
            {
                view.Selector.SelectedItem = SelectedItem;
                view.Selector.ScrollTo(SelectedItem);
            }
            view.Selector.SelectionChanged += Selector_SelectionChanged;
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            if (selView != null)
                selView.Selector.SelectionChanged -= Selector_SelectionChanged;
        }

        void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = sender as LongListSelector;
            // occurs when selecting a group
            if (selector.SelectedItem == null)
                return;
            SelectedItem = selector.SelectedItem;
            this.TryClose();
        }
    }
}
