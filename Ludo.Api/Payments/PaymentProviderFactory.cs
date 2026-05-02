using SignalR.Server.Interfaces;

namespace SignalR.Server.Payments
{
    public class PaymentProviderFactory
    {
        private readonly Dictionary<CurrencyType, IPaymentProvider> _providers;
        public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers)
        {
            _providers = providers.ToDictionary(p => p.Currency, p => p);
        }
        public IPaymentProvider Get(CurrencyType currency)
        {
            if (!_providers.TryGetValue(currency, out var provider))
                throw new Exception($"No provider registered for {currency}");

            return provider;
        }
    }
}