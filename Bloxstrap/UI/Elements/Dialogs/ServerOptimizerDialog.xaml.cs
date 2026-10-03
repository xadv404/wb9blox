using System.Windows;
using Bloxstrap.RobloxInterfaces;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class ServerOptimizerDialog
    {
        private readonly CancellationTokenSource _cancellation = new();
        private PublicServer? _server;
        private long _placeId;
        public bool Joined { get; private set; }

        public ServerOptimizerDialog()
        {
            InitializeComponent();
            Closed += (_, _) => _cancellation.Cancel();
            Loaded += (_, _) => PlaceInput.Focus();
            PlaceInput.TextChanged += (_, _) =>
            {
                ClearResult();
                StatusText.Text = "Prêt à rechercher un serveur.";
            };
        }

        private void ClearResult()
        {
            _server = null;
            JoinButton.IsEnabled = false;
            JoinButton.IsDefault = false;
            SearchButton.IsDefault = true;
            ResultPanel.Visibility = Visibility.Collapsed;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            ClearResult();
            if (!ServerOptimizer.TryParsePlace(PlaceInput.Text, out _placeId))
            {
                StatusText.Text = "Saisis un identifiant positif ou un lien https://www.roblox.com/games/…";
                return;
            }
            SearchButton.IsEnabled = false;
            PlaceInput.IsEnabled = false;
            StatusText.Text = "Recherche des serveurs…";
            SearchProgress.Visibility = Visibility.Visible;
            try
            {
                _server = await ServerOptimizer.FindBestAsync(App.HttpClient, _placeId, _cancellation.Token);
                StatusText.Text = _server is null
                    ? "Aucun serveur libre avec un ping disponible. Tu peux lancer le jeu normalement."
                    : "Un serveur disponible a été trouvé.";
                JoinButton.IsEnabled = _server is not null;
                if (_server is not null)
                {
                    PingText.Text = $"{_server.Ping:0} ms";
                    PlayersText.Text = $"{_server.Playing} / {_server.MaxPlayers}";
                    ResultPanel.Visibility = Visibility.Visible;
                    SearchButton.IsDefault = false;
                    JoinButton.IsDefault = true;
                }
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerOptimizer", ex);
                StatusText.Text = ex is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests }
                    ? "Roblox limite les requêtes. Réessaie dans quelques instants."
                    : "Impossible de récupérer les serveurs. Vérifie ta connexion ou lance le jeu normalement.";
            }
            finally
            {
                SearchButton.IsEnabled = true;
                PlaceInput.IsEnabled = true;
                SearchProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void Join_Click(object sender, RoutedEventArgs e)
        {
            if (_server is null)
                return;
            try
            {
                Utilities.ShellExecute(ServerOptimizer.GetJoinUri(_placeId, _server.Id));
                Joined = true;
                Close();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ServerOptimizer::Join", ex);
                StatusText.Text = "Impossible d’ouvrir Roblox. Vérifie son installation et réessaie.";
            }
        }
    }
}
