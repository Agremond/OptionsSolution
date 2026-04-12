using QuikSharp.DataStructures;
using System;

namespace GrokOptions
{
    public class Position
    {
        private const decimal DefaultSummResult = 0m;
        private int toolQty;
        private int entranceOrderQty;
        private int closingOrderQty;
        private QuikDateTime dateTimeEntrance;
        private QuikDateTime dateTimeClosing;

        /// <summary>
        /// Initializes a new instance of the Position class with default values.
        /// </summary>
        public Position()
        {
            State = State.Canceled;
            summResult = DefaultSummResult;
        }

        /// <summary>
        /// Gets or sets the option data for this position.
        /// </summary>
        public Option Option { get; set; }

        /// <summary>
        /// Gets or sets the Tool instance for futures positions.
        /// </summary>
        public Tool Tool { get; set; }

        /// <summary>
        /// Gets or sets the operating mode of the trading robot (Live or Test).
        /// </summary>
        public RobotMode RobotMode { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the position.
        /// </summary>
        public long PositionID { get; set; }

        /// <summary>
        /// Gets or sets the security code (e.g., option or futures code).
        /// </summary>
        public string SecurityCode { get; set; }

        /// <summary>
        /// Gets or sets the class code of the instrument (e.g., SPBFUT for futures).
        /// </summary>
        public string ClassCode { get; set; }

        /// <summary>
        /// Gets or sets the date and time of position entry in QuikDateTime format.
        /// </summary>
        public QuikDateTime? DateTimeEntrance { get => dateTimeEntrance; set => dateTimeEntrance = value; }

        /// <summary>
        /// Gets or sets the average entry price of the position.
        /// </summary>
        public decimal PriceEntrance { get; set; }

        /// <summary>
        /// Gets or sets the average closing price of the position.
        /// </summary>
        public decimal PriceClosing { get; set; }

        /// <summary>
        /// Gets or sets the implied volatility at the time of position entry.
        /// </summary>
        public double IVEntrance { get; set; }

        /// <summary>
        /// Gets or sets the implied volatility at the time of position closing.
        /// </summary>
        public double IVClosing { get; set; }

        /// <summary>
        /// Gets or sets the base asset price at the time of position entry.
        /// </summary>
        public double BAEntrance { get; set; }

        /// <summary>
        /// Gets or sets the base asset price at the time of position closing.
        /// </summary>
        public double BAClosing { get; set; }

        /// <summary>
        /// Gets or sets the total exchange commission for the position.
        /// </summary>
        public double StockFee { get; set; }

        /// <summary>
        /// Gets or sets the current quantity of the instrument in the position.
        /// Positive for long positions, negative for short, zero for no position.
        /// </summary>
        public int ToolQty
        {
            get => toolQty;
            set => toolQty = value >= 0 ? value : throw new ArgumentException("ToolQty cannot be negative");
        }

        /// <summary>
        /// Gets or sets the total result (profit/loss) of the position.
        /// </summary>
        public decimal summResult { get; set; }

        /// <summary>
        /// Gets or sets the transaction ID for the order to open or increase the position.
        /// </summary>
        public long EntranceOrderID { get; set; }

        /// <summary>
        /// Gets or sets the exchange order number for opening or increasing the position.
        /// </summary>
        public long EntranceOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the initial quantity in the order to open or increase the position.
        /// </summary>
        public int EntranceOrderQty
        {
            get => entranceOrderQty;
            set => entranceOrderQty = value >= 0 ? value : throw new ArgumentException("EntranceOrderQty cannot be negative");
        }

        /// <summary>
        /// Gets or sets the remaining quantity in the order to open or increase the position.
        /// </summary>
        public int EntranceOrderBalance { get; set; }

        /// <summary>
        /// Gets or sets the transaction ID for the order to close or reduce the position.
        /// </summary>
        public long ClosingOrderID { get; set; }

        /// <summary>
        /// Gets or sets the exchange order number for closing or reducing the position.
        /// </summary>
        public long ClosingOrderNumber { get; set; }

        /// <summary>
        /// Gets or sets the initial quantity in the order to close or reduce the position.
        /// </summary>
        public int ClosingOrderQty
        {
            get => closingOrderQty;
            set => closingOrderQty = value >= 0 ? value : throw new ArgumentException("ClosingOrderQty cannot be negative");
        }

        /// <summary>
        /// Gets or sets the remaining quantity in the order to close or reduce the position.
        /// </summary>
        public int ClosingOrderBalance { get; set; }

        /// <summary>
        /// Gets or sets the date and time of position closing in QuikDateTime format.
        /// </summary>
        public QuikDateTime? DateTimeClosing { get => dateTimeClosing; set => dateTimeClosing = value; }

        /// <summary>
        /// Gets or sets the current state of the order (e.g., Active, Canceled).
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// Gets or sets whether the position is a futures position.
        /// </summary>
        public bool IsFutures { get; set; }

        /// <summary>
        /// Gets or sets the direction of the position (Buy or Sell).
        /// </summary>
        public Operation Operation { get; set; }

        /// <summary>
        /// Gets or sets the message from the exchange related to the position or order.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Returns a string representation of the position for debugging and logging.
        /// </summary>
        public override string ToString()
        {
            return $"Position[ID={PositionID}, Instrument={SecurityCode}, Quantity={ToolQty}, " +
                   $"Direction={Operation}, State={State}, EntryPrice={PriceEntrance}, " +
                   $"ExitPrice={PriceClosing}, RobotMode={RobotMode}]";
        }
    }
}