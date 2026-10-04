namespace MapController
{
    partial class MapControl
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.mapBox = new SharpMap.Forms.MapBox();
            this.mapZoomToolStrip1 = new SharpMap.Forms.ToolBar.MapZoomToolStrip(this.components);
            this.mapVariableLayerToolStrip1 = new SharpMap.Forms.ToolBar.MapVariableLayerToolStrip(this.components);
            this.mapQueryToolStrip1 = new SharpMap.Forms.ToolBar.MapQueryToolStrip(this.components);
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.mapBox);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(668, 290);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(668, 317);
            this.toolStripContainer1.TabIndex = 0;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.mapZoomToolStrip1);
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.mapQueryToolStrip1);
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.mapVariableLayerToolStrip1);
            // 
            // mapBox
            // 
            this.mapBox.ActiveTool = SharpMap.Forms.MapBox.Tools.None;
            this.mapBox.BackColor = System.Drawing.Color.White;
            this.mapBox.Cursor = System.Windows.Forms.Cursors.Default;
            this.mapBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapBox.FineZoomFactor = 10D;
            this.mapBox.Location = new System.Drawing.Point(0, 0);
            this.mapBox.MapQueryMode = SharpMap.Forms.MapBox.MapQueryType.LayerByIndex;
            this.mapBox.Name = "mapBox";
            this.mapBox.QueryGrowFactor = 5F;
            this.mapBox.QueryLayerIndex = 0;
            this.mapBox.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(210)))), ((int)(((byte)(244)))), ((int)(((byte)(244)))), ((int)(((byte)(244)))));
            this.mapBox.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(244)))), ((int)(((byte)(244)))), ((int)(((byte)(244)))));
            this.mapBox.ShowProgressUpdate = false;
            this.mapBox.Size = new System.Drawing.Size(668, 290);
            this.mapBox.TabIndex = 0;
            this.mapBox.Text = "mapBox1";
            this.mapBox.WheelZoomMagnitude = -2D;
            this.mapBox.MouseMove += new SharpMap.Forms.MapBox.MouseEventHandler(this.MapBox1_MouseMove);
            this.mapBox.MouseDown += new SharpMap.Forms.MapBox.MouseEventHandler(this.MapBox1_MouseDown);
            this.mapBox.MouseUp += new SharpMap.Forms.MapBox.MouseEventHandler(this.MapBox1_MouseUp);
            // 
            // mapZoomToolStrip1
            // 
            this.mapZoomToolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.mapZoomToolStrip1.Enabled = false;
            this.mapZoomToolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mapZoomToolStrip1.Location = new System.Drawing.Point(3, 0);
            this.mapZoomToolStrip1.MapControl = this.mapBox;
            this.mapZoomToolStrip1.Name = "mapZoomToolStrip1";
            this.mapZoomToolStrip1.Size = new System.Drawing.Size(321, 27);
            this.mapZoomToolStrip1.TabIndex = 0;
            this.mapZoomToolStrip1.Text = "MapZoomToolStrip";
            // 
            // mapVariableLayerToolStrip1
            // 
            this.mapVariableLayerToolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.mapVariableLayerToolStrip1.Enabled = false;
            this.mapVariableLayerToolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mapVariableLayerToolStrip1.Location = new System.Drawing.Point(576, 0);
            this.mapVariableLayerToolStrip1.MapControl = this.mapBox;
            this.mapVariableLayerToolStrip1.Name = "mapVariableLayerToolStrip1";
            this.mapVariableLayerToolStrip1.Size = new System.Drawing.Size(92, 27);
            this.mapVariableLayerToolStrip1.TabIndex = 0;
            // 
            // mapQueryToolStrip1
            // 
            this.mapQueryToolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.mapQueryToolStrip1.Enabled = false;
            this.mapQueryToolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.mapQueryToolStrip1.Location = new System.Drawing.Point(324, 0);
            this.mapQueryToolStrip1.MapControl = this.mapBox;
            this.mapQueryToolStrip1.Name = "mapQueryToolStrip1";
            this.mapQueryToolStrip1.Size = new System.Drawing.Size(252, 27);
            this.mapQueryToolStrip1.TabIndex = 1;
            // 
            // MapControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "MapControl";
            this.Size = new System.Drawing.Size(668, 317);
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        public SharpMap.Forms.MapBox mapBox;
        public SharpMap.Forms.ToolBar.MapVariableLayerToolStrip mapVariableLayerToolStrip1;
        public SharpMap.Forms.ToolBar.MapZoomToolStrip mapZoomToolStrip1;
        public SharpMap.Forms.ToolBar.MapQueryToolStrip mapQueryToolStrip1;
    }
}
