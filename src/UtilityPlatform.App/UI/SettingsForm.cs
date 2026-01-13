using System.Drawing;
using System.Windows.Forms;
using UtilityPlatform.App.PowerToys;
using UtilityPlatform.Core.PowerToys;

namespace UtilityPlatform.App.UI;

public class SettingsForm(PowerToyManager powerToyManager) : Form
{
    private readonly PowerToyManager _powerToyManager = powerToyManager ?? throw new ArgumentNullException(nameof(powerToyManager));

    private readonly ListView _powerToysListView = new()
    {
        Dock = DockStyle.Fill,
        View = View.List,
        CheckBoxes = true
    };

    private readonly Label _descriptionLabel = new()
    {
        Dock = DockStyle.Top,
        AutoSize = false,
        Height = 60
    };

    private readonly Panel _settingsPanel = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true
    };

    private bool _isUpdatingList;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        Text = "Utility Platform Settings";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);

        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 300,
            FixedPanel = FixedPanel.Panel1
        };

        splitContainer.Panel1.Controls.Add(_powerToysListView);

        var rightPanel = new Panel { Dock = DockStyle.Fill };
        rightPanel.Controls.Add(_settingsPanel);
        rightPanel.Controls.Add(_descriptionLabel);

        splitContainer.Panel2.Controls.Add(rightPanel);

        Controls.Add(splitContainer);

        _powerToysListView.ItemChecked += PowerToysListViewOnItemChecked;
        _powerToysListView.SelectedIndexChanged += (_, _) => ShowSelectedPowerToy();

        RefreshPowerToyList();
        ShowSelectedPowerToy();
    }

    private void RefreshPowerToyList()
    {
        _isUpdatingList = true;

        try
        {
            _powerToysListView.Items.Clear();

            foreach (var powerToy in _powerToyManager.GetPowerToys())
            {
                var item = new ListViewItem(powerToy.DisplayName)
                {
                    Checked = powerToy.IsEnabled,
                    Tag = powerToy.Id
                };

                _powerToysListView.Items.Add(item);
            }

            if (_powerToysListView.Items.Count > 0 && _powerToysListView.SelectedItems.Count == 0)
            {
                _powerToysListView.Items[0].Selected = true;
            }
        }
        finally
        {
            _isUpdatingList = false;
        }
    }

    private async void PowerToysListViewOnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_isUpdatingList)
        {
            return;
        }

        if (e.Item.Tag is not string id)
        {
            return;
        }

        await _powerToyManager.SetEnabledAsync(id, e.Item.Checked, CancellationToken.None);
        ShowSelectedPowerToy();
    }

    private void ShowSelectedPowerToy()
    {
        if (_powerToysListView.SelectedItems.Count == 0)
        {
            _descriptionLabel.Text = string.Empty;
            _settingsPanel.Controls.Clear();
            return;
        }

        var selected = _powerToysListView.SelectedItems[0];

        if (selected.Tag is not string id)
        {
            return;
        }

        var details = _powerToyManager.GetPowerToyDetails(id);

        _descriptionLabel.Text = details.Descriptor.Description;

        _settingsPanel.SuspendLayout();
        _settingsPanel.Controls.Clear();

        if (details.SettingDefinitions.Count == 0)
        {
            _settingsPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Top,
                Text = "No settings available for this power toy.",
                Padding = new Padding(10)
            });

            _settingsPanel.ResumeLayout();
            return;
        }

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(10)
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        for (var i = 0; i < details.SettingDefinitions.Count; i++)
        {
            var definition = details.SettingDefinitions[i];

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var label = new Label
            {
                Text = definition.DisplayName,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 10, 3, 3)
            };

            var control = CreateSettingControl(id, definition, details.Settings);
            control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            control.Margin = new Padding(3, 6, 3, 3);

            table.Controls.Add(label, 0, i);
            table.Controls.Add(control, 1, i);
        }

        _settingsPanel.Controls.Add(table);
        _settingsPanel.ResumeLayout();
    }

    private Control CreateSettingControl(string powerToyId, PowerToySettingDefinition definition, IReadOnlyDictionary<string, string?> currentSettings)
    {
        var value = currentSettings.TryGetValue(definition.Key, out var v) ? v : definition.DefaultValue?.ToString();

        return definition.SettingType switch
        {
            PowerToySettingType.Boolean => CreateBooleanControl(powerToyId, definition.Key, value, definition.Description),
            PowerToySettingType.String => CreateStringControl(powerToyId, definition.Key, value, definition.Description),
            PowerToySettingType.Int32 => CreateInt32Control(powerToyId, definition.Key, value, definition.Description),
            _ => CreateStringControl(powerToyId, definition.Key, value, definition.Description)
        };
    }

    private Control CreateBooleanControl(string powerToyId, string key, string? value, string description)
    {
        var checkbox = new CheckBox
        {
            Text = description,
            AutoSize = true,
            Checked = bool.TryParse(value, out var b) && b
        };

        checkbox.CheckedChanged += async (_, _) =>
            await _powerToyManager.UpdateSettingAsync(powerToyId, key, checkbox.Checked.ToString(), CancellationToken.None);

        return checkbox;
    }

    private Control CreateStringControl(string powerToyId, string key, string? value, string description)
    {
        var textbox = new TextBox
        {
            Text = value ?? string.Empty,
            Width = 300
        };

        var toolTip = new ToolTip();
        toolTip.SetToolTip(textbox, description);

        textbox.TextChanged += async (_, _) =>
            await _powerToyManager.UpdateSettingAsync(powerToyId, key, textbox.Text, CancellationToken.None);

        return textbox;
    }

    private Control CreateInt32Control(string powerToyId, string key, string? value, string description)
    {
        var numeric = new NumericUpDown
        {
            Width = 120,
            Minimum = int.MinValue,
            Maximum = int.MaxValue,
            Value = int.TryParse(value, out var i) ? i : 0
        };

        var toolTip = new ToolTip();
        toolTip.SetToolTip(numeric, description);

        numeric.ValueChanged += async (_, _) =>
            await _powerToyManager.UpdateSettingAsync(powerToyId, key, numeric.Value.ToString(), CancellationToken.None);

        return numeric;
    }
}

