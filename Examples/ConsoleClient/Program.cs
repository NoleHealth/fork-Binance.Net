using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;

BinanceClient.SetDefaultOptions(new BinanceClientOptions()
{
    ApiCredentials = new ApiCredentials("EPgsYcA8Lpa9Mo3DQO8FIA32lQXoGBfdPjJGcG3SfOUbdtR6jjkYpx8dJCc4PVqG", "hDzJMdjUf0m0kOfdu2wMGMpwacWiAM95azEQfA6q13tjB2wSVMJPuAKJTUJSQFZI"), // <- Provide you API key/secret in these fields to retrieve data related to your account
    LogLevel = LogLevel.Debug
});
BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
{
    ApiCredentials = new ApiCredentials("EPgsYcA8Lpa9Mo3DQO8FIA32lQXoGBfdPjJGcG3SfOUbdtR6jjkYpx8dJCc4PVqG", "hDzJMdjUf0m0kOfdu2wMGMpwacWiAM95azEQfA6q13tjB2wSVMJPuAKJTUJSQFZI"),
    LogLevel = LogLevel.Debug
});

string? read = "";
while (read != "R" && read != "S") 
{
    Console.WriteLine("Run [R]est or [S]ocket example?");
    read = Console.ReadLine();
}

if (read == "R")
{
    using (var client = new BinanceClient())
    {
        await HandleRequest("Symbol list", () => client.SpotApi.ExchangeData.GetExchangeInfoAsync(), result => string.Join(", ", result.Symbols.Select(s => s.Name).Take(10)) + " etc");
        await HandleRequest("BTCUSDT book price", () => client.SpotApi.ExchangeData.GetBookPriceAsync("BTCUSDT"), result => $"Best Ask: {result.BestAskPrice}, Best Bid: {result.BestBidPrice}");
        await HandleRequest("ETHUSDT 24h change", () => client.SpotApi.ExchangeData.GetTickerAsync("ETHUSDT"), result => $"Change: {result.PriceChange}, Change percentage: {result.PriceChangePercent}");
    }
}
else
{
    
    Console.WriteLine("Press enter to subscribe to BTCUSDT trade stream");
    Console.ReadLine();
    var socketClient = new BinanceSocketClient();
    bool within10 = false;
    DateTime? last10 = null;
    var subscription = await socketClient.SpotStreams.SubscribeToTradeUpdatesAsync("BTCUSDT", data =>
    {
        if (data.Data.Quantity >= 5M)
        {
            Console.WriteLine($"== Nibble {data.Data.TradeTime}: {data.Data.Event} {data.Data.Symbol} {data.Data.Quantity} @ {data.Data.Price}");
        }
        if (data.Data.Quantity >= 10M)
        {
            last10 = DateTime.Now;
            Console.WriteLine($"=== SHARK IN THE WATER {data.Data.TradeTime}: {data.Data.Event} {data.Data.Symbol} {data.Data.Quantity} @ {data.Data.Price}");
        }
        within10 = (last10 != null && last10.Value.AddMinutes(5) > DateTime.Now);

        if (within10 && data.Data.Quantity >= .25M)
            Console.WriteLine($"{data.Data.TradeTime}: {data.Data.Event} {data.Data.Symbol} {data.Data.Quantity} @ {data.Data.Price}");
    });
    if (!subscription.Success)
    {
        Console.WriteLine("Failed to sub: " + subscription.Error);
        Console.ReadLine();
        return;
    }

    subscription.Data.ConnectionLost += () => Console.WriteLine("Connection lost, trying to reconnect..");
    subscription.Data.ConnectionRestored += (t) => Console.WriteLine("Connection restored");

    Console.ReadLine();
    /// Unsubscribe
    await socketClient.UnsubscribeAllAsync();
}

static async Task HandleRequest<T>(string action, Func<Task<WebCallResult<T>>> request, Func<T, string> outputData)
{
    Console.WriteLine("Press enter to continue");
    Console.ReadLine();
    Console.Clear();
    Console.WriteLine("Requesting " + action + " ..");
    var bookPrices = await request();
    if (bookPrices.Success)
        Console.WriteLine($"{action}: " + outputData(bookPrices.Data));
    else
        Console.WriteLine($"Failed to retrieve data: {bookPrices.Error}");
    Console.WriteLine();
}

//var socketClient = new BinanceSocketClient();
//// Spot | Spot market and user subscription methods
//socketClient.Spot.SubscribeToAllBookTickerUpdatesAsync(data =>
//{
//    // Handle data
//});

//// FuturesCoin | Coin-M futures market and user subscription methods
//socketClient.FuturesCoin.SubscribeToAllBookTickerUpdatesAsync(data =>
//{
//    // Handle data
//});

//// FuturesUsdt | USDT-M futures market and user subscription methods
//socketClient.FuturesUsdt.SubscribeToAllBookTickerUpdatesAsync(data =>
//{
//    // Handle data
//});
Console.ReadLine();