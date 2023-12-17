
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Entities;
using MaxMind.GeoIP2;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Dapper;

namespace LanguageManagerPlugin;

class LanguageManagerPlugin : BasePlugin
{
    public override string ModuleAuthor => "aproxje";
    public override string ModuleName => "Language Manager Plugin";

    public override string ModuleVersion => "0.0.1";
    private SqliteConnection? _sqlConnection { get; set; }

    private PlayerLanguageManager playerLanguageManager = new PlayerLanguageManager();

    public override void Load(bool hotReload)
    {
        this.SetupPlayerDatabase();
        this.SetupCommand();

        RegisterListener<Listeners.OnClientConnected>(OnClientPutInServerHandler);
    }

    public static string? GetPlayerIp(CCSPlayerController player)
    {
        var playerIp = player.IpAddress;
        if (playerIp == null) { return null; }
        string[] parts = playerIp.Split(':');
        if (parts.Length == 2)
        {
            return parts[0];
        }
        else
        {
            return playerIp;
        }
    }

    public void OnClientPutInServerHandler(int slot)
    {

        var player = Utilities.GetPlayerFromSlot(slot);
        if (player == null || !player.IsValid || player.IsBot) return;

        var playerIP = GetPlayerIp(player);

        if (playerIP == null) return;

        ulong playerSteamID = player.SteamID;

        Task.Run(async () => {
            string? playerISOCode = await GetPlayerISOFromDatabase(playerSteamID);
            if ( playerISOCode != null )
            {
                SetPlayerLanguage(playerISOCode, playerSteamID);
                return;
            }

            var isoFromGeoLocation = GetPlayerISOCode(playerIP);

            if (isoFromGeoLocation == null) return;
            SetPlayerLanguage(isoFromGeoLocation, player.SteamID, true);
        }); 

    }

    public void SetPlayerLanguage(string playerISOCode, ulong steamID, bool updateDatabase = false)
    {
        CultureInfo tempCulture = new CultureInfo(playerISOCode);
        SteamID playerSteamId = new SteamID(steamID);
        this.playerLanguageManager.SetLanguage(playerSteamId, tempCulture);
        if (updateDatabase ) { this.SavePlayerISOInDatabase(steamID, tempCulture.Name); }
    }

    public async Task<string?> GetPlayerISOFromDatabase(ulong steamID)
    {
        string query = $"SELECT steamID, playerISOCode FROM playerLanguages WHERE steamID = {steamID}";
        var command = new SqliteCommand(query, _sqlConnection);
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                string playerISOCode = reader.GetFieldValue<string>(1); // assuming playerISOCode is in the second column
                if (playerISOCode != null) {
                    return playerISOCode;
                }
            }
        }
        return null;
    }

    public string? GetPlayerISOCode(string ipAddress)
    {
        using var reader = new DatabaseReader(Path.Combine(ModuleDirectory, "GeoLite2-Country.mmdb"));
        try
        {
            var response = reader.Country(ipAddress);
            return response.Country.IsoCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }

    }

    public void SavePlayerISOInDatabase(ulong steamID, string playerISO)
    {
        if (_sqlConnection == null) { return; }
        Task.Run(async () =>
        {
            try
            {
                await _sqlConnection.ExecuteAsync($@"
                INSERT INTO `playerLanguages` (`steamID`, `playerISOCode`) VALUES ({steamID}, '{playerISO}')
                ON CONFLICT(`steamID`) DO UPDATE SET `playerISOCode` = '{playerISO}'
            ");
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        });
    }

    public void SetupPlayerDatabase()
    {
        const string dbName = "playerLanguages.db";
        string pathToDb = Path.Join(ModuleDirectory, dbName);
        string connectionString = $"Data Source={pathToDb};";

        if (File.Exists(pathToDb))
        {
            // Database exists
            _sqlConnection = new SqliteConnection(connectionString);
            _sqlConnection.Open();
        } else{
            // Create a database
            using (var fileStream = File.Create(pathToDb))
            {
                fileStream.Close();
            }

            _sqlConnection = new SqliteConnection(connectionString);
            _sqlConnection.Open();

            Task.Run(async () =>
            {
                await _sqlConnection.ExecuteAsync(@"CREATE TABLE IF NOT EXISTS `playerLanguages` (
                    steamID INTEGER UNSIGNED NOT NULL PRIMARY KEY,
                    playerISOCode TEXT NOT NULL)");
            });
        }
    }

    public void SetupCommand()
    {

        // Adds another hook kindof to catch what language user is changing
        // Only works with iso rn
        this.AddCommand("css_lang", "Language manager hook",
            (player, info) => {
                if (player == null) { return; }
                if (info.ArgCount < 2) { return; }
                string isoCode = info.GetArg(1);
                // Idk check for ISO by length :^))
                if (isoCode.Length != 2) { return; }
                ulong playerSteamID = player.SteamID;
                Task.Run(() =>
                {
                    SetPlayerLanguage(isoCode, playerSteamID, true);
                });

        });
    }

}
