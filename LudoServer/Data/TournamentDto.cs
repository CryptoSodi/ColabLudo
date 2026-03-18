namespace LudoServer.Data
{
    public class TournamentDto
    {
        public string Name { get; set; }
        public string? Winner1 { get; set; }
        public string? Winner2 { get; set; }
        public string? Winner3 { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal EntryFee { get; set; }
        public decimal Prize1 { get; set; }
        public decimal Prize2 { get; set; }
        public decimal Prize3 { get; set; }
    }
}
