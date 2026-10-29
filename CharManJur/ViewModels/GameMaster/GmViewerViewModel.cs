using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.ViewModels;

// The Player Viewer panel: pick a connected player, and their character screen opens over the whole dashboard.
public class GmViewerViewModel : INotifyPropertyChanged
{
    private readonly IGmCharacterService _characters;
    private readonly IItemDataService _items;

    private ObservableCollection<GmPlayerRow>? _players;
    private bool _busy;
    private bool _isOpening;
    private string _openingText = string.Empty;

    public GmViewerViewModel(IGmCharacterService characters, IItemDataService items)
    {
        _characters = characters;
        _items = items;
    }

    public ObservableCollection<GmPlayerRow>? Players => _players;
    public bool HasNoPlayers => _players == null || _players.Count == 0;

    // Shown while the character screen is being built, which keeps the app busy for a moment.
    public bool IsOpening
    {
        get => _isOpening;
        private set { _isOpening = value; OnPropertyChanged(); }
    }

    public string OpeningText
    {
        get => _openingText;
        private set { _openingText = value; OnPropertyChanged(); }
    }

    // Wired up by the dashboard.
    public void Attach(ObservableCollection<GmPlayerRow> players)
    {
        _players = players;
        players.CollectionChanged += (_, _) => MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(HasNoPlayers)));
        OnPropertyChanged(nameof(Players));
        OnPropertyChanged(nameof(HasNoPlayers));
    }

    public async Task ViewAsync(GmPlayerRow row)
    {
        if (_busy) return;
        _busy = true;
        try
        {
            OpeningText = $"Opening {row.Title}…";
            IsOpening = true;
            await Task.Delay(60);   // lets the message paint before the page is built

            var watch = System.Diagnostics.Stopwatch.StartNew();

            var source = await _characters.OpenViewerAsync(row.Save);
            if (source == null)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Player Not Found", $"{row.Title}'s save file wasn't found.", "OK");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"[VIEWER] character loaded: {watch.ElapsedMilliseconds} ms");

            // The player's own character screen, on a copy that is never saved or logged.
            var vm = new CharacterHomeViewModel(source.Svc, source.Persistence, _items, source.Log);

            CharacterHomePage? page = null;
            vm.CloseViewerAction = () =>
            {
                if (page != null) _ = page.CloseViewerAsync();
            };

            page = new CharacterHomePage(vm);
            System.Diagnostics.Debug.WriteLine($"[VIEWER] page built: {watch.ElapsedMilliseconds} ms");

            await Application.Current!.MainPage!.Navigation.PushModalAsync(page, false);   // the page slides itself in
            System.Diagnostics.Debug.WriteLine($"[VIEWER] page pushed: {watch.ElapsedMilliseconds} ms");
        }
        finally
        {
            IsOpening = false;
            _busy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}