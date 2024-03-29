﻿using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace roleBot.RoleBot
{
    internal class Database
    {
        public static MongoClientSettings stgs;
        public static MongoClient client;
        public static IMongoDatabase database;
        public static void connect()
        {
            var settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable("dbToken"));
            stgs = settings;
            var dbclient = new MongoClient(settings);
            client = dbclient;
            var db = dbclient.GetDatabase("roleBotDatabase");
            database = db;

            Console.WriteLine("Database connected!");
        }

        public static IMongoCollection<BsonDocument> getGroupCollection(Update update)
        {
            return database.GetCollection<BsonDocument>(update.Message.Chat.Id.ToString());
        }
        public static IMongoCollection<BsonDocument> getGroupCollection(CallbackQuery update)
        {
            return database.GetCollection<BsonDocument>(update.Message.Chat.Id.ToString());
        }

        public static async Task<bool> IsCollectionExistsAsync(string collectionName, IMongoClient database)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = await database.GetDatabase("roleBotDatabase").ListCollectionsAsync(new ListCollectionsOptions { Filter = filter });
            return await collections.AnyAsync();
        }
        public static async Task<bool> IsUserExistsAsync(IMongoCollection<BsonDocument> collection, long _userID)
        {

            BsonValue value = _userID;
            var documents = await collection.Find(new BsonDocument()).ToListAsync();
            foreach (var document in documents)
            {
                if (document.ContainsValue(value))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task AddCollection(ITelegramBotClient botClient, Update update)
        {
            await database.CreateCollectionAsync(update.Message.Chat.Id.ToString()); // if not add in database
            await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: "Thank you for Adding me to the group. Please read /start and /help command to get started with the bot.\nEnjoy...",
                    replyToMessageId: update.Message.MessageId,
                    parseMode: ParseMode.Html
                    );

            var thatCollec = database.GetCollection<BsonDocument>(update.Message.Chat.Id.ToString());
            Console.WriteLine(update.Message.Chat.Id);
            Console.WriteLine(update.Message.Chat.FirstName);

            var newDoc = new BsonDocument
            {
                {"groupId",update.Message.Chat.Id},
                {"rolesList", new BsonArray {} },
                {"memberList", new BsonArray {} }
            };
            await thatCollec.InsertOneAsync(newDoc);
        }
        public static async Task AddUser(ITelegramBotClient botClient, Update update)
        {
            var newDoc = new BsonDocument
                            {
                                {"useridRole",update.Message.From.Id},
                                {"userNameRole","@"+ update.Message.From.Username },
                                {"NameRole",update.Message.From.FirstName },
                                {"isUserRole",true},
                                {"roles", new BsonArray {} },
                                {"Bio","You can store any Information about yourself here" },
                                {"admin",false },
                                {"warnsNo",0 },
                                {"warn1",false },
                                {"warn1Reason","None" },
                                {"warn2",false },
                                {"warn2Reason","None" },
                                {"warn3",false },
                                {"warn3Reason","None" }
                            };
            await getGroupCollection(update).InsertOneAsync(newDoc);
        }
        public static async Task AddRole(Update update, string roleName, string roleRestriction)
        {
            var newDoc = new BsonDocument
                            {
                                {"RoleName",roleName},
                                {"roleRestriction", roleRestriction},
                                {"members", new BsonArray {} },
                                {"admin",false },
                                {"DeleteMess",false },
                                {"pinMessage",false },
                                {"addAdmins", false },
                                {"canWarn",false },
                                {"canMute",false },
                                {"canBan",false }
                            };
            await getGroupCollection(update).InsertOneAsync(newDoc);
        }
        public static async Task checkUsername(Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            try
            {
                BsonDocument userdata = getUserData(update).Result;
                if (userdata.GetValue("userNameRole") == "@")
                {
                    if (update.Message.From.Username != null)
                    {
                        var usernameUpdate = createUpdateSet("userNameRole", "@"+ update.Message.From.Username);
                        await groupCollection.UpdateOneAsync(getUserFilter(update.Message.From.Id), usernameUpdate);
                        Console.WriteLine(update.Message.From.FirstName + " changed their username from " + "nothing" + " to " + update.Message.From.Username);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                string username = userdata.GetValue("userNameRole").AsString;
                username = username.Remove(0, 1);
                if (update.Message.From.Username != username)
                {
                    var usernameUpdate = createUpdateSet("userNameRole", "@"+ update.Message.From.Username);
                    await groupCollection.UpdateOneAsync(getUserFilter(update.Message.From.Id), usernameUpdate);
                    Console.WriteLine(update.Message.From.FirstName + " changed their username from " + username + " to " + update.Message.From.Username);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }
            
        }
        public static async Task checkName(Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            try
            {
                BsonDocument userdata = getUserData(update).Result;
                string name = userdata.GetValue("NameRole").AsString;
                if (update.Message.From.FirstName != name)
                {
                    var nameUpdate = createUpdateSet("NameRole", update.Message.From.FirstName);
                    await groupCollection.UpdateOneAsync(getUserFilter(update.Message.From.Id), nameUpdate);
                    Console.WriteLine(update.Message.From.FirstName + " changed their name from " + name + " to " + update.Message.From.FirstName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + e.StackTrace);
            }

        }
        public static async Task<BsonDocument> getGroupData(Update update)
        {
            return await getGroupCollection(update).Find(getGroupFilter(update)).FirstAsync();
        }
        public static async Task<BsonDocument> getGroupData(CallbackQuery update)
        {
            return await getGroupCollection(update).Find(getGroupFilter(update)).FirstAsync();
        }
        public static async Task<BsonDocument> getUserData(Update update)
        {
            return await getGroupCollection(update).Find(getUserFilter(update.Message.From.Id)).FirstAsync();
        }
        public static async Task<BsonDocument> getUserData(CallbackQuery update)
        {
            return await getGroupCollection(update).Find(getUserFilter(update.From.Id)).FirstAsync();
        }
        public static async Task<BsonDocument> getRoleData(Update update,string roleName)
        {
            return await getGroupCollection(update).Find(getRoleFilter(roleName)).FirstAsync();
        }
        public static async Task<BsonDocument> getRoleData(CallbackQuery update, string roleName)
        {
            return await getGroupCollection(update).Find(getRoleFilter(roleName)).FirstAsync();
        }

        public static FilterDefinition<BsonDocument> getGroupFilter(Update update)
        {
            return Builders<BsonDocument>.Filter.Eq("groupId", update.Message.Chat.Id);
        }
        public static FilterDefinition<BsonDocument> getGroupFilter(CallbackQuery update)
        {
            return Builders<BsonDocument>.Filter.Eq("groupId", update.Message.Chat.Id);
        }

        public static FilterDefinition<BsonDocument> getRoleFilter(string roleName)
        {
            return Builders<BsonDocument>.Filter.Eq("RoleName", roleName);
        }

        public static FilterDefinition<BsonDocument> getUserFilter(long userid)
        {
            return Builders<BsonDocument>.Filter.Eq("useridRole", userid);
        }
        public static UpdateDefinition<BsonDocument> createUpdateSet(string attribute, object value)
        {
            return Builders<BsonDocument>.Update.Set(attribute, value);
        }
    }

}
