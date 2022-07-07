using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace roleBot.RoleBot
{
    public class Program
    {
        private static string botToken = Environment.GetEnvironmentVariable("botToken");
        public static readonly TelegramBotClient botClient = new TelegramBotClient(botToken);
        public static User me = botClient.GetMeAsync().Result;
        static async Task Main(string[] args)
        {
            try
            {

                Console.WriteLine(
                $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
                );
                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { }, // receive all update types
                    ThrowPendingUpdates = true
                };

                Database.connect();

                botClient.StartReceiving(
                    Handlers.HandleUpdateAsync,
                    Handlers.HandleErrorAsync,
                    receiverOptions,
                    cancellationToken
                    );

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            await Task.Delay(-1);
        }
    }
}
