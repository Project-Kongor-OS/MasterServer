namespace ZORGATH;

public class LogoutHandler : IOldClientRequestHandler
{
    public async Task<IActionResult> HandleRequest(ControllerContext controllerContext, Dictionary<string, string> formData)
    {
        if (formData.TryGetValue("cookie", out var cookie))
        {
            if (cookie != "" && cookie != null)
            {
                using BountyContext bountyContext = controllerContext.HttpContext.RequestServices.GetRequiredService<BountyContext>();
                await bountyContext.Accounts.Where(a => a.Cookie == cookie).ExecuteUpdateAsync(s => s.SetProperty(prop => prop.Cookie, value => null));
            }
        }

        return new OkObjectResult("");
    }
}
