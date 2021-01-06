using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace BotTest
{
    internal abstract class Game
    {
        public ChatId ChatId { get; protected set; }

        public List<Player> PlayerList = new List<Player>();

        public bool GameStarted { get; set; }

        public async Task AddPlayerAsync(User user)
        {

            if (PlayerList.Count < 4)
            {
                
                if(!(await Program.UserIsInGameAsync(user)) && !GameStarted)
                {
                    PlayerList.Add(new Player(user));
                    await Program.client.SendTextMessageAsync(ChatId, user.FirstName + ", вы приняты.");
                    System.Console.WriteLine(user.FirstName + "joined the game.");
                }
                // If this user is already in the game.
                else if (!GameStarted)
                {
                    await Program.client.SendTextMessageAsync(ChatId, user.FirstName + ", вы уже в игре.");
                }
                else
                {
                    await Program.client.SendTextMessageAsync(ChatId, user.FirstName + ", уже идёт игра.");
                }
                
            }
            // If 4 users.
            else
            {
                await Program.client.SendTextMessageAsync(ChatId, user.FirstName + ", здесь занято.");
            }
        }

        public abstract Task StartAsync();
    }
}

