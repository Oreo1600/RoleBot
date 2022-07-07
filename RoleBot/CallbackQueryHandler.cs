using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace roleBot.RoleBot
{
    public class CallbackQueryHandler
    {
        public static async Task populate(ITelegramBotClient botClient, CallbackQuery update, IMongoCollection<BsonDocument> groupCollection)
        {
            try
            {
                var groupDataFilter = Builders<BsonDocument>.Filter.Eq("groupId", update.Message.Chat.Id);
                BsonDocument groupData = await groupCollection.Find(groupDataFilter).FirstAsync();

                BsonArray roleList = groupData.GetValue("rolesList").AsBsonArray;

                if (!roleList.Contains(update.Data))
                {
                    await botClient.AnswerCallbackQueryAsync(update.Id, "There isn't any role with that name!");
                    return;
                }
                long tpUserid = update.From.Id;
                var roleUserFilter = Builders<BsonDocument>.Filter.Eq("useridRole", tpUserid);
                BsonDocument tpUserdata = await groupCollection.Find(roleUserFilter).FirstAsync();
                if (tpUserdata.GetValue("roles").AsBsonArray.Contains(update.Data))
                {
                    await botClient.AnswerCallbackQueryAsync(update.Id, "You already have this role!");
                    return;
                }

                ChatMember rollUser = await botClient.GetChatMemberAsync(update.Message.Chat.Id, tpUserid);

                var roleUpdate = Builders<BsonDocument>.Update.Push<long>("members", rollUser.User.Id);
                await groupCollection.UpdateOneAsync(Database.getRoleFilter(update.Data), roleUpdate);

                var rollAddUpdate = Builders<BsonDocument>.Update.Push<string>("roles", update.Data);
                await groupCollection.UpdateOneAsync(roleUserFilter, rollAddUpdate);

                await botClient.AnswerCallbackQueryAsync(update.Id, "You have successfully aquired this role!");
            }
            catch (Exception e)
            {
                await botClient.AnswerCallbackQueryAsync(update.Id, e.Message);
                return;
            }
            
        }

    }
}
