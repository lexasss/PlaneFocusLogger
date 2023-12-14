using System;
using System.Windows.Controls;

namespace EMirrorsScores.Utils;

/// <summary>
/// Initilizes and sets changing action of some types of UI widgets
/// </summary>
internal static class UIHelper
{
    public delegate void ItemChanged<T>(T value);

    /// <summary>
    /// Initializes a <see cref="ComboBox"/>. If the selected item is enum, it initializes the items with values from this enum
    /// </summary>
    /// <typeparam name="T">enum or other type that is assigned to the <see cref="ComboBox"/> items</typeparam>
    /// <param name="cmb">The <see cref="ComboBox"/> instance</param>
    /// <param name="selected">The selected item</param>
    /// <param name="changedAction">Callback for SelectionChanged event</param>
    public static void InitComboBox<T>(ComboBox cmb, T selected, ItemChanged<T> changedAction)
    {
        if (typeof(T).IsEnum)
        {
            foreach (var item in Enum.GetNames(typeof(T)))
            {
                cmb.Items.Add(item);
            }
        }

        cmb.SelectedItem = selected?.ToString();
#pragma warning disable CS8604 // Possible null reference argument.
        cmb.SelectionChanged += (s, e) => changedAction(typeof(T).IsEnum ? 
            (T)Enum.Parse(typeof(T), (string?)cmb.SelectedItem) :
            (T?)cmb.SelectedItem);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    /// <summary>
    /// Initializes a <see cref="CheckBox"/>
    /// </summary>
    /// <param name="chk">The <see cref="CheckBox"/> instance</param>
    /// <param name="isChecked">IsChecked value</param>
    /// <param name="changedAction">Callback for Checked and Unchecked events</param>
    public static void InitCheckBox(CheckBox chk, bool isChecked, ItemChanged<bool> changedAction)
    {
        chk.IsChecked = isChecked;
        chk.Checked += (s, e) => changedAction(chk.IsChecked ?? false);
        chk.Unchecked += (s, e) => changedAction(chk.IsChecked ?? false);
    }

    /// <summary>
    /// Initializes a <see cref="TextBox"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="txb">The <see cref="TextBox"/> instance</param>
    /// <param name="value">Initial text value. Supports <see cref="string"/>, <see cref="int"/> and <see cref="double"/> types</param>
    /// <param name="changedAction">Callback for TextChanged event</param>
    public static void InitTextBox<T>(TextBox txb, T value, ItemChanged<T> changedAction)
    {
        txb.Text = value?.ToString();
        txb.TextChanged += (s, e) =>
        {
            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(txb.Text, out int value))
                    changedAction((T)(value as object));
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(txb.Text, out double value))
                    changedAction((T)(value as object));
            }
            else if (typeof(T) == typeof(string))
            {
                var obj = txb.Text as object;
                changedAction((T)obj!);
            }
            else
            {
                throw new Exception("UIHelper.InitTextBox: unsupported type");
            }
        };
    }
}
