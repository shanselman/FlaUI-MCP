using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace WpfTestApp
{
    public partial class MainWindow : Window
    {
        private int _clickCount;
        private ObservableCollection<GridItem> _gridItems = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadGridData();
            LoadListData();
        }

        // Buttons tab
        private void ClickMe_Click(object sender, RoutedEventArgs e)
        {
            _clickCount++;
            ClickCountLabel.Text = $"Click count: {_clickCount}";
        }

        private void EnableCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            ConditionalButton.IsEnabled = EnableCheckbox.IsChecked == true;
        }

        // Grid tab
        private void LoadGridData()
        {
            var categories = new[] { "Alpha", "Beta", "Gamma", "Delta" };
            var rng = new System.Random(42);
            for (int i = 0; i < 50; i++)
            {
                _gridItems.Add(new GridItem
                {
                    Selected = false,
                    ID = $"ITEM-{i + 1:D3}",
                    Name = $"Test Item {i + 1}",
                    Category = categories[i % categories.Length],
                    Value = $"{rng.Next(1, 1000)}"
                });
            }
            TestDataGrid.ItemsSource = _gridItems;
        }

        private void GridSelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gridItems) item.Selected = true;
            TestDataGrid.Items.Refresh();
            UpdateGridSelection();
        }

        private void GridClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _gridItems) item.Selected = false;
            TestDataGrid.Items.Refresh();
            UpdateGridSelection();
        }

        private void UpdateGridSelection()
        {
            var count = 0;
            foreach (var item in _gridItems)
                if (item.Selected) count++;
            GridSelectionLabel.Text = $"{count} items selected";
            GridClearButton.IsEnabled = count > 0;
        }

        // Trees tab
        private void LoadListData()
        {
            TestListView.Items.Add(new FileItem { Name = "Document.pdf", Type = "PDF", Size = "2.4 MB" });
            TestListView.Items.Add(new FileItem { Name = "Photo.jpg", Type = "Image", Size = "4.1 MB" });
            TestListView.Items.Add(new FileItem { Name = "Data.csv", Type = "CSV", Size = "156 KB" });
            TestListView.Items.Add(new FileItem { Name = "Report.docx", Type = "Word", Size = "890 KB" });
        }
    }

    public class GridItem : INotifyPropertyChanged
    {
        private bool _selected;
        public bool Selected
        {
            get => _selected;
            set { _selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected))); }
        }
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Value { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class FileItem
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Size { get; set; } = "";
    }
}
