using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace LINECommon.Windows.Controls
{
    public class TreeViewCheckboxItem : TreeViewItem, INotifyPropertyChanged
    {
        private System.Windows.Controls.CheckBox _checkbox = null;
        private TextBlock _textBlock = null;
        private System.Windows.RoutedEvent checkedEvent;
        private object _dataType;
        private TreeViewCheckboxItem _parent = null;

        public bool? Checked
        {
            get { return _checkbox.IsChecked; }
            set { _checkbox.IsChecked = value; }
        }

        public string Text
        {
            get { return _textBlock.Text; }
            set { _textBlock.Text = value; }
        }

        public System.Windows.RoutedEvent CheckedEvent
        {
            get { return checkedEvent; }
        }



        public object Data => _dataType;

        public ObservableCollection<TreeViewCheckboxItem> Children { get; set; }

        public Visibility IsChecked { get; set; }

        public TreeViewCheckboxItem(object data)
        {
            _dataType = data;
            Children = new ObservableCollection<TreeViewCheckboxItem>();

            CreateTreeViewItemTemplate();
        }

        public TreeViewCheckboxItem(object data, TreeViewCheckboxItem parent)
        {
            _dataType = data;
            Children = new ObservableCollection<TreeViewCheckboxItem>();

            CreateTreeViewItemTemplate();
        }

        private void CreateTreeViewItemTemplate()
        {
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            _checkbox = new System.Windows.Controls.CheckBox();
            _checkbox.Name = "checkbox";
            _checkbox.Margin = new Thickness(0, 0, 10, 0);
            _checkbox.Checked += CheckboxOnChecked;
            _checkbox.Unchecked += CheckboxOnUnchecked;
            stackPanel.Children.Add(_checkbox);


            _textBlock = new TextBlock();
            _textBlock.Text = "Value";

            stackPanel.Children.Add(_textBlock);

            Header = stackPanel;
        }

        private void CheckboxOnUnchecked(object sender, RoutedEventArgs e)
        {
            // Look to see if the parent is checked.
            System.Windows.Controls.CheckBox cb = (System.Windows.Controls.CheckBox)sender;
            bool? status = cb.IsChecked;
            StackPanel sp = cb.Parent as StackPanel;
            TreeViewCheckboxItem node = sp.Parent as TreeViewCheckboxItem;
            var parent = node.Parent;
            if (parent is TreeViewCheckboxItem)
                CheckAncestors(status, (TreeViewCheckboxItem)parent);
            else
                CheckDescendants(status, node);
        }

        private void CheckAncestors(bool? status, TreeViewCheckboxItem parent)
        {
            int cnt = 0;
            // check all of the children
            foreach (TreeViewCheckboxItem child in parent.Items)
            {
                if (child.Checked.HasValue && child.Checked.Value)
                    cnt++;
            }
            if (cnt == parent.Items.Count)
                parent.Checked = true;
            else if (cnt == 0)
                parent.Checked = false;
            else
                parent.Checked = null;
        }

        private void CheckDescendants(bool? status, TreeViewCheckboxItem parent)
        {
            foreach (TreeViewCheckboxItem child in parent.Items)
            {
                child.Checked = status;
            }
        }

        private void CheckboxOnChecked(object sender, RoutedEventArgs e)
        {
            // Look to see if the parent is checked.
            System.Windows.Controls.CheckBox cb = (System.Windows.Controls.CheckBox)sender;
            bool? status = cb.IsChecked;
            StackPanel sp = cb.Parent as StackPanel;
            TreeViewCheckboxItem node = sp.Parent as TreeViewCheckboxItem;
            var parent = node.Parent;
            if (parent is TreeViewCheckboxItem)
                CheckAncestors(status, (TreeViewCheckboxItem)parent);
            else
                CheckDescendants(status, node);

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
