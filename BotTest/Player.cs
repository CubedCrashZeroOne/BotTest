using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;


namespace BotTest
{
    internal sealed class Player
    {
        private User _user;
        public User User { get => _user; }

        // The amount of points in the current game.
        public int Points { get; set; }
        
        // If the hand changed from the last inline query..
        private bool _cardsChanged;
        public bool CardsChanged { set => _cardsChanged = value; }

        private int _cardsDrawn = 0;
        // Cards drawn this turn.
        public int CardsDrawn { get => _cardsDrawn; set => _cardsDrawn = value; }

        // The inline query results shown to the player
        private InlineQueryResultBase[] _queryResult = null;
        
        //The list of cards currently in this player's possession.
        private List<Card> _hand = new List<Card>();

        public async Task GetCardAsync(Card card)
        {
            _hand.Add(card);
            _cardsChanged = true;
        }

        public async Task CountPointsAsync()
        {
            foreach(var card in _hand)
            {
                switch (card.Value)
                {
                    case CardValue.Jack:
                        Points += 20;
                        break;
                    case CardValue.Ace:
                        Points += 15;
                        break;
                    case CardValue.Ten:
                        Points += 10;
                        break;
                    case CardValue.Queen:
                        if (card.Suit.Equals(CardSuit.Spade)) Points += 40;
                        else Points += 10;
                        break;
                    case CardValue.King:
                        if (card.Suit.Equals(CardSuit.Heart)) Points += 50;
                        else Points += 10;
                        break;
                }
            }
        }

        public async Task<Card?> PlayCardAsync(CardSuit suit, CardValue value)
        {
            if (_hand.Exists(c => c.Suit == suit && c.Value == value))
            {
                Card result = _hand.Find(c => c.Suit == suit && c.Value == value);
                _hand.Remove(result);
                _cardsChanged = true;
                return result;
            }
            else return null;
        }

        public async Task<InlineQueryResultBase[]> QueryResultAsync()
        {
            if (_cardsChanged)
            {
                _queryResult = new InlineQueryResultBase[_hand.Count + (_cardsDrawn >= 5 ? 1 : 2)];
                for (int i = 0; i < _hand.Count; ++i)
                {
                    _queryResult[i] =
                        new InlineQueryResultArticle(
                            id: _hand[i].ToString(),
                            title: _hand[i].ToString(),
                            inputMessageContent: new InputTextMessageContent(_hand[i].ToString())
                            );
                }
                _queryResult[_queryResult.Length - 1] = new InlineQueryResultArticle(
                            id: "End",
                            title: "End",
                            inputMessageContent: new InputTextMessageContent("End")
                            );
                // Allow pickup only if less than 5 cards were picked up this turn.
                if(_cardsDrawn < 5)
                {
                    _queryResult[_queryResult.Length - 2] = new InlineQueryResultArticle(
                            id: "Pickup",
                            title: "Pickup",
                            inputMessageContent: new InputTextMessageContent("Pickup")
                            );
                }
            }
            _cardsChanged = false;
            return _queryResult;
        }

        // Return cards to the deck.
        public List<Card> ReturnCards()
        {
            List<Card> result = new List<Card>();
            foreach(var card in _hand)
            {
                result.Add(card);
            }

            _hand.Clear();

            return result;
        }


        public int CardCount() => _hand.Count;
        public bool HasNoCards() => _hand.Count == 0;

        public Player(User user)
        {
            _user = user;
        }
    }
}

