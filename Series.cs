using System.Collections.Generic;

namespace GrokOptions
{
    public class Series
    {
        List<Strike> strikes;
        string expdate;
        string ba;
        /// <summary>
        /// Задает и получает список страйков
        /// </summary>
        public List<Strike> Strikes
        {
            get
            { return strikes; }
            set
            { strikes = value; }
        }
        /// <summary>
        /// Задает и получает дату экспирации данной серии
        /// </summary>
        public string ExpDate
        {
            get
            { return expdate; }
            set
            { expdate = value; }
        }
        /// <summary>
        /// Задает и получает код базового актива
        /// </summary>
        public string BaseActive
        {
            get
            { return ba; }
            set
            { ba = value; }
        }

    }
}
