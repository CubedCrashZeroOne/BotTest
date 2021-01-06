using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace BotTest
{
    internal class GameSwedka : Game
    {
        // Temporary deck for shuffle.
        private readonly List<Card> _cards;

        // Cards currently in the deck.
        private readonly Stack<Card> _deck;

        // Cards on the table.
        private readonly Stack<Card> _table;

        // Random object used for shuffle. 
        private readonly Random _random;

        // Index of the player currently dealing.
        private int _currentDealer;

        // Index of the player whose turn it is.
        private int _currentPlayer;

        // Number of cards played this turn.
        private int _turnCardCounter = 0;

        // Cards the next player will draw.
        private int _cardsToDraw = 0;

        // Turns that will be skipped after this turn.
        private int _turnsToSkip = 0;

        // Used when a player ends the game with 4 similar value cards.
        private int _sameValueCards;

        // Used to check the number of jacks played this turn.
        private int _jackCardsPlayed;

        // If a jack is played, the suit selected
        private CardSuit? _currentJackSuit;


        public override async Task StartAsync()
        {
            if (GameStarted) return;
            Console.WriteLine("Game started.");

            // To make sure players can't join after the start.
            GameStarted = true;

            // The first player deals first.
            _currentDealer = 0;

            // Call the first deal.
            await DealAsync();
        }

        private async Task DealAsync()
        {
            _currentPlayer = _currentDealer;

            // In case some cards get lost.
            if (_cards.Count != 36) throw new Exception("Trying to deal without the full 36-card deck.");

            // Randomly moving the cards from the list to the deck stack.
            while (_cards.Count > 0)
            {
                int randomIndex = _random.Next(_cards.Count);
                _deck.Push(_cards[randomIndex]);
                _cards.RemoveAt(randomIndex);
            }

            // Give each player 5 cards.
            foreach (var player in PlayerList)
            {
                for (int i = 0; i < 5; ++i)
                {
                    await player.GetCardAsync(_deck.Pop());
                }
            }

            _table.Push(_deck.Pop());
            switch (_table.Peek().Value)
            {
                case CardValue.Six:
                case CardValue.Ten:
                case CardValue.Nine:
                    break;
                case CardValue.Jack:
                    _currentJackSuit = null;
                    _jackCardsPlayed++;
                    break;
                case CardValue.Seven:
                    // Draw 2 cards and skip turn.
                    _cardsToDraw += 2;
                    _turnsToSkip = 1;
                    break;
                case CardValue.Eight:
                    // Draw 1 card.
                    _cardsToDraw++;
                    break;
                case CardValue.Ace:
                    // Skip turn (turn skips can be stacked).
                    _turnsToSkip++;
                    break;
                case CardValue.Queen:
                    // Draw 4 cards and skip turn.
                    if (_table.Peek().Suit.Equals(CardSuit.Spade))
                    {
                        _turnsToSkip = 1;
                        _cardsToDraw = 4;
                    }
                    break;
                case CardValue.King:
                    // Draw 5 cards
                    if (_table.Peek().Suit.Equals(CardSuit.Heart))
                    {
                        _cardsToDraw = 5;
                    }
                    break;
            }
            _turnCardCounter = 1;
            await GetCurrentStatusAsync();
        }

        // Sends the current player and the top card.
        public async Task GetCurrentStatusAsync()
        {
            string message = $"Ходит: {GetCurrentUser().FirstName}(@{GetCurrentUser().Username}){Environment.NewLine}Карта: {_table.Peek()}";
            // If it's a Jack, add a suit symbol.
            if (_table.Peek().Value.Equals(CardValue.Jack))
            {
                switch (_currentJackSuit)
                {
                    case CardSuit.Club:
                        message += "(\U00002663)";
                        break;
                    case CardSuit.Diamond:
                        message += "(\U00002666)";
                        break;
                    case CardSuit.Heart:
                        message += "(\U00002665)";
                        break;
                    case CardSuit.Spade:
                        message += "(\U00002660)";
                        break;
                }
            }
            foreach (var p in PlayerList)
            {
                message += $"{Environment.NewLine}{p.User.FirstName} - {p.CardCount()} {(p.CardCount() == 1 ? "карта" : p.CardCount() > 1 && p.CardCount() < 5 ? "карты" : "карт")}";
            }

            await Program.client.SendTextMessageAsync(ChatId, message,
                replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithSwitchInlineQueryCurrentChat("Выбрать карту")));
        }

        private async Task ReshuffleAsync()
        {
            await Program.client.SendTextMessageAsync(ChatId, "Колода перетасована.");
            // Temporarily moving the last turn to the deck.
            // If current turn is 0 cards, take 1 card.
            for (int i = _turnCardCounter == 0 ? 1 : _turnCardCounter; i > 0; --i)
            {
                _deck.Push(_table.Pop());
            }

            // Moving the rest of the table to the cards list.
            while (_table.Count > 0)
            {
                _cards.Add(_table.Pop());
            }

            // Returning the last turn to the table.
            while (_deck.Count > 0)
            {
                _table.Push(_deck.Pop());
            }

            // Shuffling the cards back to the deck.
            while (_cards.Count > 0)
            {
                int randomIndex = _random.Next(_cards.Count);
                _deck.Push(_cards[randomIndex]);
                _cards.RemoveAt(randomIndex);
            }
        }

        public async Task<Boolean> EndTurnAsync()
        {
            // Can't end turn without choosing jack suit.
            if(_table.Peek().Value.Equals(CardValue.Jack) && _currentJackSuit == null)
            {
                return false;
            }


            // Can't stop turn if the last card was a 6.
            if (_table.Peek().Value.Equals(CardValue.Six))
            {
                // If already drawn 5 cards, return the 6 to the player's hand.
                if (PlayerList[_currentPlayer].CardsDrawn >= 5)
                {
                    while (_sameValueCards > 0)
                    {
                        await PlayerList[_currentPlayer].GetCardAsync(_table.Pop());
                        await Program.client.SendTextMessageAsync(ChatId, "Шестёрка возвращается игроку.");
                        _sameValueCards--;
                    }
                }
                else
                {
                    return false;
                }

            }
            else if (_turnCardCounter == 0 && PlayerList[_currentPlayer].CardsDrawn < 5) return false;

            if(_cardsToDraw != 0) await Program.client.SendTextMessageAsync(ChatId,
                $"{(PlayerList[(_currentPlayer + 1) % PlayerList.Count]).User.FirstName} берёт {_cardsToDraw} {(_cardsToDraw == 1 ? "карту" : _cardsToDraw > 1 && _cardsToDraw < 5 ? "карты" : "карт")}");
            while (_cardsToDraw > 0)
            {
                // If the deck is empty, reshuffle.
                if (_deck.Count == 0) await ReshuffleAsync();
                // Draw a card.
                await PlayerList[(_currentPlayer + 1) % PlayerList.Count].GetCardAsync(_deck.Pop());
                _cardsToDraw--;
            }

           
            PlayerList[_currentPlayer].CardsDrawn = 0;

            // Find if any player has no cards left.
            Player player = PlayerList.Find(p => p.HasNoCards());

            if (player != null)
            {
                _turnsToSkip = 0;
                await EndDealAsync();
                _jackCardsPlayed = 0;
                return true;
            }
            else
            {
                // Skip the needed amount of turns, and pass the turn to the next player.
                _currentPlayer += _turnsToSkip + 1;
                _turnsToSkip = 0;
                // Keep the player index from overflowing.
                _currentPlayer %= PlayerList.Count;
                _turnCardCounter = 0;
                _jackCardsPlayed = 0;

                return true;
            }


        }

        public async Task<Boolean> PlayCardAsync(Card card)
        {
            /* 
             If the value matches, place it anyway.
             If the suit matches(except if the table is a jack), or the card is a 6 or a Jack, 
             only place it if this is the first card of the turn, or the previous card was a 6. 
             If the card is a Jack, and the previous card was a 6, make sure they have the same suit. 
             If the table card is a Jack, make sure the suit matches with the picked suit.
            */
            if (
                _table.Peek().Value.Equals(card.Value) ||
                (
                ((_table.Peek().Suit.Equals(card.Suit) && !_table.Peek().Value.Equals(CardValue.Jack)) ||
                card.Value.Equals(CardValue.Six) ||
                (card.Value.Equals(CardValue.Jack) &&
                (!(_table.Peek().Value.Equals(CardValue.Six)) ||
                _table.Peek().Suit.Equals(card.Suit))) ||
                (_table.Peek().Value.Equals(CardValue.Jack) && 
                card.Suit.Equals(_currentJackSuit))) &&
                (_turnCardCounter.Equals(0) ||
                _table.Peek().Value.Equals(CardValue.Six))
                ))
            {
                switch (card.Value)
                {
                    case CardValue.Six:
                    case CardValue.Ten:
                    case CardValue.Nine:
                        break;
                    case CardValue.Jack:
                        _currentJackSuit = null;
                        _jackCardsPlayed++;
                        break;
                    case CardValue.Seven:
                        // Draw 2 cards and skip turn.
                        _cardsToDraw += 2;
                        _turnsToSkip = 1;
                        break;
                    case CardValue.Eight:
                        // Draw 1 card.
                        _cardsToDraw++;
                        break;
                    case CardValue.Ace:
                        // Skip turn (turn skips can be stacked).
                        _turnsToSkip++;
                        break;
                    case CardValue.Queen:
                        // Draw 4 cards and skip turn.
                        if (card.Suit.Equals(CardSuit.Spade))
                        {
                            _turnsToSkip = 1;
                            _cardsToDraw = 4;
                        }
                        break;
                    case CardValue.King:
                        // Draw 5 cards
                        if (card.Suit.Equals(CardSuit.Heart))
                        {
                            _cardsToDraw = 5;
                        }
                        break;
                }
                _turnCardCounter++;
                // if the card is the same as the previous one, increment the counter.
                if (card.Value.Equals(_table.Peek().Value)) _sameValueCards++;
                else _sameValueCards = 1;

                //TODO: add 4 card finish.

                _table.Push(card);

                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<Boolean> DrawCardAsync()
        {
            if (_deck.Count == 0)
            {
                await ReshuffleAsync();
                if(_deck.Count == 0)
                {
                    // If there are no cards left.
                    PlayerList[_currentPlayer].CardsDrawn = 5;
                    return false;
                }
            }

            await PlayerList[_currentPlayer].GetCardAsync(_deck.Pop());
            PlayerList[_currentPlayer].CardsDrawn++;
            return true;
        }

        private async Task EndDealAsync()
        {
            // Minus 20 points for ending with a Jack (can be stacked).
            if (_table.Peek().Value.Equals(CardValue.Jack) && PlayerList[_currentPlayer].HasNoCards())
            {
                PlayerList[_currentPlayer].Points -= 20 * _jackCardsPlayed;
                await Program.client.SendTextMessageAsync(ChatId, $"Минус {20 * _jackCardsPlayed}!");
            }
            for(int i = 0; i < PlayerList.Count;)
            {
                await PlayerList[i].CountPointsAsync();
                // Return all cards in player's hand to the cards list.
                _cards.InsertRange(0, PlayerList[i].ReturnCards());

                if (PlayerList[i].Points >= 220 && PlayerList[i].Points != PlayerList.Min(p => p.Points))
                {
                    await Program.client.SendTextMessageAsync(ChatId, $"{PlayerList[i].User.FirstName} выбывает из игры.");
                    PlayerList.RemoveAt(i);
                }
                else
                {
                    ++i;
                }
            }
            foreach (var player in PlayerList)
            {
                // Return all cards in player's hand to the cards list.
                await player.CountPointsAsync();
                _cards.InsertRange(0, player.ReturnCards());
                // If points greater than or equal 220, remove player. 
                // Except the case when this is the minimum in the game.
                if (player.Points >= 220 && player.Points != PlayerList.Min(p => p.Points))
                {
                    PlayerList.Remove(player);
                    await Program.client.SendTextMessageAsync(ChatId, $"{player.User.FirstName} выбывает из игры.");
                }
            }
            // If only one player left.
            if (PlayerList.Count == 1)
            {
                await EndGameAsync();
                return;
            }
            else if (PlayerList.Count < 1)
            {
                throw new ArgumentOutOfRangeException("PlayerList can't have less than 1 player.");
            }
            _currentDealer++;
            _currentDealer %= PlayerList.Count;

            while(_deck.Count > 0)
            {
                _cards.Add(_deck.Pop());
            }
            while(_table.Count > 0)
            {
                _cards.Add(_table.Pop());
            }

            // Sends the points of remaining players to the chat.
            await SendPointsAsync();

            await DealAsync();
        }

        public async Task PrematureEndDealAsync()
        {
            // Jack -20 don't count. 
            _jackCardsPlayed = 0;

            // Give cards to the next player.
            if (_cardsToDraw != 0) await Program.client.SendTextMessageAsync(ChatId,
                 $"{(PlayerList[(_currentPlayer + 1) % PlayerList.Count]).User.FirstName} берёт {_cardsToDraw} {(_cardsToDraw == 1 ? "карту" : _cardsToDraw > 1 && _cardsToDraw < 5 ? "карты" : "карт")}");
            while (_cardsToDraw > 0)
            {
                // If the deck is empty, reshuffle.
                if (_deck.Count == 0) await ReshuffleAsync();
                // Draw a card.
                await PlayerList[(_currentPlayer + 1) % PlayerList.Count].GetCardAsync(_deck.Pop());
                _cardsToDraw--;
            }

            // Don't skip turns.
            _turnsToSkip = 0;
            // End deal normally.
            await EndDealAsync();
        }

        public bool CanEndPrematurely() => _sameValueCards == 4 && !_table.Peek().Value.Equals(CardValue.Six) && _turnCardCounter > 0;

        public bool IsJackWithNoSuit() => _table.Peek().Value.Equals(CardValue.Jack) && _currentJackSuit == null;

        public async Task AssignSuitJack(CardSuit suit)
        {
            _currentJackSuit = suit;
            await GetCurrentStatusAsync();
        }

        private async Task SendPointsAsync()
        {
            string message = string.Empty;
            foreach (var player in PlayerList)
            {
                message += $"{player.User.FirstName}: {player.Points}{Environment.NewLine}";
            }
            message += $"Раздаёт {PlayerList[_currentDealer].User.FirstName}";

            await Program.client.SendTextMessageAsync(ChatId, message);
        }

        private async Task EndGameAsync()
        {
            await Program.client.SendTextMessageAsync(ChatId,
                $"Победитель: {PlayerList[0].User.FirstName}{Environment.NewLine}{PlayerList[0].Points} очков");
            // Removes this game from the game list.
            await Program.EndGameAsync(this);
        }

        // Returns the user whose turn it is.
        public User GetCurrentUser() => PlayerList[_currentPlayer].User;

        public GameSwedka(ChatId chatId)
        {
            ChatId = chatId;
            _deck = new Stack<Card>();
            _table = new Stack<Card>();

            // Filling the starting list with all possible cards 6-A.
            _cards = new List<Card>()
            {
                new Card(CardSuit.Club, CardValue.Six),
                new Card(CardSuit.Club, CardValue.Seven),
                new Card(CardSuit.Club, CardValue.Eight),
                new Card(CardSuit.Club, CardValue.Nine),
                new Card(CardSuit.Club, CardValue.Ten),
                new Card(CardSuit.Club, CardValue.Jack),
                new Card(CardSuit.Club, CardValue.Queen),
                new Card(CardSuit.Club, CardValue.King),
                new Card(CardSuit.Club, CardValue.Ace),
                new Card(CardSuit.Diamond, CardValue.Six),
                new Card(CardSuit.Diamond, CardValue.Seven),
                new Card(CardSuit.Diamond, CardValue.Eight),
                new Card(CardSuit.Diamond, CardValue.Nine),
                new Card(CardSuit.Diamond, CardValue.Ten),
                new Card(CardSuit.Diamond, CardValue.Jack),
                new Card(CardSuit.Diamond, CardValue.Queen),
                new Card(CardSuit.Diamond, CardValue.King),
                new Card(CardSuit.Diamond, CardValue.Ace),
                new Card(CardSuit.Heart, CardValue.Six),
                new Card(CardSuit.Heart, CardValue.Seven),
                new Card(CardSuit.Heart, CardValue.Eight),
                new Card(CardSuit.Heart, CardValue.Nine),
                new Card(CardSuit.Heart, CardValue.Ten),
                new Card(CardSuit.Heart, CardValue.Jack),
                new Card(CardSuit.Heart, CardValue.Queen),
                new Card(CardSuit.Heart, CardValue.King),
                new Card(CardSuit.Heart, CardValue.Ace),
                new Card(CardSuit.Spade, CardValue.Six),
                new Card(CardSuit.Spade, CardValue.Seven),
                new Card(CardSuit.Spade, CardValue.Eight),
                new Card(CardSuit.Spade, CardValue.Nine),
                new Card(CardSuit.Spade, CardValue.Ten),
                new Card(CardSuit.Spade, CardValue.Jack),
                new Card(CardSuit.Spade, CardValue.Queen),
                new Card(CardSuit.Spade, CardValue.King),
                new Card(CardSuit.Spade, CardValue.Ace),
            };
            _random = new Random();
        }
    }
}

