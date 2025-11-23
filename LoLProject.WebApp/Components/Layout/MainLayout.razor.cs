using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using LoLProject.WebApp.Clients;

namespace LoLProject.WebApp.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        // Injection via propriété (Clean Code pour le Code-Behind)
        [Inject]
        public NavigationManager Navigation { get; set; } = default!;

        [Inject]
        public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        [Inject]
        public LoLClient LoLClient { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is { IsAuthenticated: true })
            {
                try
                {
                    await LoLClient.SyncUserAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur sync user: {ex.Message}");
                }
            }
        }

        private void Login()
        {
            var returnUrl = Uri.EscapeDataString(Navigation.Uri);
            Navigation.NavigateTo($"authentication/login?returnUrl={returnUrl}", forceLoad: true);
        }

        private void Logout()
        {
            Navigation.NavigateTo("/authentication/logout", forceLoad: true);
        }
    }
}