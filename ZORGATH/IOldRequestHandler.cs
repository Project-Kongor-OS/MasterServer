namespace ZORGATH;

public interface IOldRequestHandler
{
	/// <summary>
	///    Handles a PHP request with the given `formData`
	///    and returns the appropriate result.
	/// </summary>
	public Task<IActionResult> HandleRequest(ControllerContext controllerContext, Dictionary<string, string> formData);
}