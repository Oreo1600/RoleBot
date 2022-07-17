using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MongoDB.Driver;
using MongoDB.Bson;

namespace roleBot.RoleBot
{
    internal class Handlers
    {
        public static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            Console.WriteLine(exception.StackTrace);
            return Task.CompletedTask;
        }
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, cancellationToken);
            }
        }
        private static async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery clbq)
        {
            Console.WriteLine(clbq.Data);
            var groupCollection = Database.database.GetCollection<BsonDocument>(clbq.Message.Chat.Id.ToString());
            if (clbq.Message.ReplyToMessage != null && clbq.Message.ReplyToMessage.Text.ToLower().StartsWith("/populaterole"))
            {
                await CallbackQueryHandler.populate(botClient, clbq, groupCollection);
            }
            else if (clbq.Message.ReplyToMessage != null && clbq.Message.ReplyToMessage.Text.ToLower().StartsWith("/info"))
            {
                await CallbackQueryHandler.info(botClient, clbq, groupCollection);
            }
        }
        private static async Task BotOnMessageReceived(ITelegramBotClient botClient, Update update)
        {
            if (update.Message == null|| update.Message.Type != MessageType.Text)
                return;
            string messageText = update.Message.Text;
            Console.WriteLine($"Received a Message from {update.Message.From.FirstName}. Says: {messageText}");

            bool collExist = await Database.IsCollectionExistsAsync(update.Message.Chat.Id.ToString(), Database.client);
            if (!collExist)
            {
                await Database.AddCollection(botClient, update);
            }

            bool userExist = await Database.IsUserExistsAsync(Database.getGroupCollection(update), update.Message.From.Id);
            if (!userExist)
            {
                await Database.AddUser(botClient, update);
            }

            var groupCollection = Database.getGroupCollection(update);

            await Database.checkUsername(update, groupCollection);
            await Database.checkName(update, groupCollection);
            
            string role;
            if (update.Message.Text.Contains('$'))
            {
                string removedMess = update.Message.Text.Remove(0,update.Message.Text.IndexOf('$') + 1);
                role = removedMess.Split(" ")[0];
                await Commands.ping(botClient, update, role);
            }


            if (update.Message.Text == "/start" || update.Message.Text == $"/start@{Program.me.Username}")
            {
                await Commands.start(botClient, update);
            }
            else if (update.Message.Text == "/help" || update.Message.Text == $"/help@{Program.me.Username}")
            {
                await Commands.help(botClient, update);
            }
            else if (update.Message.Text.ToLower().StartsWith("/setbio"))
            {
                await Commands.setBio(botClient, update, groupCollection);
            }
            else if (update.Message.Text.ToLower().StartsWith("/giverole"))
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id);
                if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                {
                    await Commands.AddUser(botClient, update, groupCollection);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "You need admin rights to initiate this command!",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );
                }
            }
            else if (update.Message.Text.ToLower().StartsWith("/addrole"))
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id);
                if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                {
                    await Commands.AddRole(botClient, update, groupCollection);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "You need admin rights to initiate this command!",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );
                }
            }
            else if (update.Message.Text.ToLower().StartsWith("/populaterole"))
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id);
                if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                {
                    await Commands.populateRole(botClient, update);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "You need admin rights to initiate this command!",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );
                }
            }
            else if (update.Message.Text.ToLower().StartsWith("/removerole"))
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id);
                if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                {
                    await Commands.RemoveUser(botClient, update,groupCollection);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "You need admin rights to initiate this command!",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );
                }
            }
            else if (update.Message.Text.ToLower().StartsWith("/profile"))
            {
                await Commands.profile(botClient, update, groupCollection);
            }
            else if (update.Message.Text.ToLower().StartsWith("/deleterole"))
            {
                ChatMember chatMember = await botClient.GetChatMemberAsync(update.Message.Chat.Id, update.Message.From.Id);
                if (chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator || update.Message.From.Id == 1197998359) //checking if user has admin rights
                {
                    await Commands.DeleteRole(botClient, update,groupCollection);
                }
                else
                {
                    await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "You need admin rights to initiate this command!",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );
                }
            }
            else if (update.Message.Text == "/info" || update.Message.Text == $"/info@{Program.me.Username}")
            {
                await Commands.infoAsync(botClient, update);
            }

        }


        private static Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }
    }
}
