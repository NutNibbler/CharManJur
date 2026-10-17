using CharManJur.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class LanguageDisplay : INotifyPropertyChanged
{
    public Language Language { get; set; } = new();

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    // Helper properties for binding
    public int Id => Language.Id;
    public string Name => Language.Name;
    public string Description => Language.Description;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}