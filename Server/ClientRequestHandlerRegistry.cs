using KONGOR.Shared.Handlers.Client;
using ProjectKongor.Protocol.Handlers;
using ProjectKongor.Protocol.Services;
using ProjectKongor.Protocol.Registries;
using ProjectKongor.Protocol.Handlers.Client;

namespace ProjectKongor.Server;

public class ClientRequestHandlerRegistry : IClientRequestHandlerRegistry
{
	public IReadOnlyDictionary<string, IClientRequestHandler> Handlers { get; }

	public ClientRequestHandlerRegistry(IAccountService accountService, IAuthService authService, IStatsService statsService)
	{
		Handlers = new Dictionary<string, IClientRequestHandler>()
		{
			// Alphabetized by protocol key
			{"match_history_overview", new MatchHistoryOverviewHandler(statsService, authService)},
            {"nick2id", new Nick2IdHandler(accountService)},
			{"show_simple_stats" , new ShowSimpleStatsHandler(statsService, authService)}
		};
	}
}