using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace roleBot.RoleBot
{
    internal class Commands
    {
        public static async Task<Message> start(ITelegramBotClient botClient,Update update)
        {
            return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Hello there, I am Roles Bot. I can give different roles to the users, And you can ping all the members of that role by just pinging the role.\n\nMade by @veebapun");
        }
        public static async Task<Message>help(ITelegramBotClient botClient,Update update)
        {
            return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Commands:\n\n/start - Check if the bot is alive or not\n/setBio - Set your group bio, You can see it in /profile command.\n/AddRole - Add a new Role (admin only)\nUsage: /AddRole Role_Name\n/GiveRole - Give a role to some user.(Reply to a user while sending this command, again admin only)\nUsage - /GiveRole ROLE_NAME\n/removeRole - remove a role from user(Reply to a user while sending this command, again admin only)\nUsage: /removeRole ROLE_NAME\n/deleterole - delete a role entirely from group (Need admin rights)\nUsage: /deleterole ROLE_NAME\n/profile - See your group profile. \n/populateRole - Make a button which people can click to get the specific role\nUsage: /populateRole ROLE_NAME ROLE_DESCRIPTION\n\n To ping all the members of a role use $roll_name");
        }
        public static async Task<Message> setBio(ITelegramBotClient botClient,Update update,IMongoCollection<BsonDocument> groupCollection)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            try
            {
                string[] messageSplit = update.Message.Text.Split(' ',2);
                if (messageSplit.Length == 1)
                {
                    return
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please specify a bio.\nUsage: /setbio bio text", replyToMessageId: update.Message.MessageId);
                }

                var BioUpdate = Database.createUpdateSet("Bio", messageSplit[1]);

                await groupCollection.UpdateOneAsync(Database.getUserFilter(update), BioUpdate);

                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Done!", replyToMessageId: update.Message.MessageId);
            }
            catch (Exception e)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, e.Message, replyToMessageId: update.Message.MessageId);
            }

        }

        public static async Task<Message> AddRole(ITelegramBotClient botClient, Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            try
            {
                string[] messageSplit = update.Message.Text.Split(' ');
                if (messageSplit.Length == 1)
                {
                    return
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please specify a Role Name.\nUsage: /AddRole RoleName", replyToMessageId: update.Message.MessageId);
                }

                /*BsonDocument grpupDataNew = Database.getGroupData(update).Result.Add(messageSplit[1], new BsonArray());
                await groupCollection.ReplaceOneAsync(Database.getGroupFilter(update), grpupDataNew);*/

                await Database.AddRole(update, messageSplit[1], "None");

                var roleUpdate = Builders<BsonDocument>.Update.Push<string>("rolesList", messageSplit[1]);
                await groupCollection.UpdateOneAsync(Database.getGroupFilter(update), roleUpdate);

                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Done!", replyToMessageId: update.Message.MessageId);
            }
            catch (Exception e)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, e.Message, replyToMessageId: update.Message.MessageId);
            }

        }
        public static async Task<Message> AddUser(ITelegramBotClient botClient, Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            //checking if chat is not a group
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            try
            {
                //checking the right parameters
                string[] messageSplit = update.Message.Text.Split(' ');
                if (messageSplit.Length == 1)
                {
                    return
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please specify a Role Name while replying to a user.\nUsage: /GiveRole rolename", replyToMessageId: update.Message.MessageId);
                }

                //checking if role exist
                BsonArray roleList = Database.getGroupData(update).Result.GetValue("rolesList").AsBsonArray;
                if (!roleList.Contains(messageSplit[1]))
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "There isn't any role with that name!", replyToMessageId: update.Message.MessageId);
                }

                //Adding user to the role
                if (update.Message.ReplyToMessage != null) //check if reply message is not null
                {
                    long tpUserid = update.Message.ReplyToMessage.From.Id;
                    Console.WriteLine(tpUserid);

                    var roleUserFilter = Builders<BsonDocument>.Filter.Eq("useridRole", tpUserid);      //get the third person userdata
                    BsonDocument tpUserdata = await groupCollection.Find(roleUserFilter).FirstAsync();

                    //checking if user already has the role
                    if (tpUserdata.GetValue("roles").AsBsonArray.Contains(messageSplit[1]))
                    {
                        return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This user already has this role!", replyToMessageId: update.Message.MessageId);
                    }

                    ChatMember rollUser = await botClient.GetChatMemberAsync(update.Message.Chat.Id, tpUserid);
                    
                    var roleUpdate = Builders<BsonDocument>.Update.Push<long>("members", rollUser.User.Id);
                    await groupCollection.UpdateOneAsync(Database.getRoleFilter(messageSplit[1]), roleUpdate);

                    var rollAddUpdate = Builders<BsonDocument>.Update.Push<string>("roles", messageSplit[1]);
                    await groupCollection.UpdateOneAsync(roleUserFilter, rollAddUpdate);

                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Done!", replyToMessageId: update.Message.MessageId);
                }
                else
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "User Not found!", replyToMessageId: update.Message.MessageId);
                }

                
            }
            catch (Exception e)
            {
                if (e.Message == "Bad Request: user not found")
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Invalid Username!", replyToMessageId: update.Message.MessageId);
                }
                if (e.Message == "Sequence contains no elements")
                {
                    Console.WriteLine(e.Message + e.StackTrace);
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "In order to give a role to user,\n1.User must be in the group.\n2.User must need to have send one message after adding me to the group!\nIf the issue still persist contact the support group!", replyToMessageId: update.Message.MessageId);
                }
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Unexpected Error!\n"+ e.Message + e.StackTrace, replyToMessageId: update.Message.MessageId);
            }
        }

        public static async Task<Message> profile(ITelegramBotClient botClient, Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            if (update.Message.Text.Split(" ").Length == 2)
            {
                return await processOtherProfile(botClient, update, groupCollection);
              
            }
            BsonDocument userData = Database.getUserData(update).Result;
            BsonArray roles = userData.GetValue("roles").AsBsonArray;
            string bio = userData.GetValue("Bio").AsString;
            string userRolls = " ";
            for (int i = 0; i < roles.Count; i++)
            {
                userRolls = userRolls + " - " + roles[i].ToString() + "\n";
            }
            try
            {
                //getting user profile picture
                UserProfilePhotos photo = await botClient.GetUserProfilePhotosAsync(update.Message.From.Id);
                string fileid = photo.Photos[0][0].FileId;
                Telegram.Bot.Types.File pfp = await botClient.GetFileAsync(fileid);

                return await botClient.SendPhotoAsync
                    (
                        update.Message.Chat.Id,
                        pfp.FileId,
                        $"👤 Name : {update.Message.From.FirstName + update.Message.From.LastName}\n" +
                        $"🆔 User Id : <code>{update.Message.From.Id}</code>\n\n" +
                        $"✏️Bio : <code>{bio}</code>\n" +
                        $"👥 Roles:\n{userRolls}",
                        
                        parseMode: ParseMode.Html
                    );
            }
            catch (Exception ex)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id,ex.Message);
                if (ex.Message == "Index was outside the bounds of the array.")
                {
                    return await botClient.SendTextMessageAsync
                    (
                        update.Message.Chat.Id,
                        $"👤 Name : {update.Message.From.FirstName + update.Message.From.LastName}\n" +
                        $"🆔 User Id : <code>{update.Message.From.Id}</code>\n\n" +
                        $"✏️Bio : {bio}\n" +
                        $"👥 Roles: {userRolls}",
                        parseMode: ParseMode.Html
                    );
                }
                else
                {
                    return await botClient.SendTextMessageAsync
                    (
                        update.Message.Chat.Id,
                        "Some error occured!\n" + ex.Message
                    );
                }
            }
        }
    
        public static async Task<Message> processOtherProfile(ITelegramBotClient botClient, Update update, IMongoCollection<BsonDocument> groupCollection)
        {

            if (update.Message.Text.Split(" ")[1] == "group")
            {
                try
                {
                    //getting group profile picture                  

                    BsonArray rolesList = Database.getGroupData(update).Result.GetValue("rolesList").AsBsonArray;
                    string groupRolls = "";
                    for (int i = 0; i < rolesList.Count; i++)
                    {
                        groupRolls = groupRolls + " - "+rolesList[i].ToString() +"\n";
                    }

                    return await botClient.SendTextMessageAsync
                        (
                            update.Message.Chat.Id,
                            $"Group Name : {update.Message.Chat.Title}\n" +
                            $"Chat Id : <code>{update.Message.Chat.Id}</code>\n\n" +
                            $"All Roles:\n{groupRolls}",

                            parseMode: ParseMode.Html
                        );
                }
                catch (Exception e)
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, e.Message);
                }              
            }
            else if (update.Message.Entities[1].Type == MessageEntityType.TextMention)
            {
                User rollUser = update.Message.Entities.First(x => x.User is not null).User;

                var tpUserFilter = Builders<BsonDocument>.Filter.Eq("useridRole", rollUser.Id);
                BsonDocument userData = await Database.getGroupCollection(update).Find(tpUserFilter).FirstAsync();
                BsonArray roles = userData.GetValue("roles").AsBsonArray;
                string bio = userData.GetValue("Bio").AsString;
                string userRolls = " ";
                for (int i = 0; i < roles.Count; i++)
                {
                    userRolls = userRolls + " - " + roles[i].ToString() + "\n";
                }
                try
                {
                    //getting user profile picture
                    UserProfilePhotos photo = await botClient.GetUserProfilePhotosAsync(rollUser.Id);
                    string fileid = photo.Photos[0][0].FileId;
                    Telegram.Bot.Types.File pfp = await botClient.GetFileAsync(fileid);

                    return await botClient.SendPhotoAsync
                        (
                            update.Message.Chat.Id,
                            pfp.FileId,
                            $"👤 Name : {rollUser.FirstName + rollUser.LastName}\n" +
                            $"🆔 User Id : <code>{rollUser.Id}</code>\n\n" +
                            $"✏️Bio : <code>{bio}</code>\n" +
                            $"👥 Roles:\n{userRolls}",

                            parseMode: ParseMode.Html
                        );
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Index was outside the bounds of the array.")
                    {
                        return await botClient.SendTextMessageAsync
                        (
                            update.Message.Chat.Id,
                            $"👤 Name : {rollUser.FirstName + rollUser.LastName}\n" +
                            $"🆔 User Id : <code>{rollUser.Id}</code>\n\n" +
                            $"✏️Bio : <code>{bio}</code>\n" +
                            $"👥 Roles: {userRolls}",

                            parseMode: ParseMode.Html
                        );
                    }
                    else if (ex.Message == "Sequence contains no elements")
                    {
                        return await botClient.SendTextMessageAsync
                        (
                            update.Message.Chat.Id,
                            "User not found in the database!"
                        );
                    }
                    else
                    {
                        return await botClient.SendTextMessageAsync
                        (
                            update.Message.Chat.Id,
                            "Some error occured!"
                        );
                    }
                }
            }
            else
            {
                try
                {
                    var tpUserFilter = Builders<BsonDocument>.Filter.Eq("userNameRole", update.Message.Text.Split(" ")[1]);
                    BsonDocument tpUserdata = await groupCollection.Find(tpUserFilter).FirstAsync();
                    long tpUserid = tpUserdata.GetValue("useridRole").AsInt64;
                    ChatMember rollUser = await botClient.GetChatMemberAsync(update.Message.Chat.Id, tpUserid);

                    BsonDocument userData = await Database.getGroupCollection(update).Find(tpUserFilter).FirstAsync();
                    BsonArray roles = userData.GetValue("roles").AsBsonArray;
                    string bio = userData.GetValue("Bio").AsString;
                    string userRolls = " ";
                    for (int i = 0; i < roles.Count; i++)
                    {
                        userRolls = userRolls + " - " + roles[i].ToString() + "\n";
                    }
                    try
                    {
                        //getting user profile picture
                        UserProfilePhotos photo = await botClient.GetUserProfilePhotosAsync(rollUser.User.Id);
                        string fileid = photo.Photos[0][0].FileId;
                        Telegram.Bot.Types.File pfp = await botClient.GetFileAsync(fileid);

                        return await botClient.SendPhotoAsync
                            (
                                update.Message.Chat.Id,
                                pfp.FileId,
                                $"👤 Name : {rollUser.User.FirstName + rollUser.User.LastName}\n" +
                                $"🆔 User Id : <code>{rollUser.User.Id}</code>\n\n" +
                                $"✏️Bio : <code>{bio}</code>\n" +
                                $"👥 Roles:\n{userRolls}",

                                parseMode: ParseMode.Html
                            );
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message == "Index was outside the bounds of the array.")
                        {
                            return await botClient.SendTextMessageAsync
                            (
                                update.Message.Chat.Id,
                                $"👤 Name : {rollUser.User.FirstName + rollUser.User.LastName}\n" +
                                $"🆔 User Id : <code>{rollUser.User.Id}</code>\n\n" +
                                $"✏️Bio : <code>{bio}</code>\n" +
                                $"👥 Roles: {userRolls}",

                                parseMode: ParseMode.Html
                            );
                        }
                        else if (ex.Message == "Sequence contains no elements")
                        {
                            return await botClient.SendTextMessageAsync
                            (
                                update.Message.Chat.Id,
                                "User not found in the database!"
                            );
                        }
                    }
                    return await botClient.SendTextMessageAsync
                           (
                               update.Message.Chat.Id,
                               "Some Error Occured"
                           );
                }
                catch (Exception e)
                {
                    if (e.Message == "Sequence contains no elements")
                    {
                        return await botClient.SendTextMessageAsync
                        (
                            update.Message.Chat.Id,
                            "User not found in the database!"
                        );
                    }
                    return await botClient.SendTextMessageAsync
                           (
                               update.Message.Chat.Id,
                               "Some Error Occured"
                           );
                }
                
            }
        }
        public static async Task ping(ITelegramBotClient botClient, Update update, string role)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            try
            {
                BsonDocument groupData = Database.getGroupData(update).Result;
                BsonArray roleList = groupData.GetValue("rolesList").AsBsonArray;

                if (roleList.Contains(role))
                {
                    BsonArray users = Database.getRoleData(update,role).Result.GetValue("members").AsBsonArray;
                    ChatMember[] userArray = new ChatMember[users.Count()];
                    int usersAffected = 0;
                    for (int i = 0; i < users.Count; i++)
                    {
                        try
                        {
                            userArray[i] = await botClient.GetChatMemberAsync(update.Message.Chat.Id, (long)users[i]);
                            usersAffected++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            if (e.Message == "Bad Request: user not found")
                            {
                                continue;
                            }
                        }
                    }
                    int count = usersAffected - 1;
                    while (count >= 0)
                    {
                        try
                        {
                            string message = "";
                            if (count >= 6)
                            {
                                for (int j = 0; j < 6; j++)
                                {
                                    message = message + $" [{userArray[count].User.FirstName}](tg://user?id={userArray[count].User.Id}) , ";
                                    count--;
                                }
                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, message, ParseMode.Markdown, replyToMessageId: update.Message.MessageId);
                            }
                            else
                            {
                                for (int i = count; i >= 0; i--)
                                {
                                    message = message + $" [{userArray[i].User.FirstName}](tg://user?id={userArray[i].User.Id}) , ";
                                    count--;
                                }
                                await botClient.SendTextMessageAsync(update.Message.Chat.Id, message, ParseMode.Markdown, replyToMessageId: update.Message.MessageId);
                                break;
                            }
                        }
                        catch (Exception er)
                        {
                            Console.WriteLine(er.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {               
                Console.WriteLine(e.Message + e.StackTrace);
            }
            
        }

        public static async Task<Message> populateRole(ITelegramBotClient botClient, Update update)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            try
            {
                string[] messageSplit = update.Message.Text.Split(' ',3);
                if (messageSplit.Length == 1 || messageSplit.Length == 2)
                {
                    return
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please specify a Role Name and Role description.\nUsage: /populateRole rolename description", replyToMessageId: update.Message.MessageId);
                }
                BsonArray roleList = Database.getGroupData(update).Result.GetValue("rolesList").AsBsonArray;


                if (!roleList.Contains(messageSplit[1]))
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "There isn't any role with that name!", replyToMessageId: update.Message.MessageId);
                }

                InlineKeyboardMarkup inlineKeyboard = new(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData($"{messageSplit[1]}"),
                            }
                        }

                        );
               return await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Role Name: {messageSplit[1]}\nDescription:{messageSplit[2]}\n\nCLICK BELOW TO GET THE ROLE!", replyMarkup: inlineKeyboard, replyToMessageId: update.Message.MessageId);
            }
            catch (Exception Ex)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, Ex.Message, replyToMessageId: update.Message.MessageId);
            }

        }

        public static async Task<Message> RemoveUser(ITelegramBotClient botClient, Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            try
            {
                string[] messageSplit = update.Message.Text.Split(' ');
                if (messageSplit.Length == 1)
                {
                    return
                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please specify a Role Name while replying to a user.\nUsage: /RemoveRole rolename", replyToMessageId: update.Message.MessageId);
                }

                BsonArray roleList = Database.getGroupData(update).Result.GetValue("rolesList").AsBsonArray;


                if (!roleList.Contains(messageSplit[1]))
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "There isn't any role with that name!", replyToMessageId: update.Message.MessageId);
                }

                if (update.Message.ReplyToMessage != null)
                {
                    long tpUserid = update.Message.ReplyToMessage.From.Id;
                    Console.WriteLine(tpUserid);

                    var roleUserFilter = Builders<BsonDocument>.Filter.Eq("useridRole", tpUserid);
                    BsonDocument tpUserdata = await groupCollection.Find(roleUserFilter).FirstAsync();
                    if (!tpUserdata.GetValue("roles").AsBsonArray.Contains(messageSplit[1]))
                    {
                        return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This user doesn't have that role to begin with!", replyToMessageId: update.Message.MessageId);
                    }
                    BsonArray userRoles = tpUserdata.GetValue("roles").AsBsonArray;
                    BsonArray RolesUsers = Database.getRoleData(update,messageSplit[1]).Result.GetValue("members").AsBsonArray;
                    userRoles.Remove(messageSplit[1]);
                    RolesUsers.Remove(update.Message.ReplyToMessage.From.Id);

                    var roleUpdate = Builders<BsonDocument>.Update.Set("members", RolesUsers);
                    await groupCollection.UpdateOneAsync(Database.getRoleFilter(messageSplit[1]), roleUpdate);

                    var rollAddUpdate = Builders<BsonDocument>.Update.Set("roles", userRoles);
                    await groupCollection.UpdateOneAsync(roleUserFilter, rollAddUpdate);

                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Done!", replyToMessageId: update.Message.MessageId);
                }
                else
                {
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "User Not found!", replyToMessageId: update.Message.MessageId);
                }
            }
            catch (Exception e)
            {
                if (e.Message == "Sequence contains no elements")
                {
                    Console.WriteLine(e.Message + e.StackTrace);
                    return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "In order to give a role to user, User must need to have send one message after adding me to the group!", replyToMessageId: update.Message.MessageId);
                }
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Unexpected Error!\n" + e.Message + e.StackTrace, replyToMessageId: update.Message.MessageId);
            }

        }

        public static async Task<Message> DeleteRole(ITelegramBotClient botClient, Update update, IMongoCollection<BsonDocument> groupCollection)
        {
            if (update.Message.Chat.Type != ChatType.Group && update.Message.Chat.Type != ChatType.Supergroup)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "This command is only available inside a group.", replyToMessageId: update.Message.MessageId);
            }
            string[] messageSplit = update.Message.Text.Split(" ");
            if (messageSplit.Length == 1)
            {
                return
                await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Please specify a Role Name to delete.\nUsage: /deleterole rolename", replyToMessageId: update.Message.MessageId);
            }
            BsonArray roleList = Database.getGroupData(update).Result.GetValue("rolesList").AsBsonArray;
            if (!roleList.Contains(messageSplit[1]))
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "There isn't any role with that name!", replyToMessageId: update.Message.MessageId);
            }

            BsonDocument roleData = Database.getRoleData(update, messageSplit[1]).Result;

            if (roleData.GetValue("members").AsBsonArray.Count > 0)
            {
                return await botClient.SendTextMessageAsync(update.Message.Chat.Id, $"I cannot delete this role.\nThere are currently {roleData.GetValue("members").AsBsonArray.Count} user(s) has this role! Please remove the role from them first.", replyToMessageId: update.Message.MessageId);
            }

            await groupCollection.DeleteOneAsync(Database.getRoleFilter(messageSplit[1]));

            BsonArray rolesList = Database.getGroupData(update).Result.GetValue("rolesList").AsBsonArray;
            bool rem = rolesList.Remove(messageSplit[1]);
            Console.WriteLine(rem);
            var roleUpdate = Builders<BsonDocument>.Update.Set("rolesList", rolesList);
            await groupCollection.UpdateOneAsync(Database.getGroupFilter(update), roleUpdate);

            return await botClient.SendTextMessageAsync(update.Message.Chat.Id, "Done!", replyToMessageId: update.Message.MessageId);
        }
    }
}
