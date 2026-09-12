using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Shared.Requests;
using Shared.Responses;

namespace CityMapApp.Components.Pages;

public partial class Home
{
    private SubmissionFormModel _request = new();
    private string? _message = null;
    private bool _loading = true;
    private bool _hasSubmitted;
    private bool _submitting;
    private static readonly string[] _states = ["AL", "AK", "AZ", "AR", "CA", "CO", "CT", "DE", "FL", "GA", "HI", "ID", "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MA", "MI", "MN", "MS", "MO", "MT", "NE", "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "OH", "OK", "OR", "PA", "RI", "SC", "SD", "TN", "TX", "UT", "VT", "VA", "WA", "WV", "WI", "WY", "DC"];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var r = await Http.GetFromJsonAsync<SubmissionStatusResponse>("/api/submissions/me");
            _hasSubmitted = r?.HasSubmitted ?? false;
        }
        catch (HttpRequestException)
        {
            _message = "We couldn't check your status. Please try again.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SubmitAsync(EditContext c)
    {
        _message = null;
        if (!c.Validate())
            return; _submitting = true;

        try
        {
            var response = await Http.PostAsJsonAsync("/api/submissions", new SubmissionRequest(_request.City, _request.State, _request.Name, _request.EmailAddress));
            var payload = await response.Content.ReadFromJsonAsync<SubmissionResponse>(); if (payload?.Success == true) { Navigation.NavigateTo("/map", true); return; }
            _message = payload?.Message ?? "Unable to add your location right now.";
        }
        catch (Exception)
        {
            _message = "Unable to connect. Please try again.";
        }
        finally
        {
            _submitting = false;
        }
    }
    private sealed class SubmissionFormModel
    {
        [Required, StringLength(50)]
        public string City { get; set; } = "";

        [Required, StringLength(2)]
        public string State { get; set; } = "";

        [StringLength(100)]
        public string? Name { get; set; }

        [EmailAddress, StringLength(254)]
        public string? EmailAddress { get; set; }
    }
}
