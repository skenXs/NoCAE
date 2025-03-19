using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NoCAE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IPublicClientApplication _clientApp = null;
        StringBuilder sbLog = new StringBuilder();
        StringBuilder sbIdTokenClaims = new StringBuilder();
        StringBuilder sbResponse = new StringBuilder();
        StringBuilder sbResults = new StringBuilder();

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Call Login
        /// </summary>
        private async void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            LogText.Text = string.Empty;
            sbLog.Clear();

            if (_clientApp != null)
            {
                var accounts = await _clientApp.GetAccountsAsync();
                if (accounts.Any())
                {
                    try
                    {
                        await _clientApp.RemoveAsync(accounts.FirstOrDefault());
                        _clientApp = null;
                    }
                    catch (MsalException msalex)
                    {
                        sbLog.AppendLine("Error signing out user: " + msalex.Message);
                    }
                }
            }

            var builder = PublicClientApplicationBuilder.Create(App.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, "common")
                .WithRedirectUri("https://login.microsoftonline.com/common/oauth2/nativeclient");

            _clientApp = builder.Build();
            TokenCacheHelper.EnableSerialization(_clientApp.UserTokenCache);

            string[] scopesRequest = new string[] { "user.read" };
            await AuthAndCallAPI(null, scopesRequest);

            UpdateScreen();
        }

        /// <summary>
        /// Call AcquireToken - to acquire a token requiring user to sign-in
        /// </summary>
        private async void CallProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Set the API Endpoint to Graph 'me' endpoint
            string graphAPIEndpoint = "https://graph.microsoft.com/v1.0/me";

            // Set the scope for API call to user.read
            string[] scopes = new string[] { "user.read" };

            await AuthAndCallAPI(graphAPIEndpoint, scopes);

            UpdateScreen();
        }

        private async Task AuthAndCallAPI(string APIEndpoint, string[] scopes)
        {
            ResultText.Text = "Working...";
            sbResults.Clear();
            sbResponse.Clear();
            sbIdTokenClaims.Clear();

            var accessToken = await GetAccessToken(scopes);
            if (accessToken != null && !string.IsNullOrEmpty(APIEndpoint))
            {
                var results = await GetHttpContentWithToken(APIEndpoint, accessToken, scopes);
                sbResults.Append(results);
            }
        }

        private async Task<string> GetAccessToken(string[] scopes, string claimsChallenge = null)
        {
            IAccount firstAccount = null;

            var accounts = await _clientApp.GetAccountsAsync();
            if (accounts.Any())
            {
                firstAccount = accounts.FirstOrDefault();
            }

            AuthenticationResult authResult = null;
            try
            {
                authResult = await _clientApp.AcquireTokenSilent(scopes, firstAccount)
                    .WithClaims(claimsChallenge)
                    .ExecuteAsync()
                    .ConfigureAwait(false);
            }
            catch (MsalUiRequiredException ex)
            {
                // A MsalUiRequiredException happened on AcquireTokenSilent. 
                // This indicates you need to call AcquireTokenInteractive to acquire a token
                sbLog.AppendLine($"MsalUiRequiredException: {ex.Message}");

                try
                {
                    authResult = await _clientApp.AcquireTokenInteractive(scopes)
                    .WithClaims(claimsChallenge ?? ex.Claims)
                    .WithAccount(firstAccount)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                    ParseIDTokenClaims(authResult);
                    ParseTokenResponseInfo(authResult);
                }
                catch (MsalException msalex)
                {
                    sbLog.AppendLine("Error Acquiring Token: " + msalex.Message);
                    authResult = null;
                }
            }
            catch (Exception ex)
            {
                sbLog.AppendLine("Error Acquiring Token Silently: " + ex.Message);
                return null;
            }

            if (authResult != null)
            {
                ParseIDTokenClaims(authResult);
                ParseTokenResponseInfo(authResult);
                return authResult.AccessToken;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public async Task<string> GetHttpContentWithToken(string url, string token, string[] scopes)
        {
            var httpClient = new HttpClient();
            HttpResponseMessage APIresponse;
            try
            {
                var APIrequest = new HttpRequestMessage(HttpMethod.Get, url);
                // Add the token in Authorization header
                APIrequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                APIresponse = await httpClient.SendAsync(APIrequest);

                if (APIresponse.IsSuccessStatusCode)
                {
                    var content = await APIresponse.Content.ReadAsStringAsync();
                    return content.Replace(",", "," + Environment.NewLine);
                }
                else
                {
                    if (APIresponse.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        && APIresponse.Headers.WwwAuthenticate.Any())
                    {
                        AuthenticationHeaderValue bearer = APIresponse.Headers.WwwAuthenticate.First(v => v.Scheme == "Bearer");
                        IEnumerable<string> parameters = bearer.Parameter.Split(',').Select(v => v.Trim()).ToList();
                        var error = GetParameter(parameters, "error");

                        if (error == "insufficient_claims")
                        {
                            var claimChallengeParameter = GetParameter(parameters, "claims");
                            if (claimChallengeParameter != null)
                            {
                                var claimChallengebase64Bytes = Convert.FromBase64String(claimChallengeParameter);
                                var ClaimChallenge = Encoding.UTF8.GetString(claimChallengebase64Bytes);

                                var newAccessToken = await GetAccessToken(scopes, ClaimChallenge);
                                if (newAccessToken != null)
                                {
                                    var APIrequestAfterCAE = new HttpRequestMessage(HttpMethod.Get, url);
                                    APIrequestAfterCAE.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                                    HttpResponseMessage APIresponseAfterCAE = await httpClient.SendAsync(APIrequestAfterCAE);

                                    if (APIresponseAfterCAE.IsSuccessStatusCode)
                                    {
                                        var content = await APIresponseAfterCAE.Content.ReadAsStringAsync();
                                        return content.Replace(",", "," + Environment.NewLine);
                                    }
                                }
                            }
                        }
                        return APIresponse.StatusCode + " Authorization: " + bearer;
                    }
                    sbLog.AppendLine(APIresponse.StatusCode + " " + await APIresponse.Content.ReadAsStringAsync());
                    return APIresponse.StatusCode + " " + APIresponse.ReasonPhrase;
                }
            }
            catch (Exception ex)
            {
                sbLog.AppendLine(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        private async void SignOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_clientApp != null)
            {
                var accounts = await _clientApp.GetAccountsAsync();
                if (accounts.Any())
                {
                    try
                    {
                        await _clientApp.RemoveAsync(accounts.FirstOrDefault());
                        ResultText.Text = accounts.FirstOrDefault().Username + " User has signed-out";
                        TokenResponseText.Text = string.Empty;
                        IDToken.Text = string.Empty;
                    }
                    catch (MsalException msalex)
                    {
                        sbLog.AppendLine("Error Acquiring Token: " + msalex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Display basic information contained in the token response
        /// </summary>
        private void ParseTokenResponseInfo(AuthenticationResult authResult)
        {
            sbResponse.Clear();
            if (authResult != null)
            {
                sbLog.AppendLine($"Token Response at: {DateTime.Now}");
                sbLog.AppendLine($"Token Type: {authResult.TokenType}");
                sbLog.AppendLine($"Correlation Id: {authResult.CorrelationId}");
                sbLog.AppendLine("---------------------------------------------------------");

                sbResponse.Append("Token Scopes: ");
                foreach (var scope in authResult.Scopes)
                {
                    sbResponse.Append($"{scope} ");
                }
                sbResponse.AppendLine();
                sbResponse.AppendLine();

                sbResponse.AppendLine($"Token Expires: {authResult.ExpiresOn.ToLocalTime()}");
                sbResponse.AppendLine($"Refresh On: {authResult.AuthenticationResultMetadata.RefreshOn}");
                sbResponse.AppendLine();
                sbResponse.Append($"CacheRefreshReason: {authResult.AuthenticationResultMetadata.CacheRefreshReason}");
                sbResponse.Append($", Cache time: {authResult.AuthenticationResultMetadata.DurationInCacheInMs}");
                sbResponse.Append($", HTTP time: {authResult.AuthenticationResultMetadata.DurationInHttpInMs}");
                sbResponse.Append($", Total time: {authResult.AuthenticationResultMetadata.DurationTotalInMs}");

                sbResponse.AppendLine();
                sbResponse.AppendLine($"Tenant Id: {authResult.TenantId}");
                sbResponse.AppendLine($"Unique Id: {authResult.UniqueId}");
                sbResponse.AppendLine($"User name: {authResult.Account.Username}");
                sbResponse.AppendLine($"Environment: {authResult.Account.Environment}");
                sbResponse.AppendLine($"Home Account Id Identifier: {authResult.Account.HomeAccountId.Identifier}");
                sbResponse.AppendLine($"Home Account Id ObjectId: {authResult.Account.HomeAccountId.ObjectId}");
                sbResponse.AppendLine($"Home Account Id TenantId: {authResult.Account.HomeAccountId.TenantId}");

                sbResponse.AppendLine($"ID Token: {authResult.IdToken}");
            }
        }

        private void ParseIDTokenClaims(AuthenticationResult authResult)
        {
            sbIdTokenClaims.Clear();

            foreach (var claim in authResult.ClaimsPrincipal.Claims)
            {
                sbIdTokenClaims.AppendLine($"\"{claim.Type}\": \"{claim.Value}\"");
            }
        }

        private static string GetParameter(IEnumerable<string> parameters, string parameterName)
        {
            int offset = parameterName.Length + 1;
            return parameters.FirstOrDefault(p => p.StartsWith($"{parameterName}="))?.Substring(offset)?.Trim('"');
        }

        private void UpdateScreen()
        {
            ResultText.Text = sbResults.ToString();
            IDToken.Text = sbIdTokenClaims.ToString();
            TokenResponseText.Text = sbResponse.ToString();
            LogText.Text = sbLog.ToString();
        }
    }
}