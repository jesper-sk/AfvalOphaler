﻿namespace AfvalOphaler
{
    partial class RouteVisualizer
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mapbox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.mapbox)).BeginInit();
            this.SuspendLayout();
            // 
            // mapbox
            // 
            this.mapbox.Location = new System.Drawing.Point(0, 0);
            this.mapbox.Name = "mapbox";
            this.mapbox.Size = new System.Drawing.Size(281, 224);
            this.mapbox.TabIndex = 0;
            this.mapbox.TabStop = false;
            // 
            // RouteVisualizer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1098, 649);
            this.Controls.Add(this.mapbox);
            this.Name = "RouteVisualizer";
            this.Text = "RouteVisualizer";
            this.SizeChanged += new System.EventHandler(this.RouteVisualizer_SizeChanged);
            ((System.ComponentModel.ISupportInitialize)(this.mapbox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox mapbox;
    }
}