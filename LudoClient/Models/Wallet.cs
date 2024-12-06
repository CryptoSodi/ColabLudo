using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class Wallet
    {
        public int WalletId { get; set; }
        public int PlayerId { get; set; }
        public string AddressType { get; set; }
        public string WalletAddress { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
    }
}
