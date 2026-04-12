using DevExpress.XtraEditors;
using DevExpress.XtraBars;
using DevExpress.XtraTabbedMdi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrokOptions
{
    public partial class Journal : DevExpress.XtraEditors.XtraForm
    {
        private BindingSource bsLogs = new BindingSource();
        DataTable tableLogs = new DataTable();
        
        void Init()
        {
            tableLogs = initLogs("logs");
            bsLogs.DataSource = tableLogs;

            gridControl1.DataSource = bsLogs;

        }
        public Journal()
        {
            InitializeComponent();
            Init();
        }
        public DataTable initLogs(String name)
        {
            DataTable dataTable = new System.Data.DataTable(name);


            DataColumn dataColumn;
            dataColumn = new DataColumn("id");
            dataColumn.DataType = System.Type.GetType("System.String");
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn("Date");
            dataColumn.DataType = System.Type.GetType("System.String");
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn("Time");
            dataColumn.DataType = System.Type.GetType("System.String");
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn("Source");
            dataColumn.DataType = System.Type.GetType("System.String");
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn("Severity");
            dataColumn.DataType = System.Type.GetType("System.String");
            dataTable.Columns.Add(dataColumn);
            dataColumn = new DataColumn("Message");
            dataColumn.DataType = System.Type.GetType("System.String");
            dataTable.Columns.Add(dataColumn);
            dataTable.PrimaryKey = new DataColumn[] { dataTable.Columns["id"] };
            return dataTable;
        }

              
        public void Log(string _source, string _severity, string _message)
        {
            
            DataRow logRow = tableLogs.NewRow();
            logRow["id"] = tableLogs.Rows.Count;
            logRow["Date"] = DateTime.Now.ToShortDateString();
            logRow["Time"] = DateTime.Now.ToShortTimeString() + "." + DateTime.Now.Second.ToString();
            logRow["Source"] = _source;
            logRow["Severity"] = _severity;
            logRow["Message"] = _message;
            tableLogs.Rows.Add(logRow);
        }


        private void Form2_Load(object sender, EventArgs e)
        {
            // Create a Bar Manager that will display a bar of commands at the top of the main form.
            //BarManager barManager = new BarManager();
            //barManager.Form = this;
            //// Create a bar with a New button.
            //barManager.BeginUpdate();
            //Bar bar = new Bar(barManager, "My Bar");
            //bar.DockStyle = BarDockStyle.Top;
            //barManager.MainMenu = bar;
            //BarItem barItem = new BarButtonItem(barManager, "New");
            //barItem.ItemClick += new ItemClickEventHandler(barItem_ItemClick);
            //bar.ItemLinks.Add(barItem);
            //barManager.EndUpdate();
            //Create an XtraTabbedMdiManager that will manage MDI child windows.
            //mdiManager = new XtraTabbedMdiManager(components);
            //mdiManager.MdiParent = this;
            //mdiManager.PageAdded += MdiManager_PageAdded;
        }

    }
}