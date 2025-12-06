namespace ZORGATH;

[TestClass]
public class ClientRequesterControllerTest
{
	private class TestOldClientRequestHandler : IOldClientRequestHandler
	{
		private readonly IActionResult _result;
		public TestOldClientRequestHandler(IActionResult result)
		{
			_result = result;
		}

		public Task<IActionResult> HandleRequest(ControllerContext controllerContext, Dictionary<string, string> formData)
		{
			return Task.FromResult(_result);
		}
	}

	private class TestClientRequestHandler : IClientRequestHandler
	{
		private readonly IActionResult _result;
		public TestClientRequestHandler(IActionResult result)
		{
			_result = result;
		}

		public Task<IActionResult> HandleRequest(Dictionary<string, string> formData)
		{
			return Task.FromResult(_result);
		}
	}

	[TestMethod]
	public async Task OldHandler_IsInvoked()
	{
		IActionResult expected = new OkResult();

		// Register our fake old handler
		var oldHandlers = new Dictionary<string, IOldClientRequestHandler>()
		{
			["test"] = new TestOldClientRequestHandler(expected)
		};

		// Empty new registry
		var registry = new FakeClientRequestHandlerRegistry();

		var controller = new ClientRequesterController(oldHandlers, registry)
		{
			ControllerContext = new ControllerContextForTesting()
		};

		var formData = new Dictionary<string, string>()
		{
			["f"] = "test"
		};

		var actual = await controller.ClientRequester(formData);
		Assert.AreSame(expected, actual);
	}

	[TestMethod]
	public async Task NewHandler_IsInvoked()
	{
		IActionResult expected = new OkResult();

		// Empty old handlers
		var oldHandlers = new Dictionary<string, IOldClientRequestHandler>();

		// Register our fake new handler
		var registry = new FakeClientRequestHandlerRegistry(new Dictionary<string, IClientRequestHandler>()
		{
			["test"] = new TestClientRequestHandler(expected)
		});

		var controller = new ClientRequesterController(oldHandlers, registry)
		{
			ControllerContext = new ControllerContextForTesting()
		};

		var formData = new Dictionary<string, string>()
		{
			["f"] = "test"
		};

		var actual = await controller.ClientRequester(formData);
		Assert.AreSame(expected, actual);
	}

	[TestMethod]
	public async Task UnknownRequest_ReturnsBadRequest()
	{
		var controller = new ClientRequesterController(
			new Dictionary<string, IOldClientRequestHandler>(),
			new FakeClientRequestHandlerRegistry()
		);

		var formData = new Dictionary<string, string>()
		{
			["f"] = "unknown"
		};

		var actual = await controller.ClientRequester(formData);
		Assert.IsInstanceOfType(actual, typeof(BadRequestObjectResult));
	}

	[TestMethod]
	public async Task UnspecifiedRequest_ReturnsBadRequest()
	{
		var controller = new ClientRequesterController(
			new Dictionary<string, IOldClientRequestHandler>(),
			new FakeClientRequestHandlerRegistry()
		);

		var formData = new Dictionary<string, string>();

		var actual = await controller.ClientRequester(formData);
		Assert.IsInstanceOfType(actual, typeof(BadRequestObjectResult));
	}
}

// Fake registry implementation for tests
public class FakeClientRequestHandlerRegistry : IClientRequestHandlerRegistry
{
	public IReadOnlyDictionary<string, IClientRequestHandler> Handlers { get; }

	public FakeClientRequestHandlerRegistry()
	{
		Handlers = new Dictionary<string, IClientRequestHandler>();
	}

	public FakeClientRequestHandlerRegistry(Dictionary<string, IClientRequestHandler> handlers)
	{
		Handlers = handlers;
	}
}
