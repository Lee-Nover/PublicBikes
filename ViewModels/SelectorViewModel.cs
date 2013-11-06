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
    public delegate void ItemSelectedEventHandler(object item);

    public class SelectorViewModel : Screen
    {
        public DataTemplate GroupHeaderTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }
        public IEnumerable ItemsSource { get; set; }
        public object SelectedItem { get; set; }
        public bool IsFlatList { get; set; }
        public event ItemSelectedEventHandler ItemSelected;
        
        private SelectorView selView;

        protected override void OnViewAttached(object view, object context)
        {
            selView = view as SelectorView;
            var rs = Application.Current.RootVisual.RenderSize;
            selView.MaxHeight = System.Math.Max(rs.Height, rs.Width);
            selView.MaxWidth = selView.MaxHeight;
            base.OnViewAttached(view, context);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (GroupHeaderTemplate != null)
                selView.Selector.GroupHeaderTemplate = GroupHeaderTemplate;
            if (ItemTemplate != null)
                selView.Selector.ItemTemplate = ItemTemplate;
            selView.Selector.IsFlatList = IsFlatList;
            selView.Selector.ItemsSource = ItemsSource;
            if (SelectedItem != null)
            {
                selView.Selector.SelectedItem = SelectedItem;
                selView.Selector.ScrollTo(SelectedItem);
            }
            selView.Selector.SelectionChanged += Selector_SelectionChanged;
        }

        protected override void OnDeactivate(bool close)
        {
            if (selView != null)
                selView.Selector.SelectionChanged -= Selector_SelectionChanged;
            base.OnDeactivate(close);
        }

        void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selector = sender as LongListSelector;
            // occurs when selecting a group
            if (selector.SelectedItem == null)
                return;
            SelectedItem = selector.SelectedItem;
            if (ItemSelected != null)
                ItemSelected(SelectedItem);
            else
            {
                var nav = IoC.Get<INavigationService>();
                nav.GoBack();
            }
        }
    }
}
