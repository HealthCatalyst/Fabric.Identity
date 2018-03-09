namespace Fabric.Identity.Client.Extensions
{
    using System.Net.Http;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    /// <summary>
    /// HttpResponseMessage extensions.
    /// </summary>
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// The deserialize content async.
        /// </summary>
        /// <param name="responseMessage">The response message.</param>
        /// <typeparam name="T">The type to deserialize </typeparam>
        /// <returns>The <see cref="Task"/>. </returns>
        public static async Task<T> DeserializeContentAsync<T>(this HttpResponseMessage responseMessage)
        {
            return JsonConvert.DeserializeObject<T>(
                await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
    }
}
