using Microsoft.EntityFrameworkCore;
using ProjectKongor.Protocol.Services;
using PUZZLEBOX;

namespace SKELETON_KING;

public class AccountService : IAccountService
{
	private readonly BountyContext _context;

	public AccountService(BountyContext context)
	{
		_context = context;
	}

	public async Task<Dictionary<string, int>> GetAccountIdsAsync(IEnumerable<string> usernames)
	{
		return await _context.Accounts
			.Where(a => usernames.Contains(a.Name))
			.ToDictionaryAsync(a => a.Name, a => a.AccountId);
	}
}