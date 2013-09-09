using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using System.Windows;
using System.Collections;
using System.Windows.Controls;

namespace Bicikelj.ViewModels
{
    public class SelectorViewModel : Screen
    {
        public DataTemplate GroupHeaderTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }
        public IEnumerable ItemsSource { get; set; }

        protected override void OnActivate()
        {
            base.OnActivate();
            var view = GetView() as SelectorView;
            if (GroupHeaderTemplate != null)
                view.Selector.GroupHeaderTemplate = GroupHeaderTemplate;
            if (ItemTemplate != null)
                view.Selector.ItemTemplate = ItemTemplate;
            view.Selector.ItemsSource = ItemsSource;
            view.Selector.SelectionChanged += Selector_SelectionChanged;
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            var view = GetView() as SelectorView;
            view.Selector.SelectionChanged -= Selector_SelectionChanged;
        }

        void Selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.TryClose();
            
        }
    }
}
