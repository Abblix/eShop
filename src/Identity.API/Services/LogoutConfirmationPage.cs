using Abblix.Oidc.Server.Common;
using Abblix.Oidc.Server.Endpoints.EndSession.Interfaces;
using Abblix.Oidc.Server.MinimalApi.Formatters.Interfaces;
using Abblix.Utils;
using EndSessionRequest = Abblix.Oidc.Server.Model.EndSessionRequest;

namespace eShop.Identity.API.Services
{
    /// <summary>
    /// Sends a logout the end user has to confirm to this application's own confirmation page.
    /// </summary>
    /// <remarks>
    /// The library answers such a request with the value the confirming request has to carry back, as a JSON body,
    /// which a browser following a client's sign-out would show as it is. This decorator redirects to the page
    /// instead and hands it that value together with the parameters of the original request, so that the page can
    /// send all of them back to the end-session endpoint once the user agrees.
    /// </remarks>
    public class LogoutConfirmationPage(IEndSessionResponseFormatter inner) : IEndSessionResponseFormatter
    {
        /// <summary>
        /// Marks a page shown again because the answer the user gave was no longer accepted, typically because
        /// it took longer than the confirmation lasts. It belongs to this page, not to the protocol, so the page
        /// does not send it back.
        /// </summary>
        public const string AskedAgain = "asked_again";

        public Task<IResult> FormatResponseAsync(
            EndSessionRequest request, Result<IEndSessionResponse, OidcError> response)
        {
            if (response.TryGetSuccess(out var success) && success is ConfirmationRequired confirmation)
                return Task.FromResult(Results.RedirectToRoute("default", PageValues(request, confirmation.Confirmation)));

            return inner.FormatResponseAsync(request, response);
        }

        /// <summary>
        /// The page's controller and action, and the parameters it posts back as its query.
        /// </summary>
        /// <remarks>
        /// Resolved through the conventional route MapDefaultControllerRoute registers, when the redirect is written,
        /// so the address follows the controller's route and the application's base path; route values the
        /// template does not use become the query string.
        /// </remarks>
        private static RouteValueDictionary PageValues(EndSessionRequest request, string confirmation)
        {
            KeyValuePair<string, string>[] parameters =
            [
                new(AskedAgain, request.Confirmation != null ? bool.TrueString : null),
                new(EndSessionRequest.Parameters.Confirmation, confirmation),
                new(EndSessionRequest.Parameters.IdTokenHint, request.IdTokenHint),
                new(EndSessionRequest.Parameters.LogoutHint, request.LogoutHint),
                new(EndSessionRequest.Parameters.ClientId, request.ClientId),
                new(EndSessionRequest.Parameters.PostLogoutRedirectUri, request.PostLogoutRedirectUri?.OriginalString),
                new(EndSessionRequest.Parameters.State, request.State),
                new(EndSessionRequest.Parameters.UiLocales, request.UiLocales is { } locales
                    ? string.Join(' ', locales.Select(locale => locale.Name))
                    : null),
            ];

            var values = new RouteValueDictionary
            {
                ["controller"] = "Account",
                ["action"] = nameof(Controllers.AccountController.Logout),
            };
            foreach (var (name, value) in parameters.Where(parameter => parameter.Value != null))
                values[name] = value;

            return values;
        }
    }
}
