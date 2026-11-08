// Added by the VoreWar mercenary-company conversion fork.
// 2026-11-08: Include company identity in lightweight save previews (spec 40E).
// 2026-11-06: Lightweight saved-company previews without hydrating the gameplay graph.
// See CHANGES.md and License.txt for GPL-3.0 terms.
using System;
using System.Linq;

[Serializable]
internal sealed class CompanySaveSummary
{
    public string Id, Slot, Name, Leader, Company;
    public int Day, Gold, Members, Completed, BattleTurn, Kind;
    public bool Ironman, Ended, Preview, InBattle;
    internal static CompanySaveSummary From(CompanyCampaign state)
    {
        return new CompanySaveSummary { Id = state.Id, Slot = state.SaveSlot ?? "legacy", Name = state.SaveLabel ?? "Autosave",
            Company = CompanyHistoryStore.CompanyName(state.CompanyName),
            Leader = state.Characters.FirstOrDefault(c => c.Id == state.LeaderId)?.Unit.Name,
            Day = state.ElapsedDays + 1, Gold = state.Gold, Members = state.Members.Count, Completed = state.ContractsCompleted,
            BattleTurn = state.Battle?.Tactical?.currentTurn ?? 0, Kind = state.SaveKind, Ironman = state.Ironman,
            Ended = state.Ended, Preview = state.Preview, InBattle = state.InBattle };
    }
    internal string Details => "Company: " + CompanyHistoryStore.CompanyName(Company) + "\nLeader: " + Leader + "\nDay: " + Day + "\nGold: " + Gold + "\nMercenaries: " + Members +
        "\nContracts completed: " + Completed + (InBattle ? "\nBattle turn: " + BattleTurn : "\nWorld map") + (Ironman ? "\nIronman" : "");
}
