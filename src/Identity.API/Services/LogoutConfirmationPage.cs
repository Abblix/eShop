using Abblix.Oidc.Server.Common;
using Abblix.Oidc.Server.Endpoints.EndSession.Interfaces;
using Abblix.Oidc.Server.MinimalApi.Formatters.Interfaces;
using Abblix.Utils;
using Microsoft.AspNetCore.WebUtilities;
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
    public class LogoutConfirmationPage(
        IEndSessionResponseFormatter inner,
        IHttpContextAccessor httpContextAccessor,
        LinkGenerator linkGenerator) : IEndSessionResponseFormatter
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
                return Task.FromResult(Results.Redirect(PageUrl(request, confirmation.Confirmation)));

            return inner.FormatResponseAsync(request, response);
        }

        /// <summary>
        /// The page's address with the parameters it posts back as its query.
        /// </summary>
        /// <remarks>
        /// A path, not an absolute address, so the browser stays on the scheme and host it reached this server on
        /// rather than one rebuilt from the request's Host header. The path comes from the page's action, so it
        /// follows whatever route maps it and the application's base path.
        /// </remarks>
        private string PageUrl(EndSessionRequest request, string confirmation)
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

            var httpContext = httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException("The logout confirmation is formatted outside a request.");
            var path = linkGenerator.GetPathByAction(httpContext, nameof(Controllers.AccountController.Logout), "Account")
                ?? throw new InvalidOperationException("The logout page is not mapped.");

            return QueryHelpers.AddQueryString(path, parameters.Where(parameter => parameter.Value != null));
        }
    }
}
