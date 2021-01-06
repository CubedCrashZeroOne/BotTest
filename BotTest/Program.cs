using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Args;

namespace BotTest
{

    internal static class Program
    {
        internal static ITelegramBotClient client;

        private static List<Game> _gameList = new List<Game>();

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, I compiled.");

            // Card Games Bot token.
            var token = "*************************";
            client = new TelegramBotClient(token);

            Console.WriteLine("I made client.");
            try
            {
                var me = await client.GetMeAsync();
                Console.WriteLine("I didn't error.");
            }
            catch (Exception)
            {
                Console.WriteLine("frick.");
            }
                
            client.OnMessage += OnMessage;
            client.OnInlineQuery += OnInlineQueryReceived;
            client.OnInlineResultChosen += OnInlineResultChosen;
            client.StartReceiving();
            System.Threading.Thread.Sleep(int.MaxValue);
        }

        // Chacks if user is already in a game.
        public static async Task<Boolean> UserIsInGameAsync(User user)
        {
            return (from g in _gameList
                    from p in g.PlayerList
                    where p.User.Id.Equals(user.Id)
                    select g).Count() > 0;
        }

        // Removes a game from the list.
        public static async Task EndGameAsync(Game game)
        {
            _gameList.Remove(game);
        }

        private static async void OnMessage(object sender, MessageEventArgs e)
        {
            // Id of the chat the message came from.
            ChatId chatId = e.Message.Chat.Id;

            // The game in the chat with the above id. 
            Game game;

            // If the message was sent in less than two minutes ago.
            if (e.Message.Text != null && !e.Message.From.IsBot && DateTime.Now.Subtract(e.Message.Date.ToLocalTime()).CompareTo(TimeSpan.FromMinutes(2)) < 0)
            {
                Console.WriteLine(DateTime.Now.Subtract(e.Message.Date.ToLocalTime()) + " ago:");
                Console.WriteLine($"{e.Message.Date.ToLocalTime()} Received a text message in chat {chatId}.\n {DateTime.Now}");

                // Removes the @cardgames_bot.
                string command = e.Message.Text.Split('@')[0];

                // If the commant is a join /start.
                if (command.Contains("/start"))
                {
                    // Splits the command and chat id.
                    string[] commandSplit = command.Split(' ');
                    if (commandSplit.Length == 2)
                    {
                        game = _gameList.Find(g => g.ChatId == commandSplit[1]);
                        if(game != null)
                        {
                            await game.AddPlayerAsync(e.Message.From);
                        }
                    }
                }

                // Find the game that matches the message's chat id.
                game = _gameList.Find(g => g.ChatId.Identifier.Equals(chatId.Identifier));
                if(game != null && game.GameStarted && !(game as GameSwedka).GetCurrentUser().Id.Equals(e.Message.From.Id))
                {
                    
                }
                
                // Choose the action depending on the message.
                switch (command)
                {
                    case "/newswedka":
                        // If there are no games in this chat.
                        if (game == null)
                        {
                            // Create a new Swedka game in this chat.
                            _gameList.Add(new GameSwedka(chatId));

                            // Send a message and a join link.
                            await client.SendTextMessageAsync(chatId, "Заходите в игру:",
                            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl
                            ("Вступить", string.Format($"https://t.me/cardgames_bot?start={chatId}"))));
                        }
                        // If there's already a game in this chat, don't start a new one.
                        else
                        {
                            // Send a message notifying that a game already exists.
                            await client.SendTextMessageAsync(chatId, "В этом чате уже идёт игра.");
                            break;
                        }

                        break;

                    case "/startgame":
                        if(game == null)
                        {
                            await client.SendTextMessageAsync(chatId, "В этом чате не запущена игра.");
                        }
                        else if(game.PlayerList.Count < 2)
                        {
                            await client.SendTextMessageAsync(chatId, "В игре нужно как минимум 2 игрока.");
                        }
                        else
                        {

                            if (!game.GameStarted) await client.SendTextMessageAsync(chatId, "Игра начинается!");
                            await game.StartAsync();
                        }
                        break;
                }
            }
        }

        private static async void OnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            Game game = (from g in _gameList
                         from p in g.PlayerList
                         where p.User.Id.Equals(inlineQueryEventArgs.InlineQuery.From.Id)
                         select g).FirstOrDefault();

            // If no game or player was found.
            if (game == null) return;

            Player player = (from g in _gameList
                            from p in g.PlayerList
                            where p.User.Id.Equals(inlineQueryEventArgs.InlineQuery.From.Id)
                            select p).FirstOrDefault();

            InlineQueryResultBase[] results = await player.QueryResultAsync();
            
            // If the last card is a Jack, the player can choose its suit.
            if ((game as GameSwedka).IsJackWithNoSuit() && player.User.Id.Equals((game as GameSwedka).GetCurrentUser().Id))
            results = new InlineQueryResultBase[]

            {
                new InlineQueryResultArticle(
                     id: "\U00002663",
                     title: "\U00002663",
                     inputMessageContent: new InputTextMessageContent("\U00002663")
                     ),
                new InlineQueryResultArticle(
                     id: "\U00002666",
                     title: "\U00002666",
                     inputMessageContent: new InputTextMessageContent("\U00002666")
                     ),
                new InlineQueryResultArticle(
                     id: "\U00002665",
                     title: "\U00002665",
                     inputMessageContent: new InputTextMessageContent("\U00002665")
                     ),
                new InlineQueryResultArticle(
                     id: "\U00002660",
                     title: "\U00002660",
                     inputMessageContent: new InputTextMessageContent("\U00002660")
                     )
            }.Concat(results).ToArray();

            if ((game as GameSwedka).CanEndPrematurely() && player.User.Id.Equals((game as GameSwedka).GetCurrentUser().Id))
                results = new InlineQueryResultBase[]
                {
                    new InlineQueryResultArticle(
                     id: "Finish",
                     title: "Finish",
                     inputMessageContent: new InputTextMessageContent("Finish")
                     )
                }.Concat(results).ToArray();

            // If the player is already in the game, but it hasn't started.
            if (results == null) return;

            await client.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
                );
        }

        private static async void OnInlineResultChosen(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");

            Game game = (from g in _gameList
                         from p in g.PlayerList
                         where p.User.Id.Equals(chosenInlineResultEventArgs.ChosenInlineResult.From.Id)
                         select g).FirstOrDefault();

            if (game == null) return;

            Player player = (from g in _gameList
                             from p in g.PlayerList
                             where p.User.Id.Equals(chosenInlineResultEventArgs.ChosenInlineResult.From.Id)
                             select p).FirstOrDefault();

            switch (chosenInlineResultEventArgs.ChosenInlineResult.ResultId)
            {
                case "\U00002663":
                    await (game as GameSwedka).AssignSuitJack(CardSuit.Club);
                    return;
                case "\U00002666":
                    await (game as GameSwedka).AssignSuitJack(CardSuit.Diamond);
                    return;
                case "\U00002665":
                    await (game as GameSwedka).AssignSuitJack(CardSuit.Heart);
                    return;
                case "\U00002660":
                    await (game as GameSwedka).AssignSuitJack(CardSuit.Spade);
                    return;
                default:
                    break;
            }


            if (player.User.Id.Equals((game as GameSwedka).GetCurrentUser().Id))
            {
                if (chosenInlineResultEventArgs.ChosenInlineResult.ResultId.Equals("End"))
                {
                    if (await (game as GameSwedka).EndTurnAsync())
                    {
                        await client.SendTextMessageAsync(game.ChatId,
                            $"{player.User.FirstName} закончил ход");
                        await (game as GameSwedka).GetCurrentStatusAsync();
                        return;
                    }
                    else
                    {
                        await client.SendTextMessageAsync(game.ChatId,
                            "Вы не можете сейчас закончить ход");
                        return;
                    }
                }

                if (chosenInlineResultEventArgs.ChosenInlineResult.ResultId.Equals("Pickup"))
                {
                    if (await (game as GameSwedka).DrawCardAsync())
                    {
                        await client.SendTextMessageAsync(game.ChatId,
                            $"{player.User.FirstName} берёт карту",
                            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Выбрать карту")));
                        return;
                    }
                    else
                    {
                        await client.SendTextMessageAsync(game.ChatId, "Карт в колоде не осталось");
                        return;
                    }
                }

                if (chosenInlineResultEventArgs.ChosenInlineResult.ResultId.Equals("Finish"))
                {
                    await (game as GameSwedka).PrematureEndDealAsync();
                    await client.SendTextMessageAsync(game.ChatId, $"{player.User.FirstName} закончил раздачу");
                    return;
                }

                // Suit and value of played card.
                CardSuit suit;
                CardValue value;
                Card? card;
                switch (char.ToUpper(chosenInlineResultEventArgs.ChosenInlineResult.ResultId[0]))
                {
                    case '\U00002663':
                        suit = CardSuit.Club;
                        break;
                    case '\U00002666':
                        suit = CardSuit.Diamond;
                        break;
                    case '\U00002665':
                        suit = CardSuit.Heart;
                        break;
                    case '\U00002660':
                        suit = CardSuit.Spade;
                        break;
                    default:
                        await client.SendTextMessageAsync(game.ChatId, "no, the suit not work");
                        return;
                }
                switch (chosenInlineResultEventArgs.ChosenInlineResult.ResultId[1])
                {
                    case '6':
                        value = CardValue.Six;
                        break;
                    case '7':
                        value = CardValue.Seven;
                        break;
                    case '8':
                        value = CardValue.Eight;
                        break;
                    case '9':
                        value = CardValue.Nine;
                        break;
                    case '1':
                        value = CardValue.Ten;
                        break;
                    case 'J':
                        value = CardValue.Jack;
                        break;
                    case 'Q':
                        value = CardValue.Queen;
                        break;
                    case 'K':
                        value = CardValue.King;
                        break;
                    case 'A':
                        value = CardValue.Ace;
                        break;
                    default:
                        await client.SendTextMessageAsync(game.ChatId, "no, the value not work");
                        return;
                }
                card = await player.PlayCardAsync(suit, value);

                // If the player doesn't have this card.
                if (card == null)
                {
                    await client.SendTextMessageAsync(game.ChatId, $"У вас нет такой карты");
                    return;
                }
                else
                {
                    // Try play card.
                    if(await (game as GameSwedka).PlayCardAsync(card.Value))
                    {
                        await client.SendTextMessageAsync(game.ChatId,
                            $"{player.User.FirstName} сыграл {card}");
                        await (game as GameSwedka).GetCurrentStatusAsync();
                    }
                    else
                    {
                        // If unsuccessful, return the card to the player.
                        await player.GetCardAsync(card.Value);
                        await client.SendTextMessageAsync(game.ChatId,
                            $"Вы не можете сыграть эту карту");
                    }
                }

            }
            else
            {
                await client.SendTextMessageAsync(game.ChatId, $"{player.User.FirstName}, Сейчас не ваш ход");
            }
        }
    }
}

