using Microsoft.EntityFrameworkCore;
using ProjectKongor.Protocol.DTOs;
using ProjectKongor.Protocol.HTTP.Responses;
using ProjectKongor.Protocol.Services;
using PUZZLEBOX;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SKELETON_KING;

public class StatsService : IPlayerStatsService
{
	private const int NumberOfHeroesTheGameHas = 139;

	// In Heroes of Newerth, there is a slightly different logic when computing stats for Seasons <= 6 and Seasons > 6.
	// Additionally, season id must be < 1000. 22 was chosen somewhat arbitrarily and primarily because revival started
	// in the year 2022, but anything > 6 should work just as well.
	private const int CurrentSeason = 22;

	private readonly BountyContext _context;

	public StatsService(BountyContext context)
	{
		_context = context;
	}

	public async Task<ShowSimpleStatsData?> GetShowSimpleStatsAsync(string nickname, string cookie)
	{
		// Validate cookie
		bool validCookie = await _context.Accounts.AnyAsync(a => a.Cookie == cookie);
		if (!validCookie)
			return null;

		// Query and project
		ShowSimpleStatsData? data = await _context.Accounts
			.Where(a => a.Name == nickname)
			.Select(a => new ShowSimpleStatsData(
				TotalLevel: 0, // Replace with actual calculation if needed
				TotalExperience: 0, // Replace with actual calculation if needed
				NumberOfHeroesOwned: NumberOfHeroesTheGameHas,
				TotalMatchesPlayed:
					a.PlayerSeasonStatsPublic.Wins + a.PlayerSeasonStatsPublic.Losses +
					a.PlayerSeasonStatsRanked.Wins + a.PlayerSeasonStatsRanked.Losses +
					a.PlayerSeasonStatsRankedCasual.Wins + a.PlayerSeasonStatsRankedCasual.Losses +
					a.PlayerSeasonStatsMidWars.Wins + a.PlayerSeasonStatsMidWars.Losses,
				CombinedPlayerAwardSummary: CombinedPlayerAwardSummary.AddUp(
					a.PlayerSeasonStatsPublic.PlayerAwardSummary.ToDTO(),
					a.PlayerSeasonStatsRanked.PlayerAwardSummary.ToDTO(),
					a.PlayerSeasonStatsRankedCasual.PlayerAwardSummary.ToDTO(),
					a.PlayerSeasonStatsMidWars.PlayerAwardSummary.ToDTO()
				),
				SelectedUpgradeCodes: a.SelectedUpgradeCodes,
				UnlockedUpgradeCodes: a.User.UnlockedUpgradeCodes,
				AccountId: a.AccountId,
				SeasonId: CurrentSeason,
				SeasonNormal: new SeasonShortSummary(
					wins: a.PlayerSeasonStatsRanked.Wins + a.PlayerSeasonStatsMidWars.Wins,
					losses: a.PlayerSeasonStatsRanked.Losses + a.PlayerSeasonStatsMidWars.Losses,
					winStreak: 0,
					currentLevel: 0
				),
				SeasonCasual: new SeasonShortSummary(
					wins: a.PlayerSeasonStatsRankedCasual.Wins,
					losses: a.PlayerSeasonStatsRankedCasual.Losses,
					winStreak: 0,
					currentLevel: 0
				)
			))
			.FirstOrDefaultAsync();

		return data;
	}
}