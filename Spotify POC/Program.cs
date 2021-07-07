using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spotify_POC
{
    public class Program
    {
        // Configuração do aplicativo
        private static readonly string clientId = Environment.GetEnvironmentVariable("SpotifyClientId");//"3591641179c14f67b817339112ee0ac0";
        private static readonly string clientSecret = Environment.GetEnvironmentVariable("SpotifyClientSecret");//"8f89fafb59db432eaae500e415acec13";
        private static EmbedIOAuthServer _server;

        public static async Task Main()
        {
            // Subindo um servidor de autenciação
            _server = new EmbedIOAuthServer(
                new Uri("http://localhost:5000/callback"),
                5000
                //,
                //System.Reflection.Assembly.GetExecutingAssembly(),
                //"Spotify_POC.site"
              );
            await _server.Start();
            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;

            // Montando uma requests para o servidor de autenticação
            var request = new LoginRequest(_server.BaseUri, clientId, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { 
                    Scopes.UserReadEmail, 
                    Scopes.AppRemoteControl,
                    Scopes.UserReadPlaybackState,
                    Scopes.UserModifyPlaybackState,
                    Scopes.UserReadCurrentlyPlaying, 
                    Scopes.UserReadRecentlyPlayed, 
                    Scopes.UserReadPlaybackPosition
                }
            };
            Uri uri = request.ToUri();

            // Abrindo a url no navegador 
            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine("O navegador não pode ser aberto, abra manualmente a url: {0}", uri);
            }

            Console.ReadKey();
        }

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            // Solicita um token
            AuthorizationCodeTokenResponse token = await new OAuthClient().RequestToken(
              new AuthorizationCodeTokenRequest(clientId, clientSecret, response.Code, _server.BaseUri)
            );

            // Monta o cliente usando o token recebido
            var config = SpotifyClientConfig.CreateDefault().WithToken(token.AccessToken, token.TokenType);
            var spotify = new SpotifyClient(config);

            // Seja feliz :)
            var me = await spotify.UserProfile.Current();

            var playback = await spotify.Player.GetCurrentPlayback();
            if (playback != null && playback.IsPlaying && playback.Item.Type == ItemType.Track)
            {
                var track = playback.Item as FullTrack;
                Console.WriteLine($"{me.DisplayName} está ouvindo {track.Name} do {track.Artists[0].Name} no {playback.Device.Type} {playback.Device.Name}");
            }

            await spotify.Player.PausePlayback();
            
            await Task.Delay(5000);
            
            await spotify.Player.ResumePlayback();
            
            await Task.Delay(5000);

            Console.WriteLine($"Mas agora ele vai ouvir Ramones!");
            await spotify.Player.AddToQueue(new PlayerAddToQueueRequest("https://open.spotify.com/track/07b5vArZtW08PuEqCw61Ei"));
            await spotify.Player.SkipNext();

            Environment.Exit(0);
        }
    }
}
