
namespace GrokOptions
{
    partial class Workspace
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            bool isDesignMode = DesignMode;
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
            if (--OpenFormCount == 0 && !isDesignMode)
            {
                System.Windows.Forms.Application.Exit();
            }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Workspace));
            this.toolbarFormControl1 = new DevExpress.XtraBars.ToolbarForm.ToolbarFormControl();
            this.toolbarFormManager1 = new DevExpress.XtraBars.ToolbarForm.ToolbarFormManager(this.components);
            this.bar1 = new DevExpress.XtraBars.Bar();
            this.barButtonConnect = new DevExpress.XtraBars.BarButtonItem();
            this.btnStartStop = new DevExpress.XtraBars.BarButtonItem();
            this.barButtonItem1 = new DevExpress.XtraBars.BarButtonItem();
            this.barConnectDB = new DevExpress.XtraBars.BarButtonItem();
            this.bar2 = new DevExpress.XtraBars.Bar();
            this.barStaticItem1 = new DevExpress.XtraBars.BarStaticItem();
            this.barDockControl1 = new DevExpress.XtraBars.BarDockControl();
            this.barDockControl5 = new DevExpress.XtraBars.BarDockControl();
            this.barDockControl6 = new DevExpress.XtraBars.BarDockControl();
            this.barDockControl7 = new DevExpress.XtraBars.BarDockControl();
            this.barSubItem1 = new DevExpress.XtraBars.BarSubItem();
            this.barDockingMenuItem1 = new DevExpress.XtraBars.BarDockingMenuItem();
            this.xtraTabbedMdiManager = new DevExpress.XtraTabbedMdi.XtraTabbedMdiManager(this.components);
            this.timerRenewForm = new System.Windows.Forms.Timer(this.components);
            this.comboBoxSecurities = new DevExpress.XtraEditors.ComboBoxEdit();
            ((System.ComponentModel.ISupportInitialize)(this.toolbarFormControl1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.toolbarFormManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabbedMdiManager)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxSecurities.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // toolbarFormControl1
            // 
            this.toolbarFormControl1.Location = new System.Drawing.Point(0, 0);
            this.toolbarFormControl1.Manager = this.toolbarFormManager1;
            this.toolbarFormControl1.Name = "toolbarFormControl1";
            this.toolbarFormControl1.Size = new System.Drawing.Size(659, 31);
            this.toolbarFormControl1.TabIndex = 0;
            this.toolbarFormControl1.TabStop = false;
            this.toolbarFormControl1.ToolbarForm = this;
            // 
            // toolbarFormManager1
            // 
            this.toolbarFormManager1.Bars.AddRange(new DevExpress.XtraBars.Bar[] {
            this.bar1,
            this.bar2});
            this.toolbarFormManager1.DockControls.Add(this.barDockControl1);
            this.toolbarFormManager1.DockControls.Add(this.barDockControl5);
            this.toolbarFormManager1.DockControls.Add(this.barDockControl6);
            this.toolbarFormManager1.DockControls.Add(this.barDockControl7);
            this.toolbarFormManager1.Form = this;
            this.toolbarFormManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.barSubItem1,
            this.barDockingMenuItem1,
            this.barButtonConnect,
            this.barStaticItem1,
            this.barButtonItem1,
            this.btnStartStop,
            this.barConnectDB});
            this.toolbarFormManager1.MainMenu = this.bar1;
            this.toolbarFormManager1.MaxItemId = 8;
            this.toolbarFormManager1.StatusBar = this.bar2;
            // 
            // bar1
            // 
            this.bar1.BarName = "Пользовательская 2";
            this.bar1.DockCol = 0;
            this.bar1.DockRow = 0;
            this.bar1.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            this.bar1.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.barButtonConnect),
            new DevExpress.XtraBars.LinkPersistInfo(this.btnStartStop),
            new DevExpress.XtraBars.LinkPersistInfo(this.barButtonItem1),
            new DevExpress.XtraBars.LinkPersistInfo(this.barConnectDB)});
            this.bar1.OptionsBar.MultiLine = true;
            this.bar1.OptionsBar.UseWholeRow = true;
            this.bar1.Text = "Пользовательская 2";
            // 
            // barButtonConnect
            // 
            this.barButtonConnect.Id = 3;
            this.barButtonConnect.ImageOptions.Image = global::GrokOptions.Properties.Resources.apply_16x16_off;
            this.barButtonConnect.Name = "barButtonConnect";
            this.barButtonConnect.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonConnect_ItemClick);
            // 
            // btnStartStop
            // 
            this.btnStartStop.Caption = "Start";
            this.btnStartStop.Id = 6;
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            this.btnStartStop.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.btnStartStop_ItemClick);
            // 
            // barButtonItem1
            // 
            this.barButtonItem1.Caption = "Add Instrument";
            this.barButtonItem1.Id = 5;
            this.barButtonItem1.Name = "barButtonItem1";
            this.barButtonItem1.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barButtonItem1_ItemClick_1);
            // 
            // barConnectDB
            // 
            this.barConnectDB.Caption = "ConnectDB";
            this.barConnectDB.Id = 7;
            this.barConnectDB.Name = "barConnectDB";
            this.barConnectDB.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            this.barConnectDB.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.barConnectDB_ItemClick);
            // 
            // bar2
            // 
            this.bar2.BarName = "Пользовательская 3";
            this.bar2.CanDockStyle = DevExpress.XtraBars.BarCanDockStyle.Bottom;
            this.bar2.DockCol = 0;
            this.bar2.DockRow = 0;
            this.bar2.DockStyle = DevExpress.XtraBars.BarDockStyle.Bottom;
            this.bar2.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(this.barStaticItem1)});
            this.bar2.OptionsBar.AllowQuickCustomization = false;
            this.bar2.OptionsBar.DrawDragBorder = false;
            this.bar2.OptionsBar.UseWholeRow = true;
            this.bar2.Text = "Пользовательская 3";
            // 
            // barStaticItem1
            // 
            this.barStaticItem1.Caption = "Hello, this is status bar";
            this.barStaticItem1.Id = 4;
            this.barStaticItem1.Name = "barStaticItem1";
            // 
            // barDockControl1
            // 
            this.barDockControl1.CausesValidation = false;
            this.barDockControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.barDockControl1.Location = new System.Drawing.Point(0, 31);
            this.barDockControl1.Manager = this.toolbarFormManager1;
            this.barDockControl1.Size = new System.Drawing.Size(659, 25);
            // 
            // barDockControl5
            // 
            this.barDockControl5.CausesValidation = false;
            this.barDockControl5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.barDockControl5.Location = new System.Drawing.Point(0, 473);
            this.barDockControl5.Manager = this.toolbarFormManager1;
            this.barDockControl5.Size = new System.Drawing.Size(659, 23);
            // 
            // barDockControl6
            // 
            this.barDockControl6.CausesValidation = false;
            this.barDockControl6.Dock = System.Windows.Forms.DockStyle.Left;
            this.barDockControl6.Location = new System.Drawing.Point(0, 56);
            this.barDockControl6.Manager = this.toolbarFormManager1;
            this.barDockControl6.Size = new System.Drawing.Size(0, 417);
            // 
            // barDockControl7
            // 
            this.barDockControl7.CausesValidation = false;
            this.barDockControl7.Dock = System.Windows.Forms.DockStyle.Right;
            this.barDockControl7.Location = new System.Drawing.Point(659, 56);
            this.barDockControl7.Manager = this.toolbarFormManager1;
            this.barDockControl7.Size = new System.Drawing.Size(0, 417);
            // 
            // barSubItem1
            // 
            this.barSubItem1.Caption = "barSubItem1";
            this.barSubItem1.Id = 1;
            this.barSubItem1.Name = "barSubItem1";
            // 
            // barDockingMenuItem1
            // 
            this.barDockingMenuItem1.Caption = "barDockingMenuItem1";
            this.barDockingMenuItem1.Id = 2;
            this.barDockingMenuItem1.Name = "barDockingMenuItem1";
            // 
            // xtraTabbedMdiManager
            // 
            this.xtraTabbedMdiManager.MdiParent = this;
            this.xtraTabbedMdiManager.PageAdded += new DevExpress.XtraTabbedMdi.MdiTabPageEventHandler(this.MdiManager_PageAdded);
            // 
            // timerRenewForm
            // 
            this.timerRenewForm.Interval = 1000;
            this.timerRenewForm.Tick += new System.EventHandler(this.timerRenewForm_Tick);
            // 
            // comboBoxSecurities
            // 
            this.comboBoxSecurities.EditValue = "Загрузка инструментов...";
            this.comboBoxSecurities.Location = new System.Drawing.Point(323, 35);
            this.comboBoxSecurities.MenuManager = this.toolbarFormManager1;
            this.comboBoxSecurities.Name = "comboBoxSecurities";
            this.comboBoxSecurities.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboBoxSecurities.Properties.Items.AddRange(new object[] {
            "Загрузка инструментов..."});
            this.comboBoxSecurities.Size = new System.Drawing.Size(100, 20);
            this.comboBoxSecurities.TabIndex = 5;
            this.comboBoxSecurities.SelectedIndexChanged += new System.EventHandler(this.ComboBoxSecurities_SelectedIndexChanged);
            // 
            // Workspace
            // 
            this.ClientSize = new System.Drawing.Size(659, 496);
            this.Controls.Add(this.comboBoxSecurities);
            this.Controls.Add(this.barDockControl6);
            this.Controls.Add(this.barDockControl7);
            this.Controls.Add(this.barDockControl5);
            this.Controls.Add(this.barDockControl1);
            this.Controls.Add(this.toolbarFormControl1);
            this.IconOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("Workspace.IconOptions.LargeImage")));
            this.IsMdiContainer = true;
            this.Name = "Workspace";
            this.Text = "Workspace";
            this.ToolbarFormControl = this.toolbarFormControl1;
            ((System.ComponentModel.ISupportInitialize)(this.toolbarFormControl1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.toolbarFormManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabbedMdiManager)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxSecurities.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion


        private DevExpress.XtraBars.ToolbarForm.ToolbarFormControl toolbarFormControl1;
        private DevExpress.XtraBars.ToolbarForm.ToolbarFormManager toolbarFormManager1;
        private DevExpress.XtraBars.BarDockControl barDockControl1;
        private DevExpress.XtraBars.BarDockControl barDockControl5;
        private DevExpress.XtraBars.BarDockControl barDockControl6;
        private DevExpress.XtraBars.BarDockControl barDockControl7;
        private DevExpress.XtraTabbedMdi.XtraTabbedMdiManager xtraTabbedMdiManager;
        private DevExpress.XtraBars.BarSubItem barSubItem1;
        private DevExpress.XtraBars.BarDockingMenuItem barDockingMenuItem1;
        private DevExpress.XtraBars.Bar bar1;
        private DevExpress.XtraBars.BarButtonItem barButtonConnect;
        private DevExpress.XtraBars.Bar bar2;
        private DevExpress.XtraBars.BarStaticItem barStaticItem1;
        private DevExpress.XtraBars.BarButtonItem barButtonItem1;
        private System.Windows.Forms.Timer timerRenewForm;
        private DevExpress.XtraBars.BarButtonItem barConnectDB;
        private DevExpress.XtraBars.BarButtonItem btnStartStop;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxSecurities;
    }
}

