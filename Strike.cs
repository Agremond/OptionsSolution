namespace GrokOptions
{        
    /// <summary>
    /// Страйк опционов
    /// </summary>
    public class Strike
    {
        Option call;
        Option put;
        public int strike;
        /// <summary>
        /// Задает/получает опцион Call
        /// </summary>
        public Option Call
        {
            get
            { return call; }
            set
            { call = value; }
        }
        /// <summary>
        /// Задает/получает опцион Put
        /// </summary>
        public Option Put
        {
            get
            { return put; }
            set
            { put = value; }
        }
    }
}
