namespace eShop.Identity.API.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// Opaque reference to the authorization request the user was interrupted from.
        /// </summary>
        /// <remarks>
        /// The server keeps the request itself; the browser only ever carries this reference,
        /// so the login form cannot be used to smuggle a modified authorization request back in.
        /// </remarks>
        public string RequestUri { get; set; }
    }
}
