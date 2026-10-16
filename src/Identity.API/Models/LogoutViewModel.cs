namespace eShop.Identity.API.Models
{
    public class LogoutViewModel
    {
        /// <summary>
        /// Where the confirming request goes: the server's end-session endpoint.
        /// </summary>
        public Uri EndSessionEndpoint { get; set; }

        /// <summary>
        /// The parameters of the logout request being confirmed, the value the server issued for the answer among
        /// them, sent back unchanged when the user agrees.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> Parameters { get; set; }
    }
}
