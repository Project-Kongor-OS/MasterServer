namespace ProjectKongor.Server;

using ProjectKongor.Protocol.Services;
using Microsoft.EntityFrameworkCore;
using PUZZLEBOX;

public class AuthService : IAuthService
{
	private readonly BountyContext _context;

	public AuthService(BountyContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Checks if the given cookie exists in the database.
	/// </summary>
	/// <param name="cookie">The cookie string to validate.</param>
	/// <returns>True if the cookie exists, false otherwise.</returns>
	public async Task<bool> IsValidCookieAsync(string cookie)
	{
		if (string.IsNullOrEmpty(cookie))
			return false;

		return await _context.Accounts.AnyAsync(a => a.Cookie == cookie);
	}
}
