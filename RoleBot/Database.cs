using MongoDB.Bson;
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
                {"rolesList", new BsonArray {"Member"} },
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
                                {"roles", new BsonArray {"Member"} },
                                {"Bio","You can store any Information about yourself here" }
                            };
            await getGroupCollection(update).InsertOneAsync(newDoc);
        }
        public static async Task<BsonDocument> getGroupData(Update update)
        {
            return await getGroupCollection(update).Find(getGroupFilter(update)).FirstAsync();
        }
        public static async Task<BsonDocument> getUserData(Update update)
        {
            return await getGroupCollection(update).Find(getUserFilter(update)).FirstAsync();
        }
        public static FilterDefinition<BsonDocument> getGroupFilter(Update update)
        {
            return Builders<BsonDocument>.Filter.Eq("groupId", update.Message.Chat.Id);
        }
        public static FilterDefinition<BsonDocument> getUserFilter(Update update)
        {
            return Builders<BsonDocument>.Filter.Eq("useridRole", update.Message.From.Id);
        }
        public static UpdateDefinition<BsonDocument> createUpdateSet(string attribute,object value)
        {
            return Builders<BsonDocument>.Update.Set(attribute, value);
        }
    }

}
