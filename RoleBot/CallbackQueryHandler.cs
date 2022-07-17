using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

        public static async Task info(ITelegramBotClient botClient, CallbackQuery update, IMongoCollection<BsonDocument> groupCollection)
        {
            if (update.From.Id != update.Message.ReplyToMessage.From.Id) 
            {
                await botClient.AnswerCallbackQueryAsync(update.Id, $"Please refrain from interfering with other pepople's query!");
                return;
            }
            BsonDocument groupData = Database.getGroupData(update).Result;
            BsonArray roleList = groupData.GetValue("rolesList").AsBsonArray;
            if (roleList.Contains(update.Data))
            {
                try
                {
                    BsonDocument roleData = Database.getRoleData(update, update.Data).Result;
                    BsonArray memberList = roleData.GetValue("members").AsBsonArray;

                    List<List<InlineKeyboardButton>> keyboardButtons = new List<List<InlineKeyboardButton>>();
                    foreach (var x in memberList)
                    {
                        try
                        {
                            var tpFilter = Database.getUserFilter((long)x);
                            BsonDocument tpuserdata = await groupCollection.Find(tpFilter).FirstAsync();
                            string firstname = tpuserdata.GetValue("NameRole").AsString;
                            List<InlineKeyboardButton> inlinebutton = new()
                            {
                                new InlineKeyboardButton(x.ToString())
                                {
                                    Text = firstname,
                                    CallbackData = x.ToString()
                                },
                            };
                            keyboardButtons.Add(inlinebutton);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message + e.StackTrace);
                            if (e.Message == "Sequence contains no elements")
                            {
                                continue;
                            }
                        }
                    }
                    List<InlineKeyboardButton> lastinlinebutton = new()
                            {
                                new InlineKeyboardButton("🔙Back")
                                {
                                    Text = "🔙Back",
                                    CallbackData = "back"
                                },
                            };
                    keyboardButtons.Add(lastinlinebutton);
                    InlineKeyboardMarkup inlineKeyboard = new(keyboardButtons);
                    await botClient.EditMessageTextAsync(update.Message.Chat.Id,update.Message.MessageId, $"{update.Data} has currently {memberList.Count} members.\nClick below buttons to see profile of any member.", replyMarkup: inlineKeyboard);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + ex.StackTrace);
                }
            }
            else if (update.Data == "back")
            {

                var inlinekey = new InlineKeyboardButton[roleList.Count][];
                int index = 0;
                foreach (var x in roleList)
                {
                    InlineKeyboardButton[] inlinebutton = new[]
                    {
                    new InlineKeyboardButton(x.ToString())
                    {
                        Text = roleList[index].ToString(),
                        CallbackData = roleList[index].ToString()
                    },
                };
                    inlinekey[index] = inlinebutton;
                    index++;
                }

                InlineKeyboardMarkup inlineKeyboard = new(inlinekey);
                await botClient.EditMessageTextAsync(update.Message.Chat.Id, update.Message.MessageId, $"This group has currently {roleList.Count} roles.\nClick below buttons to see the members of role.", replyMarkup: inlineKeyboard);
            }
            else
            {
                var tpFilter = Database.getUserFilter(long.Parse(update.Data));
                BsonDocument tpuserdata = await groupCollection.Find(tpFilter).FirstAsync();
                BsonArray roles = tpuserdata.GetValue("roles").AsBsonArray;
                string bio = tpuserdata.GetValue("Bio").AsString;
                string userRolls = " ";
                for (int i = 0; i < roles.Count; i++)
                {
                    userRolls = userRolls + " - " + roles[i].ToString() + "\n";
                }
                try
                {
                    InlineKeyboardMarkup inlineKeyboard = new(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"🔙Back","back"),
                            }
                        }

                        );
                    await botClient.EditMessageTextAsync
                    (
                        update.Message.Chat.Id,
                        update.Message.MessageId,
                        $"👤 Name : {tpuserdata.GetValue("NameRole")}\n" +
                        $"🆔 User Id : <code>{update.Data}</code>\n\n" +
                        $"✏️Bio : {bio}\n" +
                        $"👥 Roles:\n{userRolls}",
                        parseMode: ParseMode.Html,replyMarkup:inlineKeyboard
                    );
                    return;
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Sequence contains no elements")
                    {
                        await botClient.EditMessageTextAsync
                        (
                            update.Message.Chat.Id,update.Message.MessageId,
                            "Failed to view this user's profile!,The reason maybe that he/she is no longer participant of the chat.\n"
                        );
                    }
                    else
                    {
                        await botClient.EditMessageTextAsync
                        (
                            update.Message.Chat.Id,update.Message.MessageId,
                            "Some error occured!\n" + ex.Message
                        );
                        return;
                    }
                }
            }
        }
        
    }
}
