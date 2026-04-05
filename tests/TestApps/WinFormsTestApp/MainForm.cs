using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsTestApp
{
    /// <summary>
    /// Test application for FlaUI-MCP integration tests.
    /// Each tab exercises a different set of MCP tool capabilities.
    /// </summary>
    public class MainForm : Form
    {
        private TabControl _tabs = null!;

        public MainForm()
        {
            Text = "FlaUI-MCP Test App";
            Name = "MainForm";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;

            _tabs = new TabControl { Dock = DockStyle.Fill, Name = "MainTabs" };
            Controls.Add(_tabs);

            _tabs.TabPages.Add(CreateButtonsTab());
            _tabs.TabPages.Add(CreateFormsTab());
            _tabs.TabPages.Add(CreateGridTab());
            _tabs.TabPages.Add(CreateTreeTab());
            _tabs.TabPages.Add(CreateDialogTab());
        }

        /// <summary>
        /// Tab 1: Buttons with various states and patterns.
        /// Tests: windows_click (invoke/mouse/toggle), windows_find (by role/state),
        ///        windows_get_value (toggle state), windows_snapshot (state indicators)
        /// </summary>
        private TabPage CreateButtonsTab()
        {
            var tab = new TabPage("Buttons") { Name = "ButtonsTab" };
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10),
                AutoScroll = true
            };
            tab.Controls.Add(layout);

            // Simple button with click counter
            var clickCount = 0;
            var counterLabel = new Label
            {
                Text = "Click count: 0",
                Name = "ClickCountLabel",
                AutoSize = true
            };
            var clickButton = new Button
            {
                Text = "Click Me",
                Name = "ClickMeButton",
                AutoSize = true
            };
            clickButton.Click += (s, e) =>
            {
                clickCount++;
                counterLabel.Text = $"Click count: {clickCount}";
            };
            layout.Controls.Add(clickButton);
            layout.Controls.Add(counterLabel);

            // Button that enables/disables based on checkbox
            var enableCheckbox = new CheckBox
            {
                Text = "Enable the button below",
                Name = "EnableCheckbox",
                Checked = false,
                AutoSize = true
            };
            var conditionalButton = new Button
            {
                Text = "Conditional Button",
                Name = "ConditionalButton",
                Enabled = false,
                AutoSize = true
            };
            enableCheckbox.CheckedChanged += (s, e) =>
            {
                conditionalButton.Enabled = enableCheckbox.Checked;
            };
            layout.Controls.Add(new Label { Text = "", AutoSize = true }); // spacer
            layout.Controls.Add(enableCheckbox);
            layout.Controls.Add(conditionalButton);

            // Radio buttons
            var radioGroup = new GroupBox
            {
                Text = "Radio Group",
                Name = "RadioGroup",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            var radioLayout = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Dock = DockStyle.Fill
            };
            radioGroup.Controls.Add(radioLayout);
            var radio1 = new RadioButton { Text = "Option A", Name = "RadioA", Checked = true, AutoSize = true };
            var radio2 = new RadioButton { Text = "Option B", Name = "RadioB", AutoSize = true };
            var radio3 = new RadioButton { Text = "Option C", Name = "RadioC", AutoSize = true };
            radioLayout.Controls.Add(radio1);
            radioLayout.Controls.Add(radio2);
            radioLayout.Controls.Add(radio3);
            layout.Controls.Add(radioGroup);

            return tab;
        }

        /// <summary>
        /// Tab 2: Form controls for text input and values.
        /// Tests: windows_type, windows_fill, windows_get_text, windows_get_value,
        ///        windows_press_key, windows_find (by automationId)
        /// </summary>
        private TabPage CreateFormsTab()
        {
            var tab = new TabPage("Forms") { Name = "FormsTab" };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Padding = new Padding(10),
                AutoScroll = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tab.Controls.Add(layout);

            // Text input
            layout.Controls.Add(new Label { Text = "Name:", AutoSize = true, Anchor = AnchorStyles.Left });
            var nameBox = new TextBox { Name = "NameTextBox", Width = 200 };
            nameBox.AccessibleName = "Name";
            layout.Controls.Add(nameBox);

            // Password input
            layout.Controls.Add(new Label { Text = "Password:", AutoSize = true, Anchor = AnchorStyles.Left });
            var passBox = new TextBox { Name = "PasswordTextBox", Width = 200, UseSystemPasswordChar = true };
            passBox.AccessibleName = "Password";
            layout.Controls.Add(passBox);

            // Multiline text
            layout.Controls.Add(new Label { Text = "Notes:", AutoSize = true, Anchor = AnchorStyles.Left });
            var notesBox = new TextBox
            {
                Name = "NotesTextBox",
                Multiline = true,
                Height = 80,
                Width = 200,
                ScrollBars = ScrollBars.Vertical
            };
            notesBox.AccessibleName = "Notes";
            layout.Controls.Add(notesBox);

            // ComboBox
            layout.Controls.Add(new Label { Text = "Status:", AutoSize = true, Anchor = AnchorStyles.Left });
            var combo = new ComboBox { Name = "StatusComboBox", Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            combo.AccessibleName = "Status";
            combo.Items.AddRange(new object[] { "Active", "Inactive", "Pending" });
            combo.SelectedIndex = 0;
            layout.Controls.Add(combo);

            // Slider
            layout.Controls.Add(new Label { Text = "Volume:", AutoSize = true, Anchor = AnchorStyles.Left });
            var slider = new TrackBar
            {
                Name = "VolumeSlider",
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Width = 200
            };
            slider.AccessibleName = "Volume";
            layout.Controls.Add(slider);

            // Read-only display
            layout.Controls.Add(new Label { Text = "Result:", AutoSize = true, Anchor = AnchorStyles.Left });
            var resultBox = new TextBox
            {
                Name = "ResultTextBox",
                Width = 200,
                ReadOnly = true,
                Text = "Computed value here"
            };
            resultBox.AccessibleName = "Result";
            layout.Controls.Add(resultBox);

            return tab;
        }

        /// <summary>
        /// Tab 3: DataGridView with test data.
        /// Tests: windows_table, windows_snapshot (maxTableRows), windows_find (in grids),
        ///        windows_get_value (cell values), header detection
        /// </summary>
        private TabPage CreateGridTab()
        {
            var tab = new TabPage("Grid") { Name = "GridTab" };
            var layout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            tab.Controls.Add(layout);

            var grid = new DataGridView
            {
                Name = "TestDataGrid",
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.AccessibleName = "Test Data";

            // Add columns
            var selectCol = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Select",
                Width = 50
            };
            grid.Columns.Add(selectCol);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ID",
                HeaderText = "ID",
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Name",
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Category",
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value",
                ReadOnly = true
            });

            // Add test data - enough rows to test limiting
            var categories = new[] { "Alpha", "Beta", "Gamma", "Delta" };
            var rng = new Random(42); // deterministic seed
            for (int i = 0; i < 50; i++)
            {
                grid.Rows.Add(
                    false,
                    $"ITEM-{i + 1:D3}",
                    $"Test Item {i + 1}",
                    categories[i % categories.Length],
                    $"{rng.Next(1, 1000)}"
                );
            }

            // Status bar showing selection count
            var statusPanel = new Panel { Dock = DockStyle.Bottom, Height = 35 };
            var selectionLabel = new Label
            {
                Text = "0 items selected",
                Name = "SelectionCountLabel",
                AutoSize = true,
                Location = new Point(10, 8)
            };
            var selectAllButton = new Button
            {
                Text = "Select All",
                Name = "SelectAllButton",
                Location = new Point(200, 5),
                Enabled = true
            };
            var clearButton = new Button
            {
                Text = "Clear Selection",
                Name = "ClearSelectionButton",
                Location = new Point(300, 5),
                Enabled = false
            };

            grid.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex == 0) // Select column
                {
                    var count = 0;
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.Cells[0].Value is true) count++;
                    }
                    selectionLabel.Text = $"{count} items selected";
                    clearButton.Enabled = count > 0;
                }
            };
            grid.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            selectAllButton.Click += (s, e) =>
            {
                foreach (DataGridViewRow row in grid.Rows)
                    row.Cells[0].Value = true;
            };
            clearButton.Click += (s, e) =>
            {
                foreach (DataGridViewRow row in grid.Rows)
                    row.Cells[0].Value = false;
            };

            statusPanel.Controls.Add(selectionLabel);
            statusPanel.Controls.Add(selectAllButton);
            statusPanel.Controls.Add(clearButton);
            layout.Controls.Add(grid);
            layout.Controls.Add(statusPanel);

            return tab;
        }

        /// <summary>
        /// Tab 4: TreeView and ListView.
        /// Tests: windows_snapshot (tree depth), windows_click (expand/collapse),
        ///        windows_find (treeitem role), windows_get_text
        /// </summary>
        private TabPage CreateTreeTab()
        {
            var tab = new TabPage("Trees") { Name = "TreesTab" };
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 300
            };
            tab.Controls.Add(split);

            // TreeView
            var tree = new TreeView
            {
                Name = "TestTreeView",
                Dock = DockStyle.Fill
            };
            tree.AccessibleName = "Category Tree";
            var root1 = tree.Nodes.Add("Fruits");
            root1.Nodes.Add("Apple");
            root1.Nodes.Add("Banana");
            root1.Nodes.Add("Cherry");
            var root2 = tree.Nodes.Add("Vegetables");
            root2.Nodes.Add("Carrot");
            root2.Nodes.Add("Broccoli");
            var root3 = tree.Nodes.Add("Grains");
            root3.Nodes.Add("Rice");
            root3.Nodes.Add("Wheat");
            root3.Nodes.Add("Oats");
            tree.ExpandAll();
            split.Panel1.Controls.Add(tree);

            // ListView
            var list = new ListView
            {
                Name = "TestListView",
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true
            };
            list.AccessibleName = "Item List";
            list.Columns.Add("Name", 150);
            list.Columns.Add("Type", 100);
            list.Columns.Add("Size", 80);
            list.Items.Add(new ListViewItem(new[] { "Document.pdf", "PDF", "2.4 MB" }));
            list.Items.Add(new ListViewItem(new[] { "Photo.jpg", "Image", "4.1 MB" }));
            list.Items.Add(new ListViewItem(new[] { "Data.csv", "CSV", "156 KB" }));
            list.Items.Add(new ListViewItem(new[] { "Report.docx", "Word", "890 KB" }));
            split.Panel2.Controls.Add(list);

            return tab;
        }

        /// <summary>
        /// Tab 5: Dialog launchers for testing window management.
        /// Tests: windows_wait (exists/gone), windows_launch, windows_close,
        ///        windows_list_windows, windows_focus
        /// </summary>
        private TabPage CreateDialogTab()
        {
            var tab = new TabPage("Dialogs") { Name = "DialogsTab" };
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10)
            };
            tab.Controls.Add(layout);

            // Button that opens a modal dialog
            var modalButton = new Button
            {
                Text = "Open Modal Dialog",
                Name = "OpenModalButton",
                AutoSize = true
            };
            modalButton.Click += (s, e) =>
            {
                using var dialog = new Form
                {
                    Text = "Test Modal Dialog",
                    Name = "TestModalDialog",
                    Size = new Size(300, 200),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                var okButton = new Button
                {
                    Text = "OK",
                    Name = "OKButton",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, 120)
                };
                dialog.Controls.Add(okButton);
                dialog.Controls.Add(new Label
                {
                    Text = "This is a test modal dialog.",
                    Name = "DialogLabel",
                    Location = new Point(20, 30),
                    AutoSize = true
                });
                dialog.AcceptButton = okButton;
                dialog.ShowDialog(this);
            };
            layout.Controls.Add(modalButton);

            // Button that opens a modeless dialog
            var modelessButton = new Button
            {
                Text = "Open Modeless Dialog",
                Name = "OpenModelessButton",
                AutoSize = true
            };
            modelessButton.Click += (s, e) =>
            {
                var dialog = new Form
                {
                    Text = "Test Modeless Dialog",
                    Name = "TestModelessDialog",
                    Size = new Size(300, 200),
                    StartPosition = FormStartPosition.CenterParent
                };
                var closeButton = new Button
                {
                    Text = "Close",
                    Name = "CloseDialogButton",
                    Location = new Point(100, 120)
                };
                closeButton.Click += (s2, e2) => dialog.Close();
                dialog.Controls.Add(closeButton);
                dialog.Controls.Add(new Label
                {
                    Text = "This is a modeless dialog.\nClose it to test wait-gone.",
                    Name = "ModelessDialogLabel",
                    Location = new Point(20, 30),
                    AutoSize = true
                });
                dialog.Show(this);
            };
            layout.Controls.Add(modelessButton);

            // Status text that updates
            var statusLabel = new Label
            {
                Text = "Ready",
                Name = "DialogStatusLabel",
                AutoSize = true
            };
            layout.Controls.Add(new Label { Text = "", AutoSize = true }); // spacer
            layout.Controls.Add(statusLabel);

            return tab;
        }
    }
}
