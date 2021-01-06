namespace BotTest
{
    internal enum CardSuit
    {
        Spade, Diamond, Club, Heart
    }

    internal enum CardValue
    {
        Six = 6, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace
    }

    internal struct Card
    {
        public CardSuit Suit { get; }
        public CardValue Value { get; }

        public override string ToString()
        {
            string result = string.Empty;

            switch (Suit)
            {
                case CardSuit.Club:
                    result += "\U00002663";
                    break;
                case CardSuit.Diamond:
                    result += "\U00002666";
                    break;
                case CardSuit.Heart:
                    result += "\U00002665";
                    break;
                case CardSuit.Spade:
                    result += "\U00002660";
                    break;
            }
            switch (Value)
            {
                case CardValue.Six:
                    result += "6";
                    break;
                case CardValue.Seven:
                    result += "7";
                    break;
                case CardValue.Eight:
                    result += "8";
                    break;
                case CardValue.Nine:
                    result += "9";
                    break;
                case CardValue.Ten:
                    result += "10";
                    break;
                case CardValue.Jack:
                    result += "J";
                    break;
                case CardValue.Queen:
                    result += "Q";
                    break;
                case CardValue.King:
                    result += "K";
                    break;
                case CardValue.Ace:
                    result += "A";
                    break;
            }
            return result;
        }

        public Card(CardSuit suit, CardValue value)
        {
            Suit = suit;
            Value = value;
        }
    }
}

