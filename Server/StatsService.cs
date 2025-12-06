using Microsoft.EntityFrameworkCore;
using ProjectKongor.Protocol.DTOs;
using ProjectKongor.Protocol.HTTP.Responses;
using ProjectKongor.Protocol.Services;
using PUZZLEBOX;

namespace ProjectKongor.Server;

public class StatsService : IStatsService
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

	public async Task<ShowSimpleStatsData?> GetShowSimpleStatsAsync(string nickname)
	{
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

	/// <summary>
	/// Returns the serialized match IDs for a player for the given table.
	/// </summary>
	public async Task<string?> GetSerializedMatchIdsAsync(string nickname, string table)
	{
		if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(table))
			return null;

		return table switch
		{
			"campaign" => await _context.Accounts
				.Where(a => a.Name == nickname)
				.Select(a => a.PlayerSeasonStatsRanked.SerializedMatchIds)
				.FirstOrDefaultAsync(),
			"campaign_casual" => await _context.Accounts
				.Where(a => a.Name == nickname)
				.Select(a => a.PlayerSeasonStatsRankedCasual.SerializedMatchIds)
				.FirstOrDefaultAsync(),
			"player" => await _context.Accounts
				.Where(a => a.Name == nickname)
				.Select(a => a.PlayerSeasonStatsPublic.SerializedMatchIds)
				.FirstOrDefaultAsync(),
			"midwars" => await _context.Accounts
				.Where(a => a.Name == nickname)
				.Select(a => a.PlayerSeasonStatsMidWars.SerializedMatchIds)
				.FirstOrDefaultAsync(),
			_ => null
		};
	}

	/// <summary>
	/// Given a serialized list of match ids, returns information about the last N matches.
	/// </summary>
	public async Task<Dictionary<string, string>> GetMatchHistoryOverviewAsync(
		string nickname,
		string serializedMatchIds,
		int numberOfMatches)
	{
		if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(serializedMatchIds) || numberOfMatches <= 0)
			return new Dictionary<string, string>();

		List<string> allMatchIds = serializedMatchIds.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
		IEnumerable<int> recentMatchIds = allMatchIds
			.TakeLast(numberOfMatches)
			.Reverse()
			.Select(id => int.Parse(id));

		// This would match [CLAN]nickname
		string nicknameSuffix = $"]{nickname}";

		var rawResults = await _context.PlayerMatchResults
			.Where(r => recentMatchIds.Contains(r.match_id) &&
						(r.nickname == nickname || r.nickname.EndsWith(nicknameSuffix)))
			.OrderByDescending(r => r.match_id)
			.Select(r => new
			{
				r.match_id,
				r.wins,
				r.team,
				r.herokills,
				r.deaths,
				r.heroassists,
				r.hero_id,
				r.secs,
				r.map,
				r.mdt,
				r.cli_name
			})
			.ToListAsync();

		return rawResults
			.Select((r, index) => new
			{
				// Calculate the key(m0, m1, m2, ... m99)
				Key = $"m{index}",
				// Construct the comma-separated string value
				Value = string.Join(',', r.match_id, r.wins, r.team, r.herokills, r.deaths, r.heroassists,
									  r.hero_id, r.secs, r.map, r.mdt, r.cli_name)
			})
			.ToDictionary(x => x.Key, x => x.Value);
	}
}