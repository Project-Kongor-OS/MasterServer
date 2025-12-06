namespace ProjectKongor.Server;

using System.Collections.Concurrent;
using EBULA;
using KINESIS;
using KINESIS.Server;
using Microsoft.EntityFrameworkCore;
using ProjectKongor.Protocol.Registries;
using ProjectKongor.Protocol.Services;
using PUZZLEBOX;
using ZORGATH;

public class Server
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();

        string connectionString = builder.Configuration.GetConnectionString("BOUNTY")!;
        builder.Services.AddDbContextFactory<BountyContext>(options =>
        {
            options.UseSqlServer(connectionString, connection => connection.MigrationsAssembly("SKELETON-KING")).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        // TODO: store in a configuration file.
        ChatServerConfiguration chatServerConfiguration = new ChatServerConfiguration("localhost", 11031, 11032, 11033);
        ConcurrentDictionary<string, SrpAuthSessionData> srpAuthSessions = new();

		builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
		builder.Services.AddScoped<IStatsService, StatsService>();

		builder.Services.AddScoped<IClientRequestHandlerRegistry>(sp =>
	        new ClientRequestHandlerRegistry(
		        sp.GetRequiredService<IAccountService>(),
			    sp.GetRequiredService<IAuthService>(),
				sp.GetRequiredService<IStatsService>()
			)
        );

		builder.Services.AddSingleton<IReadOnlyDictionary<string, IOldClientRequestHandler>>(
            new Dictionary<string, IOldClientRequestHandler>()
            {
                // NOTE: Please keep this list alphabetized by the string literal in the key.
                 {"autocompleteNicks", new AutoCompleteNicksHandler() },
                 {"get_match_stats", new GetMatchStatsHandler(replayServerUrl: "http://api.kongor.online") },
                 {"get_player_award_summ", new GetPlayerAwardSummHandler() },
                 {"get_seasons", new GetSeasonsHandler() },
                 {"logout", new LogoutHandler() },
                 {"pre_auth", new PreAuthHandler(srpAuthSessions) },
                 {"server_list", new ServerListHandler() },
                 {"show_stats", new ShowStatsHandler() },
                 {"srpAuth", new SrpAuthHandler(srpAuthSessions, new(), chatServerUrl: chatServerConfiguration.Address, icbUrl: "kongor.online") },
            }
        );

        VersionProvider versionProvider = new VersionProvider("http://gitea.kongor.online", "/administrator/KONGOR/raw/branch/main/patch/");
        builder.Services.AddSingleton<IReadOnlyDictionary<string, IOldServerRequestHandler>>(
            new Dictionary<string, IOldServerRequestHandler>()
            {
                // NOTE: Please keep this list alphabetized by the string literal in the key.
                {"accept_key", new AcceptKeyHandler() },
                {"c_conn", new ClientConnectionHandler() },
                {"new_session", new NewSessionHandler(versionProvider, chatServerConfiguration.Address, chatServerConfiguration.ServerPort) },
                {"replay_auth", new ReplayAuthHandler(chatServerConfiguration.Address, chatServerConfiguration.ManagerPort) },
                {"set_online", new SetOnlineHandler() },
                {"start_game", new StartGameHandler() },
            }
        );

        EconomyConfiguration economy = new EconomyConfiguration(
            signupRewards: new(goldCoins: 2500, silverCoins: 25000, plinkoTickets: 250),
            eventRewards: new(postSignupBonus: new(goldCoins: 100, silverCoins: 1000, plinkoTickets: 10, matchesCount: 25)),
            matchRewards: new(
                solo: new(win: new(goldCoins: 20, silverCoins: 200, plinkoTickets: 10), loss: new(goldCoins: 10, silverCoins: 100, plinkoTickets: 5)),
                twoPersonGroup: new(win: new(goldCoins: 24, silverCoins: 240, plinkoTickets: 14), loss: new(goldCoins: 12, silverCoins: 120, plinkoTickets: 7)),
                threePersonGroup: new(win: new(goldCoins: 30, silverCoins: 300, plinkoTickets: 16), loss: new(goldCoins: 15, silverCoins: 150, plinkoTickets: 8)),
                fourPersonGroup: new(win: new(goldCoins: 36, silverCoins: 360, plinkoTickets: 18), loss: new(goldCoins: 18, silverCoins: 180, plinkoTickets: 9)),
                fivePersonGroup: new(win: new(goldCoins: 40, silverCoins: 400, plinkoTickets: 20), loss: new(goldCoins: 20, silverCoins: 200, plinkoTickets: 10))));
        SubmitStatsHandler submitStatsHandler = new SubmitStatsHandler(economy);
        builder.Services.AddSingleton<IReadOnlyDictionary<string, IStatsRequestHandler>>(
           new Dictionary<string, IStatsRequestHandler>()
           {
                // NOTE: Please keep this list alphabetized by the string literal in the key.
                {"resubmit_stats", new ResubmitStatsHandler(submitStatsHandler) },
                {"submit_stats", submitStatsHandler },
           }
       );

        builder.Services.AddSingleton<ChatServerConfiguration>(chatServerConfiguration);
        builder.Services.AddSingleton<ChatServer>();
        builder.Services.AddSingleton<ServerStatusChecker>();
        builder.Services.AddControllers().AddApplicationPart(typeof(ClientRequesterController).Assembly);

        var app = builder.Build();
        app.MapControllers();
        app.Services.GetRequiredService<ChatServer>().Start();
        app.Services.GetRequiredService<ServerStatusChecker>().Start();

        app.Run();
    }
}
