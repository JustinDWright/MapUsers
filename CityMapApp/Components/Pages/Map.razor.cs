using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Shared.DTOs;

namespace CityMapApp.Components.Pages;

public partial class Map
{
    private readonly List<MapPinDto> _pins = [];
    private readonly List<MapUserDto> _users = [];
    private bool _loading = true;
    private bool _mapRendered;
    private bool _usersLoading;
    private MapPinDto? _selectedPin;

    protected override async Task OnInitializedAsync()
    {
        var pins = await Http.GetFromJsonAsync<List<MapPinDto>>("/api/map") ?? [];

        _pins.Clear();
        _pins.AddRange(pins);
        _loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_mapRendered || _loading || _pins.Count == 0)
        {
            return;
        }

        _mapRendered = true;
        await Js.InvokeVoidAsync("cityMapApp.renderMap", "city-map", _pins);
    }

    private async Task ShowUsersAsync(MapPinDto pin)
    {
        _selectedPin = pin;
        _usersLoading = true;
        _users.Clear();

        var url = $"/api/map/users?city={Uri.EscapeDataString(pin.City)}&state={Uri.EscapeDataString(pin.State)}";
        var users = await Http.GetFromJsonAsync<List<MapUserDto>>(url) ?? [];
        _users.AddRange(users);
        _usersLoading = false;
    }

    private Task HandleCityLinkKeyAsync(KeyboardEventArgs args, MapPinDto pin)
    {
        return args.Key is "Enter" or " " ? ShowUsersAsync(pin) : Task.CompletedTask;
    }

    private void CloseUsers()
    {
        _selectedPin = null;
        _users.Clear();
    }
}
