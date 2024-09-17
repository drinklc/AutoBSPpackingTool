
namespace AutoBSPpackingTool
{
	partial class Form3
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
			if(disposing && (components != null))
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form3));
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.listBox2 = new System.Windows.Forms.ListBox();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.button4 = new System.Windows.Forms.Button();
			this.button5 = new System.Windows.Forms.Button();
			this.button6 = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.button7 = new System.Windows.Forms.Button();
			this.button8 = new System.Windows.Forms.Button();
			this.button9 = new System.Windows.Forms.Button();
			this.button10 = new System.Windows.Forms.Button();
			this.label7 = new System.Windows.Forms.Label();
			this.button11 = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.listBox3 = new AutoBSPpackingTool.ReorderableListBox();
			this.checkedListBox1 = new AutoBSPpackingTool.DisableableCheckedListBox();
			this.checkedListBox2 = new AutoBSPpackingTool.DisableableCheckedListBox();
			this.checkedListBox3 = new AutoBSPpackingTool.DisableableCheckedListBox();
			this.label9 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.listBox1.FormattingEnabled = true;
			this.listBox1.HorizontalScrollbar = true;
			this.listBox1.Location = new System.Drawing.Point(8, 26);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(200, 199);
			this.listBox1.TabIndex = 1;
			this.listBox1.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(216, 26);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(250, 20);
			this.textBox1.TabIndex = 5;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
			this.textBox1.Leave += new System.EventHandler(this.textBox1_Leave);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(216, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(94, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "game folder name:";
			// 
			// textBox2
			// 
			this.textBox2.Location = new System.Drawing.Point(216, 72);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(250, 20);
			this.textBox2.TabIndex = 6;
			this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
			this.textBox2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox2_KeyDown);
			this.textBox2.Leave += new System.EventHandler(this.textBox2_Leave);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(216, 54);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(115, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "game root folder name:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(216, 101);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(110, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "VPK packages paths:";
			// 
			// listBox2
			// 
			this.listBox2.AllowDrop = true;
			this.listBox2.FormattingEnabled = true;
			this.listBox2.HorizontalScrollbar = true;
			this.listBox2.Location = new System.Drawing.Point(216, 119);
			this.listBox2.Name = "listBox2";
			this.listBox2.Size = new System.Drawing.Size(250, 82);
			this.listBox2.TabIndex = 7;
			this.listBox2.SelectedIndexChanged += new System.EventHandler(this.listBox2_SelectedIndexChanged);
			this.listBox2.DragDrop += new System.Windows.Forms.DragEventHandler(this.listBox2_DragDrop);
			this.listBox2.DragEnter += new System.Windows.Forms.DragEventHandler(this.listBox2_DragEnter);
			// 
			// textBox3
			// 
			this.textBox3.Location = new System.Drawing.Point(216, 229);
			this.textBox3.Name = "textBox3";
			this.textBox3.Size = new System.Drawing.Size(198, 20);
			this.textBox3.TabIndex = 12;
			this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
			this.textBox3.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox3_KeyDown);
			this.textBox3.Leave += new System.EventHandler(this.textBox3_Leave);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(7, 228);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(22, 22);
			this.button1.TabIndex = 2;
			this.button1.Text = "+";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(31, 228);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(22, 22);
			this.button2.TabIndex = 3;
			this.button2.Text = "-";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// button3
			// 
			this.button3.Location = new System.Drawing.Point(55, 228);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(40, 22);
			this.button3.TabIndex = 4;
			this.button3.Text = "Save";
			this.button3.UseVisualStyleBackColor = true;
			this.button3.Click += new System.EventHandler(this.button3_Click);
			// 
			// button4
			// 
			this.button4.Location = new System.Drawing.Point(215, 204);
			this.button4.Name = "button4";
			this.button4.Size = new System.Drawing.Size(22, 22);
			this.button4.TabIndex = 8;
			this.button4.Text = "+";
			this.button4.UseVisualStyleBackColor = true;
			this.button4.Click += new System.EventHandler(this.button4_Click);
			// 
			// button5
			// 
			this.button5.Location = new System.Drawing.Point(239, 204);
			this.button5.Name = "button5";
			this.button5.Size = new System.Drawing.Size(22, 22);
			this.button5.TabIndex = 9;
			this.button5.Text = "-";
			this.button5.UseVisualStyleBackColor = true;
			this.button5.Click += new System.EventHandler(this.button5_Click);
			// 
			// button6
			// 
			this.button6.Location = new System.Drawing.Point(263, 204);
			this.button6.Name = "button6";
			this.button6.Size = new System.Drawing.Size(44, 22);
			this.button6.TabIndex = 10;
			this.button6.Text = "Reset";
			this.button6.UseVisualStyleBackColor = true;
			this.button6.Click += new System.EventHandler(this.button6_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(8, 265);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(99, 13);
			this.label4.TabIndex = 0;
			this.label4.Text = "available extra files:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(216, 265);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(91, 13);
			this.label5.TabIndex = 0;
			this.label5.Text = "available settings:";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(8, 8);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(41, 13);
			this.label6.TabIndex = 0;
			this.label6.Text = "games:";
			// 
			// button7
			// 
			this.button7.Location = new System.Drawing.Point(7, 485);
			this.button7.Name = "button7";
			this.button7.Size = new System.Drawing.Size(44, 22);
			this.button7.TabIndex = 14;
			this.button7.Text = "Reset";
			this.button7.UseVisualStyleBackColor = true;
			this.button7.Click += new System.EventHandler(this.button7_Click);
			// 
			// button8
			// 
			this.button8.Location = new System.Drawing.Point(215, 485);
			this.button8.Name = "button8";
			this.button8.Size = new System.Drawing.Size(44, 22);
			this.button8.TabIndex = 16;
			this.button8.Text = "Reset";
			this.button8.UseVisualStyleBackColor = true;
			this.button8.Click += new System.EventHandler(this.button8_Click);
			// 
			// button9
			// 
			this.button9.Location = new System.Drawing.Point(417, 228);
			this.button9.Name = "button9";
			this.button9.Size = new System.Drawing.Size(50, 22);
			this.button9.TabIndex = 11;
			this.button9.Text = "Browse";
			this.button9.UseVisualStyleBackColor = true;
			this.button9.Click += new System.EventHandler(this.button9_Click);
			// 
			// button10
			// 
			this.button10.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.button10.Location = new System.Drawing.Point(7, 522);
			this.button10.Name = "button10";
			this.button10.Size = new System.Drawing.Size(120, 22);
			this.button10.TabIndex = 17;
			this.button10.Text = "advanced settings";
			this.button10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.button10.UseVisualStyleBackColor = true;
			this.button10.Click += new System.EventHandler(this.button10_Click);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(8, 551);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(102, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "assets search order:";
			// 
			// button11
			// 
			this.button11.Location = new System.Drawing.Point(7, 636);
			this.button11.Name = "button11";
			this.button11.Size = new System.Drawing.Size(44, 22);
			this.button11.TabIndex = 20;
			this.button11.Text = "Reset";
			this.button11.UseVisualStyleBackColor = true;
			this.button11.Click += new System.EventHandler(this.button11_Click);
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(216, 551);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(114, 13);
			this.label8.TabIndex = 0;
			this.label8.Text = "assets search settings:";
			// 
			// listBox3
			// 
			this.listBox3.FormattingEnabled = true;
			this.listBox3.ItemHeight = 15;
			this.listBox3.Location = new System.Drawing.Point(8, 569);
			this.listBox3.Name = "listBox3";
			this.listBox3.Size = new System.Drawing.Size(200, 64);
			this.listBox3.TabIndex = 18;
			this.listBox3.ItemReordered += new System.EventHandler(this.listBox3_ItemReordered);
			this.listBox3.Leave += new System.EventHandler(this.listBox3_Leave);
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.CheckOnClick = true;
			this.checkedListBox1.FormattingEnabled = true;
			this.checkedListBox1.HorizontalScrollbar = true;
			this.checkedListBox1.Location = new System.Drawing.Point(8, 283);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(200, 199);
			this.checkedListBox1.TabIndex = 13;
			this.checkedListBox1.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox1_ItemCheck);
			this.checkedListBox1.Leave += new System.EventHandler(this.checkedListBox1_Leave);
			// 
			// checkedListBox2
			// 
			this.checkedListBox2.CheckOnClick = true;
			this.checkedListBox2.FormattingEnabled = true;
			this.checkedListBox2.HorizontalScrollbar = true;
			this.checkedListBox2.Location = new System.Drawing.Point(216, 283);
			this.checkedListBox2.Name = "checkedListBox2";
			this.checkedListBox2.Size = new System.Drawing.Size(250, 199);
			this.checkedListBox2.TabIndex = 15;
			this.checkedListBox2.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox2_ItemCheck);
			this.checkedListBox2.Leave += new System.EventHandler(this.checkedListBox2_Leave);
			// 
			// checkedListBox3
			// 
			this.checkedListBox3.CheckOnClick = true;
			this.checkedListBox3.FormattingEnabled = true;
			this.checkedListBox3.HorizontalScrollbar = true;
			this.checkedListBox3.Location = new System.Drawing.Point(216, 569);
			this.checkedListBox3.Name = "checkedListBox3";
			this.checkedListBox3.Size = new System.Drawing.Size(250, 64);
			this.checkedListBox3.TabIndex = 19;
			this.checkedListBox3.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox3_ItemCheck);
			this.checkedListBox3.Leave += new System.EventHandler(this.checkedListBox3_Leave);
			// 
			// label9
			// 
			this.label9.BackColor = System.Drawing.SystemColors.ControlDark;
			this.label9.Location = new System.Drawing.Point(8, 514);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(458, 1);
			this.label9.TabIndex = 0;
			// 
			// Form3
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(474, 665);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.button11);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.listBox3);
			this.Controls.Add(this.button10);
			this.Controls.Add(this.button9);
			this.Controls.Add(this.button8);
			this.Controls.Add(this.button7);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.checkedListBox1);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.checkedListBox2);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.button6);
			this.Controls.Add(this.button5);
			this.Controls.Add(this.button4);
			this.Controls.Add(this.textBox3);
			this.Controls.Add(this.listBox2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.textBox2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.checkedListBox3);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form3";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "game configurations";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form3_FormClosing);
			this.Load += new System.EventHandler(this.Form3_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListBox listBox2;
		private System.Windows.Forms.TextBox textBox3;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button button4;
		private System.Windows.Forms.Button button5;
		private System.Windows.Forms.Button button6;
		private System.Windows.Forms.Label label4;
		private DisableableCheckedListBox checkedListBox1;
		private System.Windows.Forms.Label label5;
		private DisableableCheckedListBox checkedListBox2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Button button7;
		private System.Windows.Forms.Button button8;
		private System.Windows.Forms.Button button9;
		private System.Windows.Forms.Button button10;
		private ReorderableListBox listBox3;
		private DisableableCheckedListBox checkedListBox3;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button button11;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
	}
}