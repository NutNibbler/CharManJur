using System;

namespace CharManJur.Services;

public interface IGlobalMenuDataService
{
    bool NeedsUIReset { get; set; }
    bool NeedsDataLoad { get; set; }
    bool IsInCharacterCreation { get; set; }

    void CharBuilderResetRequest();
    void CharBuilderLoadRequest();
    void SetCharacterCreationMode(bool isInCharacterCreation);
}

public class GlobalMenuDataService : IGlobalMenuDataService
{
    private bool _needsUIReset = false;
    private bool _needsDataLoad = false;
    private bool _isInCharacterCreation = false;  // Default to false (live game mode)

    public bool NeedsUIReset
    {
        get => _needsUIReset;
        set => _needsUIReset = value;
    }

    public bool NeedsDataLoad
    {
        get => _needsDataLoad;
        set => _needsDataLoad = value;
    }

    public bool IsInCharacterCreation
    {
        get => _isInCharacterCreation;
        set
        {
            _isInCharacterCreation = value;
            System.Diagnostics.Debug.WriteLine($"=== IsInCharacterCreation set to: {value} ===");
        }
    }

    public bool IsInCharacterCreationINV
    {
        get => _isInCharacterCreation;
        set
        {
            _isInCharacterCreation = !value;
            System.Diagnostics.Debug.WriteLine($"=== IsInCharacterCreationINV set to: {!value} ===");
        }
    }

    public void CharBuilderResetRequest()
    {
        System.Diagnostics.Debug.WriteLine("=== CharBuilderResetRequest() CALLED ===");
        _needsUIReset = true;
        _needsDataLoad = false;
        _isInCharacterCreation = true;
        System.Diagnostics.Debug.WriteLine($"=== IsInCharacterCreation set to: true (via CharBuilderResetRequest) ===");
    }

    public void CharBuilderLoadRequest()
    {
        System.Diagnostics.Debug.WriteLine("=== CharBuilderLoadRequest() CALLED ===");
        _needsDataLoad = true;
        _needsUIReset = false;
        _isInCharacterCreation = true;
        System.Diagnostics.Debug.WriteLine($"=== IsInCharacterCreation set to: true (via CharBuilderLoadRequest) ===");
    }

    public void SetCharacterCreationMode(bool isInCharacterCreation)
    {
        _isInCharacterCreation = isInCharacterCreation;
        System.Diagnostics.Debug.WriteLine($"=== SetCharacterCreationMode: {isInCharacterCreation} ===");
    }
}