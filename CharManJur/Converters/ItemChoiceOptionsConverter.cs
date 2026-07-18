using CharManJur.Models;
using CharManJur.ViewModels;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System.Globalization;

namespace CharManJur.Converters;

public class ItemChoiceOptionsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // value is the ItemChoice object
        // parameter is the ViewModel (BackgroundSelectionViewModel) containing the dictionary
        if (value is ItemChoice choice && parameter is BackgroundSelectionViewModel viewModel)
        {
            if (viewModel.ItemChoiceSelections.TryGetValue(choice.Id, out var options))
            {
                return options;
            }
        }
        return new List<SelectableItem>();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}