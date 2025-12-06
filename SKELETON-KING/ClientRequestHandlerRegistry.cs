using KONGOR.Shared.Handlers.Client;
using ProjectKongor.Protocol.Handlers;
using ProjectKongor.Protocol.Services;
using ProjectKongor.Protocol.Registries;

namespace SKELETON_KING;

public class ClientRequestHandlerRegistry : IClientRequestHandlerRegistry
{
	public IReadOnlyDictionary<string, IClientRequestHandler> Handlers { get; }

	public ClientRequestHandlerRegistry(IAccountService accountService)
	{
		Handlers = new Dictionary<string, IClientRequestHandler>()
		{
            // Alphabetized by protocol key
            {"nick2id", new Nick2IdHandler(accountService)},
		};
	}
}